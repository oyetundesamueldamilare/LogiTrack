using LogiTrack.DTOs;

namespace LogiTrack.Interfaces
{
    public interface IOrderRepository
    {
        Task AddItemToOrderAsync(int orderId, OrderItemDTO itemDto, string role, string currentUserId);
        // Admin: all orders; Customer: only their own
        Task<IEnumerable<OrderDTO>> GetAllOrdersAsync(string role, string currentUserId);

        // Admin: any order; Customer: only their own
        Task<OrderDTO?> GetOrderByIdAsync(int id, string role, string currentUserId);

        // Add new order (role check handled in service/controller)
        Task AddOrderAsync(OrderDTO orderDto);

        // Admin: any order; Customer: only their own
        Task UpdateOrderAsync(int id, OrderDTO orderDto, string role, string currentUserId);

        // Admin: any order; Customer: only their own
        Task DeleteOrderAsync(int id, string role, string currentUserId);
    }
}

