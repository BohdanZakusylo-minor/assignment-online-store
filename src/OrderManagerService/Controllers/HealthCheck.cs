using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagerService.Models;

namespace OrderManagerService.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthCheck : ControllerBase
{
    private readonly OrderDbContext _context;

    public HealthCheck(OrderDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            // Check database connectivity
            await _context.Database.CanConnectAsync();
            
            // Get basic stats
            var orderCount = await _context.Orders.CountAsync();
            var pendingOrders = await _context.Orders
                .Where(o => o.ShipmentDate == null)
                .CountAsync();
            var shippedOrders = await _context.Orders
                .Where(o => o.ShipmentDate != null)
                .CountAsync();
            
            return Ok(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                database = "Connected",
                orders = orderCount,
                pendingOrders = pendingOrders,
                shippedOrders = shippedOrders,
                service = "OrderManagerService"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                status = "Unhealthy",
                timestamp = DateTime.UtcNow,
                database = "Disconnected",
                error = ex.Message,
                service = "OrderManagerService"
            });
        }
    }
}

