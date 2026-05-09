using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using LogiTrack.Dto;
using LogiTrack.Interfaces;
using LogiTrack.Models;
using LogiTrack.Data;

namespace LogiTrack.Repository
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly ILogger _logger;
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;

        public InventoryRepository(ILogger<InventoryRepository> logger, AppDbContext context, IMemoryCache cache)
        {
            _logger = logger;
            _context = context;
            _cache = cache;
        }

        public async Task<IEnumerable<InventoryItemDto>> GetAllInventoryItemsAsync()
        {
            try
            {
                if (_cache.TryGetValue("AllInventoryItems", out IEnumerable<InventoryItemDto> cachedItems))
                {
                    return cachedItems;
                }

                var items = await _context.InventoryItems
                    .AsNoTracking() // Query optimization
                    .Select(item => new InventoryItemDto
                    {
                        ItemId = item.ItemId,
                        Name = item.Name,
                        Quantity = item.Quantity,
                        Location = item.Location,
                    }).ToListAsync();

                _cache.Set("AllInventoryItems", items, TimeSpan.FromMinutes(5));
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching inventory items");
                throw;
            }
        }

        public async Task<InventoryItemDto?> GetInventoryItemByIdAsync(int id)
        {
            try
            {
                var cacheKey = $"InventoryItem_{id}";
                if (_cache.TryGetValue(cacheKey, out InventoryItemDto cachedItem))
                {
                    return cachedItem;
                }

                var item = await _context.InventoryItems
                    .AsNoTracking() // Query optimization
                    .FirstOrDefaultAsync(i => i.ItemId == id);

                if (item == null) return null;

                var itemDto = new InventoryItemDto
                {
                    ItemId = item.ItemId,
                    Name = item.Name,
                    Quantity = item.Quantity,
                    Location = item.Location,
                };

                _cache.Set(cacheKey, itemDto, TimeSpan.FromMinutes(10));
                return itemDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching inventory item with ID {id}");
                throw;
            }
        }

        public async Task AddInventoryItemAsync(InventoryItemDto itemDto)
        {
            try
            {
                var item = new InventoryItem
                {
                    Name = itemDto.Name,
                    Quantity = itemDto.Quantity,
                    Location = itemDto.Location,
                };

                _context.InventoryItems.Add(item);
                await _context.SaveChangesAsync();

                // Invalidate cache
                _cache.Remove("AllInventoryItems");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding inventory item");
                throw;
            }
        }

        public async Task UpdateInventoryItemAsync(int id, InventoryItemDto itemDto)
        {
            try
            {
                var item = await _context.InventoryItems.FindAsync(id);
                if (item == null) throw new KeyNotFoundException($"Inventory item with ID {id} not found");

                item.Name = itemDto.Name;
                item.Quantity = itemDto.Quantity;
                item.Location = itemDto.Location;

                await _context.SaveChangesAsync();

                // Invalidate cache
                _cache.Remove("AllInventoryItems");
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

                // Invalidate cache
                _cache.Remove("AllInventoryItems");
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
