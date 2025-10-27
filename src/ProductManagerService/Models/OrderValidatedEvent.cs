namespace ProductManagerService.Models;

public class OrderValidatedEvent
{
    public int OrderId { get; set; }
    public bool IsValid { get; set; }
    public List<int> InvalidProducts { get; set; } = new();
}

