using LogiTrack.Dto;
using LogiTrack.Helpers;
using LogiTrack.Interfaces;
using LogiTrack.Models;
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
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderRepository orderRepository, ILogger<OrderController> logger)
        {
            _orderRepository = orderRepository;
            _logger = logger;
        }

        #region Helpers
        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        private string GetCurrentUserRole() => User.IsInRole("Admin") ? "Admin" : "Customer";
        #endregion

        #region Retrieval Operations

        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderRepository.GetAllOrdersAsync(GetCurrentUserRole(), GetCurrentUserId());
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            try
            {
                var order = await _orderRepository.GetOrderByIdAsync(id, GetCurrentUserRole(), GetCurrentUserId());
                if (order == null) return NotFound($"Order {id} not found.");
                return Ok(order);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        #endregion

        #region Create, Update, Delete Operations

        [HttpPost]
        public async Task<IActionResult> AddOrder([FromBody] OrderCreateDTO orderCreateDto)
        {
            if (orderCreateDto == null || !orderCreateDto.Items.Any())
                return BadRequest("Order must contain at least one item.");

            // Map incoming CreateDTO to the internal DTO the repository expects
            var orderDto = new OrderDTO
            {
                AppUserId = GetCurrentUserId(),
                OrderDate = orderCreateDto.OrderDate,
                Items = orderCreateDto.Items.Select(i => new OrderItemDTO
                {
                    ItemName = i.ItemName,
                    Quantity = i.Quantity
                }).ToList()
            };

            await _orderRepository.AddOrderAsync(orderDto);
            return CreatedAtAction(nameof(GetOrderById), new { id = orderDto.OrderId }, orderDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] OrderCreateDTO orderCreateDto)
        {
            try
            {
                var orderDto = new OrderDTO
                {
                    OrderDate = orderCreateDto.OrderDate,
                    Items = orderCreateDto.Items.Select(i => new OrderItemDTO
                    {
                        ItemName = i.ItemName,
                        Quantity = i.Quantity
                    }).ToList()
                };

                await _orderRepository.UpdateOrderAsync(id, orderDto, GetCurrentUserRole(), GetCurrentUserId());
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                await _orderRepository.DeleteOrderAsync(id, GetCurrentUserRole(), GetCurrentUserId());
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        #endregion

        #region Order Item Management

        [HttpPost("{orderId}/items")]
        public async Task<IActionResult> AddItemToOrder(int orderId, [FromBody] OrderItemCreateDTO itemCreateDto)
        {
            try
            {
                var itemDto = new OrderItemDTO
                {
                    ItemName = itemCreateDto.ItemName,
                    Quantity = itemCreateDto.Quantity
                };

                await _orderRepository.AddItemToOrderAsync(orderId, itemDto, GetCurrentUserRole(), GetCurrentUserId());
                return Ok(new { Message = $"Item {itemCreateDto.ItemName} added to order {orderId}" });
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpDelete("{orderId}/items/{itemName}")]
        public async Task<IActionResult> DeleteItemFromOrder(int orderId, string itemName)
        {
            try
            {
                await _orderRepository.DeleteItemFromOrderAsync(orderId, itemName, GetCurrentUserRole(), GetCurrentUserId());
                return Ok(new { Message = $"Item {itemName} removed from order {orderId}" });
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        #endregion

        #region Workflow Operations

        [HttpPost("{orderId}/pay")]
        public async Task<IActionResult> MakePayment(int orderId, [FromBody] decimal shippingFee)
        {
            try
            {
                await _orderRepository.MakePaymentAsync(orderId, shippingFee, GetCurrentUserId(), DateTime.UtcNow);
                return Ok(new { Message = "Payment successful. Total updated with shipping fee." });
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("autoship")]
        public async Task<IActionResult> AutoShipOrders()
        {
            await _orderRepository.AutoShipOrdersAsync(DateTime.UtcNow);
            return Ok(new { Message = "All eligible paid orders have been marked as Shipped." });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{orderId}/deliver")]
        public async Task<IActionResult> MarkAsDelivered(int orderId)
        {
            try
            {
                await _orderRepository.MarkAsDeliveredAsync(orderId, GetCurrentUserRole(), DateTime.UtcNow);
                return Ok(new { Message = "Order delivered. Background worker will now process the invoice." });
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        #endregion
    }
}