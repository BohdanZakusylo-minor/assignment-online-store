using MediatR;
using ProductManagerService.DTOs;

namespace ProductManagerService.Queries;

public class GetAllProductsQuery : IRequest<List<ProductDto>>
{
}

