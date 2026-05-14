using LogiTrack.Dto;
using LogiTrack.DTOs;

namespace LogiTrack.Interfaces
{
    public interface IInventoryRepository
    {
        // Get all inventory items
        Task<IEnumerable<InventoryItemDTO>> GetAllInventoryItemsAsync();

        // Get a single inventory item by ID
        Task<InventoryItemDTO?> GetInventoryItemByIdAsync(int id);

        // Add a new inventory item
        Task AddInventoryItemAsync(InventoryItemDTO itemDto);

        // Update an existing inventory item
        Task UpdateInventoryItemAsync(int id, InventoryItemDTO itemDto);

        // Delete an inventory item
        Task DeleteInventoryItemAsync(int id);
    }
}
