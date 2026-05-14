namespace LogiTrack.Models
{
    public enum OrderStatus
    {
        Pending = 0,
        Processing,
        Shipped,
        Delivered,
        Cancelled,
        PaymentConfirmed
    }
}
