using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrack.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }   // PK

        // Foreign key to Order
        public int OrderId { get; set; }
        [ForeignKey(nameof(OrderId))]
        public Order? Order { get; set; }

        // Foreign key to InventoryItem
        public int ItemId { get; set; }
        [ForeignKey(nameof(ItemId))]
        public InventoryItem InventoryItem { get; set; } = null!;

        public int Quantity { get; set; }   // Quantity ordered
        public decimal UnitPrice { get; set; } // snapshot price at order time
    }
}
