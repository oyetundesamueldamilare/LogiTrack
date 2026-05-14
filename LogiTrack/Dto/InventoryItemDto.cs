namespace LogiTrack.Dto
{
   
        public class InventoryItemDTO
        {
            public int ItemId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
        }
    }


