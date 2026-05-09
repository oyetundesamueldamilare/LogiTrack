using LogiTrack.Dto;

namespace LogiTrack.Interfaces
{
    public interface IInventoryRepository
    {
        Task<IEnumerable<InventoryItemDto>> GetAllInventoryItemsAsync();
        Task<InventoryItemDto?> GetInventoryItemByIdAsync(int id);
        Task AddInventoryItemAsync(InventoryItemDto itemDto);
        Task UpdateInventoryItemAsync(int id, InventoryItemDto itemDto);
        Task DeleteInventoryItemAsync(int id);
    }
}
