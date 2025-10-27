using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewService.Models;

namespace ReviewService.Controllers;

[ApiController]
[Route("[controller]")]
public class ReviewController : ControllerBase
{
    private readonly ReviewDbContext _context;

    public ReviewController(ReviewDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetReviews()
    {
        var reviews = await _context.Reviews.ToListAsync();
        return Ok(reviews);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetReview(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { error = "Id must be a positive number" });
        }

        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
        {
            return NotFound(new { error = $"Review with id {id} not found" });
        }
        return Ok(review);
    }

    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] Review review)
    {

        if (review.Rating < 1 || review.Rating > 5)
        {
            return BadRequest(new { error = "Rating must be between 1 and 5" });
        }

        if (!string.IsNullOrEmpty(review.Comment) && review.Comment.Length > 1000)
        {
            return BadRequest(new { error = "Comment cannot exceed 1000 characters" });
        }

        review.CreatedAt = DateTime.UtcNow;
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
        {
            return NotFound();
        }

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}