namespace LogiTrack.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; }    = string.Empty;
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    }
}
