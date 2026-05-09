using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using LogiTrack.Dto;
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

        public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
        {
            try
            {
                if (_cache.TryGetValue("AllOrders", out IEnumerable<OrderDto> cachedOrders))
                {
                    return cachedOrders;
                }

                // Step 1: Load entities with includes
                var orders = await _context.Orders
                    .AsNoTracking()
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.InventoryItem)
                    .ToListAsync();

                // Step 2: Map to DTOs in memory
                var orderDtos = orders.Select(order => new OrderDto
                {
                    OrderId = order.OrderId,
                    CustomerName = order.CustomerName,
                    OrderDate = order.OrderDate,
                    OrderStatus = order.OrderStatus,
                    OrderItems = (order.OrderItems ?? Enumerable.Empty<OrderItem>())
                        .Select(oi => new OrderItemDto
                        {
                            OrderItemId = oi.OrderItemId,
                            ItemId = oi.ItemId,
                            ItemName = oi.InventoryItem?.Name ?? string.Empty,
                            Quantity = oi.Quantity
                        }).ToList()
                }).ToList();

                _cache.Set("AllOrders", orderDtos, TimeSpan.FromMinutes(5));
                return orderDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching orders");
                throw;
            }
        }


        public async Task<OrderDto?> GetOrderByIdAsync(int id)
        {
            try
            {
                var cacheKey = $"Order_{id}";
                if (_cache.TryGetValue(cacheKey, out OrderDto cachedOrder))
                {
                    return cachedOrder;
                }

                var order = await _context.Orders
                    .AsNoTracking() // Query optimization
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.InventoryItem)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null) return null;

                var orderDto = new OrderDto
                {
                    OrderId = order.OrderId,
                    CustomerName = order.CustomerName,
                    OrderDate = order.OrderDate,
                    OrderStatus = order.OrderStatus,
                    OrderItems = (order.OrderItems ?? Enumerable.Empty<OrderItem>())
                        .Select(oi => new OrderItemDto
                        {
                            OrderItemId = oi.OrderItemId,
                            ItemId = oi.ItemId,
                            ItemName = oi.InventoryItem?.Name ?? string.Empty,
                            Quantity = oi.Quantity,
                        }).ToList()
                };

                _cache.Set(cacheKey, orderDto, TimeSpan.FromMinutes(10));
                return orderDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching order with ID {id}");
                throw;
            }
        }

        public async Task AddOrderAsync(OrderDto orderDto)
        {
            try
            {
                var order = new Order
                {
                    CustomerName = orderDto.CustomerName,
                    OrderDate = orderDto.OrderDate,
                    OrderStatus = orderDto.OrderStatus,
                    OrderItems = (orderDto.OrderItems ?? Enumerable.Empty<OrderItemDto>())
                        .Select(oi => new OrderItem
                        {
                            ItemId = oi.ItemId,
                            Quantity = oi.Quantity
                        }).ToList()
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Invalidate cache
                _cache.Remove("AllOrders");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding order");
                throw;
            }
        }

        public async Task UpdateOrderAsync(int id, OrderDto orderDto)
        {
            try
            {
                var existingOrder = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (existingOrder == null)
                    throw new KeyNotFoundException($"Order with ID {id} not found");

                existingOrder.CustomerName = orderDto.CustomerName;
                existingOrder.OrderDate = orderDto.OrderDate;
                existingOrder.OrderStatus = orderDto.OrderStatus;

                if (existingOrder.OrderItems != null)
                    existingOrder.OrderItems.Clear();

                existingOrder.OrderItems = (orderDto.OrderItems ?? Enumerable.Empty<OrderItemDto>())
                    .Select(oi => new OrderItem
                    {
                        ItemId = oi.ItemId,
                        Quantity = oi.Quantity
                    }).ToList();

                _context.Orders.Update(existingOrder);
                await _context.SaveChangesAsync();

                // Invalidate cache
                _cache.Remove("AllOrders");
                _cache.Remove($"Order_{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating order with ID {id}");
                throw;
            }
        }

        public async Task DeleteOrderAsync(int id)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null) throw new KeyNotFoundException($"Order with ID {id} not found");

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                // Invalidate cache
                _cache.Remove("AllOrders");
                _cache.Remove($"Order_{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting order with ID {id}");
                throw;
            }
        }
    }
}
