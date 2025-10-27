using MediatR;
using ProductManagerService.Commands;
using ProductManagerService.Models;
using ProductManagerService.Repositories;
using ProductManagerService.Services;

namespace ProductManagerService.Handlers;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductCommandResult>
{
    private readonly IProductRepository _productRepository;
    private readonly ImageService _imageService;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        ImageService imageService,
        ILogger<CreateProductCommandHandler> logger)
    {
        _productRepository = productRepository;
        _imageService = imageService;
        _logger = logger;
    }

    public async Task<ProductCommandResult> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Business validation
        ValidateProduct(request.Name, request.Description, request.Price);

        // Upload images
        List<string> imageUrls = new();
        if (request.Images != null && request.Images.Any())
        {
            imageUrls = await _imageService.UploadImagesAsync(request.Images);
        }

        // Create product
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            ImageUrls = imageUrls
        };

        var createdProduct = await _productRepository.CreateAsync(product);
        _logger.LogInformation($"Created product {createdProduct.ProductId}: {createdProduct.Name}");

        return new ProductCommandResult
        {
            ProductId = createdProduct.ProductId,
            Name = createdProduct.Name,
            Description = createdProduct.Description,
            Price = createdProduct.Price,
            ImageUrls = createdProduct.ImageUrls
        };
    }

    private void ValidateProduct(string name, string description, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required");

        if (name.Length > 200)
            throw new ArgumentException("Name cannot exceed 200 characters");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required");

        if (description.Length > 1000)
            throw new ArgumentException("Description cannot exceed 1000 characters");

        if (price < 0)
            throw new ArgumentException("Price cannot be negative");

        if (price == 0)
            throw new ArgumentException("Price must be greater than zero");
    }
}

