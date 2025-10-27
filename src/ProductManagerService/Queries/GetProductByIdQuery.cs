using MediatR;
using ProductManagerService.DTOs;

namespace ProductManagerService.Queries;

public class GetProductByIdQuery : IRequest<ProductDto?>
{
    public int ProductId { get; set; }
}

