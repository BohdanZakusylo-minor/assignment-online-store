namespace OrderManagerService.Models;

public class OrderShippedEvent
{
    public int OrderId { get; set; }
    public List<int> ProductsId { get; set; } = new();
    public DateTime OrderDate { get; set; }
    public DateTime ShipmentDate { get; set; }
    public string Destination { get; set; } = string.Empty;
}

