namespace LogiTrack.Dto
{
    public class OrderItemDTO
    {
        public int OrderItemId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;   // optional convenience
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }                 // snapshot of price at order time
    }
}
