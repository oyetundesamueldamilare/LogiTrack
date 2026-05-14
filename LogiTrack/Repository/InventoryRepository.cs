using LogiTrack.Data;
using LogiTrack.Dto;
using LogiTrack.Interfaces;
using LogiTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LogiTrack.Repository
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly ILogger _logger;
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;

        private const string AllItemsCacheKey = "AllInventoryItems";

        public InventoryRepository(ILogger<InventoryRepository> logger, AppDbContext context, IMemoryCache cache)
        {
            _logger = logger;
            _context = context;
            _cache = cache;
        }

        private static InventoryItemDTO MapToDto(InventoryItem item) =>
            new InventoryItemDTO
            {
                ItemId = item.ItemId,
                Name = item.Name,
                Quantity = item.Quantity,
                Location = item.Location,
                UnitPrice = item.UnitPrice
            };

        public async Task<IEnumerable<InventoryItemDTO>> GetAllInventoryItemsAsync()
        {
            try
            {
                if (_cache.TryGetValue(AllItemsCacheKey, out IEnumerable<InventoryItemDTO> cachedItems))
                {
                    return cachedItems;
                }

                var items = await _context.InventoryItems
                    .AsNoTracking()
                    .ToListAsync();

                var itemDtos = items.Select(MapToDto).ToList();

                _cache.Set(AllItemsCacheKey, itemDtos, TimeSpan.FromMinutes(5));
                return itemDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching inventory items");
                throw;
            }
        }

        public async Task<InventoryItemDTO?> GetInventoryItemByIdAsync(int id)
        {
            try
            {
                var cacheKey = $"InventoryItem_{id}";
                if (_cache.TryGetValue(cacheKey, out InventoryItemDTO cachedItem))
                {
                    return cachedItem;
                }

                var item = await _context.InventoryItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.ItemId == id);

                if (item == null) return null;

                var itemDto = MapToDto(item);

                _cache.Set(cacheKey, itemDto, TimeSpan.FromMinutes(10));
                return itemDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching inventory item with ID {id}");
                throw;
            }
        }

        public async Task AddInventoryItemAsync(InventoryItemDTO itemDto)
        {
            try
            {
                var item = new InventoryItem
                {
                    Name = itemDto.Name,
                    Quantity = itemDto.Quantity,
                    Location = itemDto.Location,
                    UnitPrice = itemDto.UnitPrice
                };

                _context.InventoryItems.Add(item);
                await _context.SaveChangesAsync();

                _cache.Remove(AllItemsCacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding inventory item");
                throw;
            }
        }

        public async Task UpdateInventoryItemAsync(int id, InventoryItemDTO itemDto)
        {
            try
            {
                var item = await _context.InventoryItems.FindAsync(id);
                if (item == null) throw new KeyNotFoundException($"Inventory item with ID {id} not found");

                item.Name = itemDto.Name;
                item.Quantity = itemDto.Quantity;
                item.Location = itemDto.Location;
                item.UnitPrice = itemDto.UnitPrice;

                await _context.SaveChangesAsync();

                _cache.Remove(AllItemsCacheKey);
                _cache.Remove($"InventoryItem_{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating inventory item with ID {id}");
                throw;
            }
        }

        public async Task DeleteInventoryItemAsync(int id)
        {
            try
            {
                var item = await _context.InventoryItems.FindAsync(id);
                if (item == null) throw new KeyNotFoundException($"Inventory item with ID {id} not found");

                _context.InventoryItems.Remove(item);
                await _context.SaveChangesAsync();

                _cache.Remove(AllItemsCacheKey);
                _cache.Remove($"InventoryItem_{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting inventory item with ID {id}");
                throw;
            }
        }
    }
}
