namespace OrderManagerService.DTOs;

public class OrderDto
{
    public int OrderId { get; set; }
    public List<int> ProductsId { get; set; } = new();
    public DateTime OrderDate { get; set; }
    public DateTime? ShipmentDate { get; set; }
    public string Destination { get; set; } = string.Empty;
}

public class CreateOrderDto
{
    public List<int> ProductsId { get; set; } = new();
    public string Destination { get; set; } = string.Empty;
}

