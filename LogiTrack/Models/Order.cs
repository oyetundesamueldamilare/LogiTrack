namespace LogiTrack.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public AppUser? AppUser { get; set; }
        public string AppUserId { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime DeliveryDate { get; set; }
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public bool IsInvoiceSent { get; set; } = false;
    }

}
