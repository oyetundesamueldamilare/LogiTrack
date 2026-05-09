namespace LogiTrack.Dto
{
    public class InventoryItemDto
    {
        public int ItemId { get; set; }
        public string? Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
