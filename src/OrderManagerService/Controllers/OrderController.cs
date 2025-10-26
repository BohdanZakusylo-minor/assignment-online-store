using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagerService.Models;

namespace OrderManagerService.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly OrderDbContext _context;

    public OrderController(OrderDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var orders = await _context.Orders.ToListAsync();
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { error = "Id must be a positive number" });
        }

        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound(new { error = $"Order with id {id} not found" });
        }
        return Ok(order);
    }

    [HttpGet("destination/{destination}")]
    public async Task<IActionResult> GetOrdersByDestination(string destination)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            return BadRequest(new { error = "Destination cannot be empty" });
        }

        var orders = await _context.Orders
            .Where(o => o.Destination.Contains(destination))
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("shipped/products")]
    public async Task<IActionResult> GetShippedOrdersByProducts([FromQuery] List<int> products)
    {
        if (products == null || !products.Any())
        {
            return BadRequest(new { error = "At least one product ID must be provided" });
        }

        if (products.Any(p => p <= 0))
        {
            return BadRequest(new { error = "All product IDs must be positive numbers" });
        }

        var orders = await _context.Orders
            .Where(o => o.ShipmentDate.HasValue && 
                       o.ProductsId.Any(p => products.Contains(p)))
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("not-shipped/products")]
    public async Task<IActionResult> GetNotShippedOrdersByProducts([FromQuery] List<int> products)
    {
        if (products == null || !products.Any())
        {
            return BadRequest(new { error = "At least one product ID must be provided" });
        }

        if (products.Any(p => p <= 0))
        {
            return BadRequest(new { error = "All product IDs must be positive numbers" });
        }

        var orders = await _context.Orders
            .Where(o => !o.ShipmentDate.HasValue && 
                       o.ProductsId.Any(p => products.Contains(p)))
            .ToListAsync();

        return Ok(orders);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] Order order)
    {
        if (order.ProductsId == null || !order.ProductsId.Any())
        {
            return BadRequest(new { error = "ProductsId must contain at least one product" });
        }

        if (order.ProductsId.Any(p => p <= 0))
        {
            return BadRequest(new { error = "All product IDs must be positive numbers" });
        }

        if (string.IsNullOrWhiteSpace(order.Destination))
        {
            return BadRequest(new { error = "Destination is required" });
        }

        if (order.Destination.Length > 500)
        {
            return BadRequest(new { error = "Destination cannot exceed 500 characters" });
        }
        
        order.OrderDate = DateTime.UtcNow;
        order.ShipmentDate = null;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId }, order);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> ShipOrder(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { error = "Id must be a positive number" });
        }

        var existingOrder = await _context.Orders.FindAsync(id);
        if (existingOrder == null)
        {
            return NotFound(new { error = $"Order with id {id} not found" });
        }

        if (existingOrder.ShipmentDate.HasValue)
        {
            return BadRequest(new { error = "Order has already been shipped" });
        }

        existingOrder.ShipmentDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(existingOrder);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { error = "Id must be a positive number" });
        }

        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound(new { error = $"Order with id {id} not found" });
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
