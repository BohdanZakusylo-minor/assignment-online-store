using MediatR;
using ProductManagerService.Commands;
using ProductManagerService.Repositories;

namespace ProductManagerService.Handlers;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductCommandResult>
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<UpdateProductCommandHandler> _logger;

    public UpdateProductCommandHandler(IProductRepository productRepository, ILogger<UpdateProductCommandHandler> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<ProductCommandResult> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var existingProduct = await _productRepository.GetByIdAsync(request.ProductId);
        if (existingProduct == null)
        {
            throw new KeyNotFoundException($"Product with id {request.ProductId} not found");
        }

        // Business validation
        ValidateProduct(request.Name, request.Description, request.Price);

        // Update product
        existingProduct.Name = request.Name;
        existingProduct.Description = request.Description;
        existingProduct.Price = request.Price;
        existingProduct.ImageUrls = request.ImageUrls;

        var updatedProduct = await _productRepository.UpdateAsync(existingProduct);
        _logger.LogInformation($"Updated product {updatedProduct.ProductId}");

        return new ProductCommandResult
        {
            ProductId = updatedProduct.ProductId,
            Name = updatedProduct.Name,
            Description = updatedProduct.Description,
            Price = updatedProduct.Price,
            ImageUrls = updatedProduct.ImageUrls
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

