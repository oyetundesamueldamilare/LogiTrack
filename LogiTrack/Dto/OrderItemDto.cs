namespace LogiTrack.Dto
{
    public class OrderItemDto
    {
        public int OrderItemId { get; set; }
        public int ItemId { get; set; }   // FK to InventoryItem
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
         }
}
