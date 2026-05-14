using LogiTrack.Data;
using LogiTrack.Interfaces;
using LogiTrack.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

namespace LogiTrack.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<InvoiceService> _logger;

        public InvoiceService(AppDbContext context, ILogger<InvoiceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task GenerateAndSendInvoiceAsync(int orderId)
        {
            // 1. Fetch the order with all necessary related data
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.InventoryItem)
                .Include(o => o.AppUser) // Required for the email address
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                throw new KeyNotFoundException($"Order {orderId} not found");

            if (order.AppUser == null || string.IsNullOrWhiteSpace(order.AppUser.Email))
                throw new InvalidOperationException($"Cannot send invoice: User email is missing for Order {orderId}");

            // 2. Generate PDF and get the file path
            var invoicePath = await GeneratePdfAsync(order);

            // 3. Send the email with the attachment
            await SendEmailAsync(order, invoicePath);
        }

        private async Task<string> GeneratePdfAsync(Order order)
        {
            var directoryPath = "Invoices";
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var invoicePath = Path.Combine(directoryPath, $"Invoice_{order.OrderId}.pdf");

            // NOTE: Using StreamWriter creates a text file with a .pdf extension. 
            // It will open in a browser, but it's not a true PDF. 
            // In the future, look into libraries like QuestPDF or iText7 for real PDF generation!
            using (var writer = new StreamWriter(invoicePath))
            {
                await writer.WriteLineAsync($"Invoice for Order {order.OrderId}");
                await writer.WriteLineAsync($"Customer: {order.AppUser.Email}");

                // Use the new DeliveryDate property, fallback to UTC Now if null
                await writer.WriteLineAsync($"Date: {order.DeliveryDate.ToString("g") ?? DateTime.UtcNow.ToString("g")}");

                await writer.WriteLineAsync("Items:");
                foreach (var item in order.OrderItems)
                {
                    var itemName = item.InventoryItem?.Name ?? "Unknown Item";
                    await writer.WriteLineAsync($"{itemName} x{item.Quantity} @ {item.UnitPrice:C}");
                }
                await writer.WriteLineAsync($"Total: {order.TotalPrice:C}");
            }

            return invoicePath;
        }

        private async Task SendEmailAsync(Order order, string invoicePath)
        {
            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress("psalmueldamilare@gmail.com");
                message.To.Add(order.AppUser.Email);
                message.Subject = $"Invoice for Order {order.OrderId}";
                message.Body = $"Dear Customer,\n\nPlease find attached your invoice for Order {order.OrderId}.\n\nThank you for shopping with us!\n\nLogiTrack Team";

                message.Attachments.Add(new Attachment(invoicePath));

                // BEST PRACTICE WARNING: Don't hardcode credentials in production.
                // Move "your-username" and "your-password" to appsettings.json or Azure KeyVault.
                using var smtp = new SmtpClient("smtp.yourserver.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("your-username", "your-password"),
                    EnableSsl = true
                };

                await smtp.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invoice email for Order {OrderId}", order.OrderId);
                throw;
            }
        }
    }
}