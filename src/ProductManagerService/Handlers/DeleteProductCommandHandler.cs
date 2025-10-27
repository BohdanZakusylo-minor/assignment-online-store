using MediatR;
using ProductManagerService.Commands;
using ProductManagerService.Repositories;

namespace ProductManagerService.Handlers;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public DeleteProductCommandHandler(IProductRepository productRepository, ILogger<DeleteProductCommandHandler> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var deleted = await _productRepository.DeleteAsync(request.ProductId);
        if (deleted)
        {
            _logger.LogInformation($"Deleted product {request.ProductId}");
        }
        return deleted;
    }
}

