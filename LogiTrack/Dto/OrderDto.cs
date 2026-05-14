using LogiTrack.Models;

namespace LogiTrack.DTOs
{
    public class OrderDTO
    {
        public int OrderId { get; set; }
        public string AppUserId { get; set; } = string.Empty;  // reference to user
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; } = string.Empty; // enum as string
        public decimal TotalPrice { get; set; }
        public List<OrderItemDTO> Items { get; set; } = new List<OrderItemDTO>();
    }
}

