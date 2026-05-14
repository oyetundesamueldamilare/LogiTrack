using LogiTrack.Data;
using LogiTrack.Dto;
using LogiTrack.Interfaces;
using LogiTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LogiTrack.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<OrderRepository> _logger;

        public OrderRepository(AppDbContext context, IMemoryCache cache, ILogger<OrderRepository> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        #region Retrieval Operations

        public async Task<IEnumerable<OrderDTO>> GetAllOrdersAsync(string role, string currentUserId)
        {
            IQueryable<Order> query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.InventoryItem);

            if (!role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(o => o.AppUserId == currentUserId);
            }

            var orders = await query.ToListAsync();
            return orders.Select(MapToDto);
        }

        public async Task<OrderDTO?> GetOrderByIdAsync(int id, string role, string currentUserId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.InventoryItem)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return null;

            if (!role.Equals("Admin", StringComparison.OrdinalIgnoreCase) && order.AppUserId != currentUserId)
                throw new UnauthorizedAccessException("You do not have permission to view this order.");

            return MapToDto(order);
        }

        #endregion

        #region Create, Update, Delete Operations

        public async Task AddOrderAsync(OrderDTO orderDto)
        {
            // 1. Initialize the Domain Model
            var order = new Order
            {
                AppUserId = orderDto.AppUserId,
                OrderDate = DateTime.UtcNow,
                OrderStatus = OrderStatus.Pending,
                OrderItems = new List<OrderItem>()
            };

            // 2. Resolve Items and Lock Prices
            foreach (var itemDto in orderDto.Items)
            {
                var inventoryItem = await _context.InventoryItems
                    .FirstOrDefaultAsync(i => i.Name == itemDto.ItemName);

                if (inventoryItem == null)
                    throw new KeyNotFoundException($"Product '{itemDto.ItemName}' not found in inventory.");

                order.OrderItems.Add(new OrderItem
                {
                    ItemId = inventoryItem.ItemId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = inventoryItem.UnitPrice // Use current DB price for security
                });
            }

            // 3. Final Total Calculation
            order.TotalPrice = order.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            InvalidateOrderCaches(order.AppUserId, order.OrderId);
        }

        public async Task UpdateOrderAsync(int id, OrderDTO orderDto, string role, string currentUserId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) throw new KeyNotFoundException("Order not found");

            if (!role.Equals("Admin", StringComparison.OrdinalIgnoreCase) && order.AppUserId != currentUserId)
                throw new UnauthorizedAccessException();

            // We only allow updating the date or items from the DTO; Status and Price are handled via Workflow
            order.OrderDate = orderDto.OrderDate;

            // Logic for updating items can be complex (add/remove/update quantity)
            // For now, we update the basic order metadata
            await _context.SaveChangesAsync();
            InvalidateOrderCaches(order.AppUserId, id);
        }

        public async Task DeleteOrderAsync(int id, string role, string currentUserId)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) throw new KeyNotFoundException("Order not found");

            if (!role.Equals("Admin", StringComparison.OrdinalIgnoreCase) && order.AppUserId != currentUserId)
                throw new UnauthorizedAccessException();

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            InvalidateOrderCaches(order.AppUserId, id);
        }

        #endregion

        #region Order Item Management

        public async Task AddItemToOrderAsync(int orderId, OrderItemDTO itemDto, string role, string currentUserId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) throw new KeyNotFoundException("Order not found");

            if (!role.Equals("Admin", StringComparison.OrdinalIgnoreCase) && order.AppUserId != currentUserId)
                throw new UnauthorizedAccessException();

            var inventoryItem = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.Name == itemDto.ItemName);

            if (inventoryItem == null) throw new KeyNotFoundException("Inventory item not found.");

            var newItem = new OrderItem
            {
                OrderId = orderId,
                ItemId = inventoryItem.ItemId,
                Quantity = itemDto.Quantity,
                UnitPrice = inventoryItem.UnitPrice
            };

            order.OrderItems.Add(newItem);
            order.TotalPrice = order.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice);

            await _context.SaveChangesAsync();
            InvalidateOrderCaches(order.AppUserId, orderId);
        }

        public async Task DeleteItemFromOrderAsync(int orderId, string itemName, string role, string currentUserId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.InventoryItem)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) throw new KeyNotFoundException("Order not found");

            if (!role.Equals("Admin", StringComparison.OrdinalIgnoreCase) && order.AppUserId != currentUserId)
                throw new UnauthorizedAccessException();

            var item = order.OrderItems.FirstOrDefault(oi => oi.InventoryItem.Name == itemName);
            if (item == null) throw new KeyNotFoundException("Item not found in order");

            _context.OrderItems.Remove(item);
            order.TotalPrice = order.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice);

            await _context.SaveChangesAsync();
            InvalidateOrderCaches(order.AppUserId, orderId);
        }

        #endregion

        #region Workflow Operations

        public async Task MakePaymentAsync(int orderId, decimal shippingFee, string userId, DateTime paymentDate)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId && o.AppUserId == userId);
            if (order == null) throw new KeyNotFoundException("Order not found.");

            order.OrderStatus = OrderStatus.PaymentConfirmed;
            order.PaymentDate = paymentDate;
            order.TotalPrice += shippingFee;

            await _context.SaveChangesAsync();
            InvalidateOrderCaches(userId, orderId);
        }

        public async Task AutoShipOrdersAsync(DateTime paymentDate)
        {
            var ordersToShip = await _context.Orders
                .Where(o => o.OrderStatus == OrderStatus.PaymentConfirmed && o.PaymentDate <= paymentDate)
                .ToListAsync();

            foreach (var order in ordersToShip)
            {
                order.OrderStatus = OrderStatus.Shipped;
            }

            await _context.SaveChangesAsync();
        }

        public async Task MarkAsDeliveredAsync(int orderId, string role, DateTime deliveryDate)
        {
            if (!role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Only admins can mark orders as delivered.");

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) throw new KeyNotFoundException("Order not found");

            order.OrderStatus = OrderStatus.Delivered;
            order.DeliveryDate = deliveryDate;

            await _context.SaveChangesAsync();
            InvalidateOrderCaches(order.AppUserId, orderId);
        }

        #endregion

        #region Private Helpers

        private void InvalidateOrderCaches(string userId, int orderId)
        {
            _cache.Remove($"orders_user_{userId}");
            _cache.Remove($"order_details_{orderId}");
            _logger.LogInformation("Cache invalidated for User {UserId} and Order {OrderId}", userId, orderId);
        }

        private static OrderDTO MapToDto(Order order) =>
             new OrderDTO
             {
                 OrderId = order.OrderId,
                 AppUserId = order.AppUserId,
                 OrderDate = order.OrderDate,
                 OrderStatus = order.OrderStatus.ToString(),
                 TotalPrice = order.TotalPrice,
                 Items = order.OrderItems.Select(oi => new OrderItemDTO
                 {
                     OrderItemId = oi.OrderItemId,
                     ItemName = oi.InventoryItem?.Name ?? "Unknown Item",
                     Quantity = oi.Quantity,
                     UnitPrice = oi.UnitPrice
                 }).ToList()
             };
        #endregion
    }
}