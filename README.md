# Online Store Microservices Architecture

## Overview

This is a microservices-based online store application consisting of three independent services, each with its own database and responsibilities. The services communicate via RabbitMQ message broker for event-driven architecture.

## Architecture

### Services

1. **ReviewService** (Port 8080)
   - Manages product reviews and ratings
   - Database: PostgreSQL (port 5432)
   - Simple CRUD operations with direct database access

2. **OrderManagerService** (Port 8081)
   - Manages order lifecycle
   - Database: PostgreSQL (port 5433)
   - Event-driven with RabbitMQ
   - Publishes: `OrderCreated`, `OrderShipped`
   - Listens: `OrderValidated`

3. **ProductManagerService** (Port 8082)
   - Manages product catalog with image storage
   - Database: PostgreSQL (port 5434)
   - Implements **CQRS pattern** with MediatR
   - Uses MinIO for object storage
   - Event-driven with RabbitMQ
   - Listens: `OrderCreated`
   - Publishes: `OrderValidated`

### Infrastructure

- **PostgreSQL**: 3 separate instances (one per service)
- **RabbitMQ**: Message broker (ports 5672, 15672)
- **MinIO**: Object storage for product images (ports 9000, 9001)
- **Docker Compose**: Local development environment

---

## API Endpoints

### Review Service (Port 8080)

#### Get All Reviews
```http
GET /Review
```
**Response:** List of all reviews

#### Get Review by ID
```http
GET /Review/{id}
```
**Parameters:**
- `id` (int): Review ID (must be positive)

**Response:** Review object
```json
{
  "id": 1,
  "rating": 5,
  "comment": "Excellent product!",
  "createdAt": "2024-10-27T12:00:00Z"
}
```

#### Create Review
```http
POST /Review
Content-Type: application/json

{
  "rating": 4,
  "comment": "Great value for money"
}
```

**Validation:**
- Rating must be between 1 and 5
- Comment max 1000 characters
- `createdAt` is automatically set to current UTC time

**Response:** Created review (201 Created)

#### Delete Review
```http
DELETE /Review/{id}
```
**Parameters:**
- `id` (int): Review ID

**Response:** 204 No Content

---

### Order Manager Service (Port 8081)

#### Get All Orders
```http
GET /Order
```
**Response:** List of all orders

#### Get Order by ID
```http
GET /Order/{id}
```
**Parameters:**
- `id` (int): Order ID

**Response:**
```json
{
  "orderId": 1,
  "productsId": [1, 2, 3],
  "orderDate": "2024-10-27T10:00:00Z",
  "shipmentDate": "2024-10-27T10:05:00Z",
  "destination": "123 Main St, New York"
}
```

#### Get Orders by Destination
```http
GET /Order/destination/{destination}
```
**Parameters:**
- `destination` (string): Partial destination match

**Response:** List of matching orders

#### Get Shipped Orders by Products
```http
GET /Order/shipped/products?products=1&products=2
```
**Parameters:**
- `products` (int[]): Query parameter with product IDs

**Response:** List of shipped orders containing specified products

#### Get Not-Shipped Orders by Products
```http
GET /Order/not-shipped/products?products=1&products=2
```
**Parameters:**
- `products` (int[]): Query parameter with product IDs

**Response:** List of not-shipped orders containing specified products

#### Create Order
```http
POST /Order
Content-Type: application/json

{
  "productsId": [1, 2, 3],
  "destination": "123 Main St, New York"
}
```

**Validation:**
- At least one product ID required
- All product IDs must be positive
- Destination required (max 500 characters)

**Behavior:**
- Sets `orderDate` to current UTC time
- Sets `shipmentDate` to null (to be set later)
- Publishes `OrderCreated` event to RabbitMQ for validation

**Response:**
```json
{
  "order": { ... },
  "message": "Your order has been placed, please wait for the confirmation"
}
```

#### Ship Order (Manual)
```http
PUT /Order/{id}
```
**Parameters:**
- `id` (int): Order ID

**Validation:**
- Order must exist
- Order must not already be shipped

**Behavior:**
- Sets `shipmentDate` to current UTC time
- Publishes `OrderShipped` event to RabbitMQ

**Response:** Updated order

#### Delete Order
```http
DELETE /Order/{id}
```
**Parameters:**
- `id` (int): Order ID

**Response:** 204 No Content

#### Order Flow (Automatic)
1. **Order Created** → Published to `order.created` queue
2. **ProductService** listens and validates product IDs
3. **Validation Result** → Published to `order.validated` queue
4. **OrderService** listens:
   - If valid: Wait 5 seconds → Auto-ship the order
   - If invalid: Delete the order

---

### Product Manager Service (Port 8082)

Uses **CQRS (Command Query Responsibility Segregation)** pattern with MediatR

#### Get All Products
```http
GET /Product
```
**Handler:** `GetAllProductsQueryHandler` (Query)

**Response:**
```json
[
  {
    "productId": 1,
    "name": "Laptop",
    "description": "High-performance laptop",
    "price": 999.99,
    "imageUrls": [
      "http://localhost:9000/product-images/product-guid-image1.jpg"
    ]
  }
]
```

#### Get Product by ID
```http
GET /Product/{id}
```
**Parameters:**
- `id` (int): Product ID

**Handler:** `GetProductByIdQueryHandler` (Query)

**Response:** Product DTO or 404 if not found

#### Create Product
```http
POST /Product
Content-Type: multipart/form-data

name: Laptop
description: High-performance laptop
price: 999.99
images: (file1.jpg, file2.jpg)
```

**Handler:** `CreateProductCommandHandler` (Command)

**Validation:**
- Name: Required, max 200 characters
- Description: Required, max 1000 characters
- Price: Must be greater than 0

**Behavior:**
- Uploads images to MinIO bucket `product-images`
- Generates unique object names
- Returns URLs for uploaded images

**Response:**
```json
{
  "productId": 1,
  "name": "Laptop",
  "description": "High-performance laptop",
  "price": 999.99,
  "imageUrls": ["http://localhost:9000/product-images/product-uuid-file.jpg"]
}
```

#### Update Product
```http
PUT /Product/{id}
Content-Type: application/json

{
  "productId": 1,
  "name": "Updated Laptop",
  "description": "Updated description",
  "price": 899.99,
  "imageUrls": ["http://localhost:9000/product-images/image1.jpg"]
}
```

**Handler:** `UpdateProductCommandHandler` (Command)

**Validation:**
- Same as Create Product
- URL `id` must match `productId` in body
- Product must exist

**Response:** Updated product

#### Delete Product
```http
DELETE /Product/{id}
```
**Parameters:**
- `id` (int): Product ID

**Handler:** `DeleteProductCommandHandler` (Command)

**Response:** 204 No Content or 404 if not found

#### Health Check
```http
GET /health
```
**Response:** Health status

---

## Data Models

### Review
```csharp
{
  "id": int,
  "rating": int (1-5),
  "comment": string (optional, max 1000 chars),
  "createdAt": datetime
}
```

### Order
```csharp
{
  "orderId": int,
  "productsId": int[],
  "orderDate": datetime,
  "shipmentDate": datetime?,
  "destination": string (max 500 chars)
}
```

### Product
```csharp
{
  "productId": int,
  "name": string (max 200 chars),
  "description": string (max 1000 chars),
  "price": decimal (must be > 0),
  "imageUrls": string[]
}
```

---

## Running the Application

### Prerequisites
- Docker and Docker Compose

### Start All Services
```bash
cd /path/to/assignment-online-store/
docker-compose up -d
```

This will start:
- 3 PostgreSQL databases
- RabbitMQ message broker
- MinIO object storage
- All 3 microservices

### Access Services
- **ReviewService**: http://localhost:8080
- **OrderManagerService**: http://localhost:8081
- **ProductManagerService**: http://localhost:8082
- **RabbitMQ Management**: http://localhost:15672 (username: rabbituser, password: rabbitpass)
- **MinIO Console**: http://localhost:9001 (username: minioadmin, password: minioadmin)

### Swagger Documentation
Swagger UI is available in Development mode:
- ReviewService: http://localhost:8080/swagger
- OrderManagerService: http://localhost:8081/swagger
- ProductManagerService: http://localhost:8082/swagger

### Stop Services
```bash
docker-compose down
```

### View Logs
```bash
docker-compose logs -f [service-name]
# Example: docker-compose logs -f product-manager-service
```

---

## Architecture Patterns

### CQRS (Command Query Responsibility Segregation)
Only **ProductManagerService** implements CQRS:
- **Queries** (Read): `GetAllProductsQuery`, `GetProductByIdQuery`
- **Commands** (Write): `CreateProductCommand`, `UpdateProductCommand`, `DeleteProductCommand`
- **Handlers**: Separate handlers for each command/query
- **MediatR**: Mediates between controllers and handlers

### Event-Driven Architecture
- **OrderManagerService**: Publishes order events
- **ProductManagerService**: Listens to order events, validates products
- Uses RabbitMQ queues:
  - `order.created`: When new order is placed
  - `order.validated`: Validation results
  - `order.shipped`: When order is shipped

### Microservices Communication
- Async communication via RabbitMQ
- Services are loosely coupled
- No direct service-to-service calls

---

---

## Error Handling

All endpoints return appropriate HTTP status codes:
- **200 OK**: Successful GET/PUT
- **201 Created**: Successful POST
- **204 No Content**: Successful DELETE
- **400 Bad Request**: Validation errors
- **404 Not Found**: Resource not found
- **500 Internal Server Error**: Server errors

Error response format:
```json
{
  "error": "Error message description"
}
```

---

## Development Notes

### Technologies
- **.NET 9**: Core framework
- **ASP.NET Core Web API**: REST API framework
- **Entity Framework Core 9.0**: ORM
- **PostgreSQL**: Database
- **RabbitMQ**: Message broker
- **MinIO**: S3-compatible object storage
- **MediatR**: CQRS implementation
- **Swagger**: API documentation

### Project Structure
```
src/
├── OrderManagerService/
│   ├── Controllers/
│   ├── Models/
│   ├── Repositories/
│   ├── Services/
│   └── DTOs/
├── ProductManagerService/
│   ├── Controllers/
│   ├── Models/
│   ├── Repositories/
│   ├── Services/
│   ├── Commands/
│   ├── Queries/
│   ├── Handlers/
│   └── DTOs/
└── ReviewService/
    ├── Controllers/
    ├── Models/
    └── Migrations/
```


This project is part of an assignment for microservices architecture.

