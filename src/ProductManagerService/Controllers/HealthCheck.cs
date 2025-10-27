using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductManagerService.Models;

namespace ProductManagerService.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthCheck : ControllerBase
{
    private readonly ProductDbContext _context;

    public HealthCheck(ProductDbContext context)
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
            var productCount = await _context.Products.CountAsync();
            
            return Ok(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                database = "Connected",
                products = productCount,
                service = "ProductManagerService"
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
                service = "ProductManagerService"
            });
        }
    }
}
