using Microsoft.EntityFrameworkCore;
using OrderManagerService.Models;

namespace OrderManagerService.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _context.Orders.ToListAsync();
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders.FindAsync(id);
    }

    public async Task<List<Order>> GetByDestinationAsync(string destination)
    {
        return await _context.Orders
            .Where(o => o.Destination.Contains(destination))
            .ToListAsync();
    }

    public async Task<List<Order>> GetShippedByProductsAsync(List<int> productsId)
    {
        return await _context.Orders
            .Where(o => o.ShipmentDate.HasValue && 
                       o.ProductsId.Any(p => productsId.Contains(p)))
            .ToListAsync();
    }

    public async Task<List<Order>> GetNotShippedByProductsAsync(List<int> productsId)
    {
        return await _context.Orders
            .Where(o => !o.ShipmentDate.HasValue && 
                       o.ProductsId.Any(p => productsId.Contains(p)))
            .ToListAsync();
    }

    public async Task<Order> CreateAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<Order> UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            return false;
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return true;
    }
}

