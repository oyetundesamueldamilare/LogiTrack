namespace LogiTrack.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }   // PK
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int ItemId { get; set; }
        public InventoryItem InventoryItem { get; set; }     = null!;

        public int Quantity { get; set; }   // Quantity ordered
    }
}
