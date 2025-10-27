using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductManagerService.Models;

namespace ProductManagerService.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductController : ControllerBase
{
    private readonly ProductDbContext _context;

    public ProductController(ProductDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _context.Products.ToListAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { error = "Id must be a positive number" });
        }

        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { error = $"Product with id {id} not found" });
        }
        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Description))
        {
            return BadRequest(new { error = "Description is required" });
        }

        if (product.Description.Length > 1000)
        {
            return BadRequest(new { error = "Description cannot exceed 1000 characters" });
        }

        if (product.Price < 0)
        {
            return BadRequest(new { error = "Price cannot be negative" });
        }

        if (product.Price == 0)
        {
            return BadRequest(new { error = "Price must be greater than zero" });
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
    {
        if (id <= 0)
        {
            return BadRequest(new { error = "Id must be a positive number" });
        }

        if (id != product.ProductId)
        {
            return BadRequest(new { error = "Id in URL must match ProductId in body" });
        }

        var existingProduct = await _context.Products.FindAsync(id);
        if (existingProduct == null)
        {
            return NotFound(new { error = $"Product with id {id} not found" });
        }

        if (string.IsNullOrWhiteSpace(product.Description))
        {
            return BadRequest(new { error = "Description is required" });
        }

        if (product.Description.Length > 1000)
        {
            return BadRequest(new { error = "Description cannot exceed 1000 characters" });
        }

        if (product.Price < 0)
        {
            return BadRequest(new { error = "Price cannot be negative" });
        }

        if (product.Price == 0)
        {
            return BadRequest(new { error = "Price must be greater than zero" });
        }

        existingProduct.Description = product.Description;
        existingProduct.Price = product.Price;
        existingProduct.ImageUrls = product.ImageUrls;

        await _context.SaveChangesAsync();
        return Ok(existingProduct);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { error = "Id must be a positive number" });
        }

        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { error = $"Product with id {id} not found" });
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
