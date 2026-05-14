using LogiTrack.Models;

namespace LogiTrack.Interfaces
{
    public interface IInvoiceService
    {
        Task GenerateAndSendInvoiceAsync(int orderId);
    }
}