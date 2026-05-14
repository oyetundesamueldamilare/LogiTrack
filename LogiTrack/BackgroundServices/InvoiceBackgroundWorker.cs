using LogiTrack.Data;
using LogiTrack.Interfaces;
using LogiTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.BackgroundServices
{
    public class InvoiceBackgroundWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InvoiceBackgroundWorker> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(10); // Runs every 10 mins

        public InvoiceBackgroundWorker(IServiceProvider serviceProvider, ILogger<InvoiceBackgroundWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Invoice Background Worker is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Checking for unsent invoices...");
                    await DoWork(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred in the Invoice Background Worker.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            // BackgroundServices are Singletons, but DbContext is Scoped. 
            // We must create a new scope to access the database.
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();

                // 1. Find orders that are delivered but haven't had an invoice sent
                var pendingOrders = await context.Orders
                    .Where(o => o.OrderStatus == OrderStatus.Delivered && !o.IsInvoiceSent)
                    .ToListAsync(stoppingToken);

                if (!pendingOrders.Any())
                {
                    _logger.LogInformation("No pending invoices found.");
                    return;
                }

                _logger.LogInformation("Found {Count} pending invoices to send.", pendingOrders.Count);

                foreach (var order in pendingOrders)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    try
                    {
                        // 2. Call the service we built earlier
                        await invoiceService.GenerateAndSendInvoiceAsync(order.OrderId);

                        // 3. Mark as sent and save
                        order.IsInvoiceSent = true;
                        await context.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation("Successfully sent invoice for Order {OrderId}", order.OrderId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send invoice for Order {OrderId}", order.OrderId);
                        // We don't throw here so the loop can try the next order
                    }
                }
            }
        }
    }
}