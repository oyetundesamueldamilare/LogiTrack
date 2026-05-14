using LogiTrack.Dto;

namespace LogiTrack.Interfaces
{
    public interface IOrderRepository
    {
        // Retrieval Operations
        Task<IEnumerable<OrderDTO>> GetAllOrdersAsync(string role, string currentUserId);
        Task<OrderDTO?> GetOrderByIdAsync(int id, string role, string currentUserId);

        // Create, Update, Delete Operations
        Task AddOrderAsync(OrderDTO orderDto);
        Task UpdateOrderAsync(int id, OrderDTO orderDto, string role, string currentUserId);
        Task DeleteOrderAsync(int id, string role, string currentUserId);

        // Order Item Management
        Task AddItemToOrderAsync(int orderId, OrderItemDTO itemDto, string role, string currentUserId);
        Task DeleteItemFromOrderAsync(int orderId, string itemName, string role, string currentUserId);

        // Workflow & Status Operations
        Task MakePaymentAsync(int orderId, decimal shippingFee, string userId, DateTime paymentDate);
        Task AutoShipOrdersAsync(DateTime paymentDate);
        Task MarkAsDeliveredAsync(int orderId, string role, DateTime deliveryDate);
    }
}