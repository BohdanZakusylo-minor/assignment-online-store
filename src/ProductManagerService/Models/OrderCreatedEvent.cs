namespace ProductManagerService.Models;

public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public List<int> ProductsId { get; set; } = new();
}

