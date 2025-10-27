using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductManagerService.Commands;
using ProductManagerService.DTOs;
using ProductManagerService.Queries;
using ProductManagerService.Services;

namespace ProductManagerService.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductController> _logger;

    public ProductController(IMediator mediator, ILogger<ProductController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        try
        {
            var query = new GetAllProductsQuery();
            var products = await _mediator.Send(query);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products");
            return StatusCode(500, new { error = "An error occurred while retrieving products" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { error = "Id must be a positive number" });
        }

        try
        {
            var query = new GetProductByIdQuery { ProductId = id };
            var product = await _mediator.Send(query);
            
            if (product == null)
            {
                return NotFound(new { error = $"Product with id {id} not found" });
            }
            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting product {id}");
            return StatusCode(500, new { error = "An error occurred while retrieving the product" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct(
        [FromForm] string name,
        [FromForm] string description,
        [FromForm] decimal price,
        [FromForm] List<IFormFile> images)
    {
        try
        {
            var command = new CreateProductCommand
            {
                Name = name,
                Description = description,
                Price = price,
                Images = images ?? new List<IFormFile>()
            };

            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetProduct), new { id = result.ProductId }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(500, new { error = "An error occurred while creating the product" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto updateDto)
    {
        if (id <= 0)
        {
            return BadRequest(new { error = "Id must be a positive number" });
        }

        if (id != updateDto.ProductId)
        {
            return BadRequest(new { error = "Id in URL must match ProductId in body" });
        }

        try
        {
            var command = new UpdateProductCommand
            {
                ProductId = updateDto.ProductId,
                Name = updateDto.Name,
                Description = updateDto.Description,
                Price = updateDto.Price,
                ImageUrls = updateDto.ImageUrls
            };

            var product = await _mediator.Send(command);
            return Ok(product);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating product {id}");
            return StatusCode(500, new { error = "An error occurred while updating the product" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { error = "Id must be a positive number" });
        }

        try
        {
            var command = new DeleteProductCommand { ProductId = id };
            var deleted = await _mediator.Send(command);
            
            if (!deleted)
            {
                return NotFound(new { error = $"Product with id {id} not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting product {id}");
            return StatusCode(500, new { error = "An error occurred while deleting the product" });
        }
    }
}
