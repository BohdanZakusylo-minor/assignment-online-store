using MediatR;

namespace ProductManagerService.Commands;

public class CreateProductCommand : IRequest<ProductCommandResult>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public List<IFormFile> Images { get; set; } = new();
}

public class ProductCommandResult
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}

