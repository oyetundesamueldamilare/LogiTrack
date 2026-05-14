using LogiTrack.Dto;
using LogiTrack.DTOs;
using LogiTrack.Helpers;
using LogiTrack.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LogiTrack.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [ExecutionTime]
    public class OrderController : ControllerBase
    {
        private readonly IOrderRepository _orderRepository;

        public OrderController(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        private string GetCurrentUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        private string GetCurrentUserRole()
        {
            if (User.IsInRole("Admin")) return "Admin";
            return "Customer";
        }

        // Customers see only their orders; Admins see all
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            var role = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            var orders = await _orderRepository.GetAllOrdersAsync(role, userId);
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var role = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            var order = await _orderRepository.GetOrderByIdAsync(id, role, userId);
            if (order == null) return NotFound();
            return Ok(order);
        }

        // Checkout flow: create a new order with items
        [HttpPost]
        public async Task<IActionResult> AddOrder([FromBody] OrderCreateDTO orderCreateDto)
        {
            var orderDto = new OrderDTO
            {
                AppUserId = GetCurrentUserId(),
                OrderDate = orderCreateDto.OrderDate,
                OrderStatus = "Pending",
                Items = orderCreateDto.Items.Select(i => new OrderItemDTO
                {
                    ItemName = i.ItemName,
                    Quantity = i.Quantity
                }).ToList()
            };

            await _orderRepository.AddOrderAsync(orderDto);

            return CreatedAtAction(nameof(GetOrderById), new { id = orderDto.OrderId }, orderDto);
        }

        // Cart flow: add item to existing order
        [HttpPost("{orderId}/items")]
        public async Task<IActionResult> AddItemToOrder(int orderId, [FromBody] OrderItemCreateDTO itemCreateDto)
        {
            var role = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            var itemDto = new OrderItemDTO
            {
                ItemName = itemCreateDto.ItemName,
                Quantity = itemCreateDto.Quantity
            };

            await _orderRepository.AddItemToOrderAsync(orderId, itemDto, role, userId);
            return Ok($"Item {itemCreateDto.ItemName} added to order {orderId}");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] OrderCreateDTO orderCreateDto)
        {
            var role = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            var orderDto = new OrderDTO
            {
                AppUserId = GetCurrentUserId(),
                OrderDate = orderCreateDto.OrderDate,
                OrderStatus = "Pending",
                Items = orderCreateDto.Items.Select(i => new OrderItemDTO
                {
                    ItemName = i.ItemName,
                    Quantity = i.Quantity
                }).ToList()
            };

            await _orderRepository.UpdateOrderAsync(id, orderDto, role, userId);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var role = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            await _orderRepository.DeleteOrderAsync(id, role, userId);
            return NoContent();
        }
    }
}
