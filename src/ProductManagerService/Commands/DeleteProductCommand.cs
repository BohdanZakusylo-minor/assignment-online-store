using MediatR;

namespace ProductManagerService.Commands;

public class DeleteProductCommand : IRequest<bool>
{
    public int ProductId { get; set; }
}

