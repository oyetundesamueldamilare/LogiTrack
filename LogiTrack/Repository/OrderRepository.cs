using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using LogiTrack.DTOs;
using LogiTrack.Interfaces;
using LogiTrack.Models;
using LogiTrack.Data;

namespace LogiTrack.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ILogger _logger;
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;

        public OrderRepository(ILogger<OrderRepository> logger, AppDbContext context, IMemoryCache cache)
        {
            _logger = logger;
            _context = context;
            _cache = cache;
        }

        private static OrderDTO MapToDto(Order order) =>
            new OrderDTO
            {
                OrderId = order.OrderId,
                AppUserId = order.AppUserId,
                OrderDate = order.OrderDate,
                OrderStatus = order.OrderStatus.ToString(),
                TotalPrice = order.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice),
                Items = order.OrderItems.Select(oi => new OrderItemDTO
                {
                    OrderItemId = oi.OrderItemId,
                    ItemId = oi.ItemId,
                    ItemName = oi.InventoryItem?.Name ?? string.Empty,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };

        public async Task<IEnumerable<OrderDTO>> GetAllOrdersAsync(string role, string currentUserId)
        {
            try
            {
                var cacheKey = $"AllOrders_{role}_{currentUserId}";
                if (_cache.TryGetValue(cacheKey, out IEnumerable<OrderDTO> cachedOrders))
                {
                    return cachedOrders;
                }

                IQueryable<Order> query = _context.Orders
                    .AsNoTracking()
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.InventoryItem);

                if (role.Equals("Customer", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(o => o.AppUserId == currentUserId);
                }

                var orders = await query.ToListAsync();
                var orderDtos = orders.Select(MapToDto).ToList();

                _cache.Set(cacheKey, orderDtos, TimeSpan.FromMinutes(5));
                return orderDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching orders");
                throw;
            }
        }

        public async Task<OrderDTO?> GetOrderByIdAsync(int id, string role, string currentUserId)
        {
            try
            {
                var cacheKey = $"Order_{id}_{role}_{currentUserId}";
                if (_cache.TryGetValue(cacheKey, out OrderDTO cachedOrder))
                {
                    return cachedOrder;
                }

                IQueryable<Order> query = _context.Orders
                    .AsNoTracking()
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.InventoryItem)
                    .Where(o => o.OrderId == id);

                if (role.Equals("Customer", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(o => o.AppUserId == currentUserId);
                }

                var order = await query.FirstOrDefaultAsync();
                if (order == null) return null;

                var orderDto = MapToDto(order);
                _cache.Set(cacheKey, orderDto, TimeSpan.FromMinutes(10));
                return orderDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching order with ID {id}");
                throw;
            }
        }

        public async Task AddOrderAsync(OrderDTO orderDto)
        {
            try
            {
                var orderItems = new List<OrderItem>();
                foreach (var i in orderDto.Items)
                {
                    var inventoryItem = await _context.InventoryItems
                        .FirstOrDefaultAsync(ii => ii.Name == i.ItemName);

                    if (inventoryItem == null)
                        throw new KeyNotFoundException($"Inventory item '{i.ItemName}' not found");

                    orderItems.Add(new OrderItem
                    {
                        ItemId = inventoryItem.ItemId,
                        Quantity = i.Quantity,
                        UnitPrice = inventoryItem.UnitPrice
                    });
                }

                var order = new Order
                {
                    AppUserId = orderDto.AppUserId,
                    OrderDate = orderDto.OrderDate,
                    OrderStatus = Enum.TryParse<OrderStatus>(orderDto.OrderStatus, out var status) ? status : OrderStatus.Pending,
                    OrderItems = orderItems,
                    TotalPrice = orderItems.Sum(oi => oi.Quantity * oi.UnitPrice)
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                _cache.Remove("AllOrders_Admin_all");
                _cache.Remove($"AllOrders_Customer_{order.AppUserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding order");
                throw;
            }
        }

        public async Task AddItemToOrderAsync(int orderId, OrderItemDTO itemDto, string role, string currentUserId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) throw new KeyNotFoundException($"Order {orderId} not found");

            if (role.Equals("Customer", StringComparison.OrdinalIgnoreCase) && order.AppUserId != currentUserId)
                throw new UnauthorizedAccessException("Customers can only modify their own orders");

            var inventoryItem = await _context.InventoryItems
                .FirstOrDefaultAsync(ii => ii.Name == itemDto.ItemName);

            if (inventoryItem == null)
                throw new KeyNotFoundException($"Inventory item '{itemDto.ItemName}' not found");

            order.OrderItems.Add(new OrderItem
            {
                ItemId = inventoryItem.ItemId,
                Quantity = itemDto.Quantity,
                UnitPrice = inventoryItem.UnitPrice
            });

            order.TotalPrice = order.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice);

            await _context.SaveChangesAsync();
        }


        public async Task UpdateOrderAsync(int id, OrderDTO orderDto, string role, string currentUserId)
        {
            try
            {
                var existingOrder = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (existingOrder == null)
                    throw new KeyNotFoundException($"Order with ID {id} not found");

                if (role.Equals("Customer", StringComparison.OrdinalIgnoreCase) && existingOrder.AppUserId != currentUserId)
                    throw new UnauthorizedAccessException("Customers can only update their own orders");

                existingOrder.OrderDate = orderDto.OrderDate;
                existingOrder.OrderStatus = Enum.TryParse<OrderStatus>(orderDto.OrderStatus, out var status) ? status : existingOrder.OrderStatus;

                existingOrder.OrderItems.Clear();
                foreach (var i in orderDto.Items)
                {
                    var inventoryItem = await _context.InventoryItems
                        .FirstOrDefaultAsync(ii => ii.Name == i.ItemName);

                    if (inventoryItem == null)
                        throw new KeyNotFoundException($"Inventory item '{i.ItemName}' not found");

                    existingOrder.OrderItems.Add(new OrderItem
                    {
                        ItemId = inventoryItem.ItemId,
                        Quantity = i.Quantity,
                        UnitPrice = inventoryItem.UnitPrice
                    });
                }

                existingOrder.TotalPrice = existingOrder.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice);

                await _context.SaveChangesAsync();

                _cache.Remove("AllOrders_Admin_all");
                _cache.Remove($"AllOrders_Customer_{existingOrder.AppUserId}");
                _cache.Remove($"Order_{id}_Admin_all");
                _cache.Remove($"Order_{id}_Customer_{existingOrder.AppUserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating order with ID {id}");
                throw;
            }
        }

        public async Task DeleteOrderAsync(int id, string role, string currentUserId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null) throw new KeyNotFoundException($"Order with ID {id} not found");

                if (role.Equals("Customer", StringComparison.OrdinalIgnoreCase) && order.AppUserId != currentUserId)
                    throw new UnauthorizedAccessException("Customers can only delete their own orders");

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                _cache.Remove("AllOrders_Admin_all");
                _cache.Remove($"AllOrders_Customer_{order.AppUserId}");
                _cache.Remove($"Order_{id}_Admin_all");
                _cache.Remove($"Order_{id}_Customer_{order.AppUserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting order with ID {id}");
                throw;
            }
        }
    }
}
