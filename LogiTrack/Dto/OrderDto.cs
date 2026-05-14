

namespace LogiTrack.Dto
{
    public class OrderDTO
    {
        public int OrderId { get; set; }
        public string AppUserId { get; set; } = string.Empty;  // reference to user
        public DateTime OrderDate { get; set; }
        public string? OrderStatus { get; set; } 
        public decimal TotalPrice { get; set; }
        public List<OrderItemDTO> Items { get; set; } = new List<OrderItemDTO>();

        public DateTime PaymentDate { get; set; }
    }
}

