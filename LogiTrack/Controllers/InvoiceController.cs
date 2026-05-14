using LogiTrack.Data;
using LogiTrack.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Restricted to Admins for security
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly AppDbContext _context;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(IInvoiceService invoiceService, AppDbContext context, ILogger<InvoicesController> logger)
        {
            _invoiceService = invoiceService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Manual Resend: Ignores the 'IsInvoiceSent' flag. 
        /// Use this when a customer says they didn't receive the email.
        /// </summary>
        [HttpPost("resend/{orderId}")]
        public async Task<IActionResult> ResendInvoice(int orderId)
        {
            try
            {
                await _invoiceService.GenerateAndSendInvoiceAsync(orderId);
                return Ok(new { Message = $"Invoice for Order {orderId} resent successfully." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Order {orderId} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend invoice for Order {OrderId}", orderId);
                return StatusCode(500, "Internal server error during resend.");
            }
        }

        /// <summary>
        /// Status Check: Useful for an Admin Dashboard to see which 
        /// invoices are still waiting for the background worker.
        /// </summary>
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingInvoices()
        {
            var pendingCount = await _context.Orders
                .CountAsync(o => o.OrderStatus == Models.OrderStatus.Delivered && !o.IsInvoiceSent);

            return Ok(new { PendingInvoices = pendingCount });
        }

        /// <summary>
        /// Bulk Trigger: Manually forces a send of all pending invoices 
        /// instead of waiting for the 10-minute worker timer.
        /// </summary>
        [HttpPost("process-pending")]
        public async Task<IActionResult> ProcessPending()
        {
            var pendingOrders = await _context.Orders
                .Where(o => o.OrderStatus == Models.OrderStatus.Delivered && !o.IsInvoiceSent)
                .ToListAsync();

            if (!pendingOrders.Any()) return Ok("No pending invoices to process.");

            int successCount = 0;
            foreach (var order in pendingOrders)
            {
                try
                {
                    await _invoiceService.GenerateAndSendInvoiceAsync(order.OrderId);
                    order.IsInvoiceSent = true;
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing bulk invoice for Order {OrderId}", order.OrderId);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = $"Successfully processed {successCount} of {pendingOrders.Count} invoices." });
        }
    }
}