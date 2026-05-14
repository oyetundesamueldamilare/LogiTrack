using LogiTrack.Dto;
using LogiTrack.DTOs;
using LogiTrack.Helpers;
using LogiTrack.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogiTrack.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [ExecutionTime]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryRepository _inventoryRepository;

        public InventoryController(IInventoryRepository inventoryRepository)
        {
            _inventoryRepository = inventoryRepository;
        }

        // Customers & Admins can view all items
        [HttpGet]
        public async Task<IActionResult> GetAllInventoryItems()
        {
            var items = await _inventoryRepository.GetAllInventoryItemsAsync();
            return Ok(items);
        }

        // Customers & Admins can view a single item
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInventoryItemById(int id)
        {
            var item = await _inventoryRepository.GetInventoryItemByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        // Only Admins can add new items
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddInventoryItem([FromBody] InventoryItemDTO itemDto)
        {
            await _inventoryRepository.AddInventoryItemAsync(itemDto);
            return CreatedAtAction(nameof(GetInventoryItemById), new { id = itemDto.ItemId }, itemDto);
        }

        // Only Admins can update items
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInventoryItem(int id, [FromBody] InventoryItemDTO itemDto)
        {
            await _inventoryRepository.UpdateInventoryItemAsync(id, itemDto);
            return NoContent();
        }

        // Only Admins can delete items
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInventoryItemById(int id)
        {
            await _inventoryRepository.DeleteInventoryItemAsync(id);
            return NoContent();
        }
    }
}
