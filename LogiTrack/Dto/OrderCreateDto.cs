namespace LogiTrack.Dto
{
    // For input (request body)
    public class OrderCreateDTO
    {
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public List<OrderItemCreateDTO> Items { get; set; } = new();
    }

    public class OrderItemCreateDTO
    {
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

}
