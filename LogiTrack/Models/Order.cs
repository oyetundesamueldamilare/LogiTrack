namespace LogiTrack.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public AppUser? AppUser { get; set; } 
        public string AppUserId {  get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

}
