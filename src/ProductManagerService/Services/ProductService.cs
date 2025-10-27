using ProductManagerService.DTOs;
using ProductManagerService.Models;
using ProductManagerService.Repositories;

namespace ProductManagerService.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ImageService _imageService;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository productRepository, 
        ImageService imageService,
        ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _imageService = imageService;
        _logger = logger;
    }

    public async Task<List<ProductDto>> GetAllProductsAsync()
    {
        var products = await _productRepository.GetAllAsync();
        return products.Select(MapToDto).ToList();
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        return product != null ? MapToDto(product) : null;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto createDto)
    {
        // Business logic validation
        ValidateProductName(createDto.Name);
        ValidateProductDescription(createDto.Description);
        ValidateProductPrice(createDto.Price);

        // Upload images
        List<string> imageUrls = new();
        if (createDto.Images != null && createDto.Images.Any())
        {
            imageUrls = await _imageService.UploadImagesAsync(createDto.Images);
        }

        var product = new Product
        {
            Name = createDto.Name,
            Description = createDto.Description,
            Price = createDto.Price,
            ImageUrls = imageUrls
        };

        var createdProduct = await _productRepository.CreateAsync(product);
        _logger.LogInformation($"Created product {createdProduct.ProductId}: {createdProduct.Name}");
        
        return MapToDto(createdProduct);
    }

    public async Task<ProductDto> UpdateProductAsync(int id, UpdateProductDto updateDto)
    {
        var existingProduct = await _productRepository.GetByIdAsync(id);
        if (existingProduct == null)
        {
            throw new KeyNotFoundException($"Product with id {id} not found");
        }

        // Business logic validation
        ValidateProductName(updateDto.Name);
        ValidateProductDescription(updateDto.Description);
        ValidateProductPrice(updateDto.Price);

        if (id != updateDto.ProductId)
        {
            throw new ArgumentException("Id in URL must match ProductId in body");
        }

        existingProduct.Name = updateDto.Name;
        existingProduct.Description = updateDto.Description;
        existingProduct.Price = updateDto.Price;
        existingProduct.ImageUrls = updateDto.ImageUrls;

        var updatedProduct = await _productRepository.UpdateAsync(existingProduct);
        _logger.LogInformation($"Updated product {updatedProduct.ProductId}");
        
        return MapToDto(updatedProduct);
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var deleted = await _productRepository.DeleteAsync(id);
        if (deleted)
        {
            _logger.LogInformation($"Deleted product {id}");
        }
        return deleted;
    }

    // Business validation rules
    private void ValidateProductName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required");
        }

        if (name.Length > 200)
        {
            throw new ArgumentException("Name cannot exceed 200 characters");
        }
    }

    private void ValidateProductDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required");
        }

        if (description.Length > 1000)
        {
            throw new ArgumentException("Description cannot exceed 1000 characters");
        }
    }

    private void ValidateProductPrice(decimal price)
    {
        if (price < 0)
        {
            throw new ArgumentException("Price cannot be negative");
        }

        if (price == 0)
        {
            throw new ArgumentException("Price must be greater than zero");
        }
    }

    private ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            ProductId = product.ProductId,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            ImageUrls = product.ImageUrls
        };
    }
}

