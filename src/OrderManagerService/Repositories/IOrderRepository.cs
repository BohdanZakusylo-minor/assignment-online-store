using OrderManagerService.Models;

namespace OrderManagerService.Repositories;

public interface IOrderRepository
{
    Task<List<Order>> GetAllAsync();
    Task<Order?> GetByIdAsync(int id);
    Task<List<Order>> GetByDestinationAsync(string destination);
    Task<List<Order>> GetShippedByProductsAsync(List<int> productsId);
    Task<List<Order>> GetNotShippedByProductsAsync(List<int> productsId);
    Task<Order> CreateAsync(Order order);
    Task<Order> UpdateAsync(Order order);
    Task<bool> DeleteAsync(int id);
}

