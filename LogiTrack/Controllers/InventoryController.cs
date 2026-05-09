using LogiTrack.Dto;
using LogiTrack.Helpers;
using LogiTrack.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogiTrack.Controllers
{
  
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

        [HttpGet]
        public async Task<IActionResult> GetAllInventoryItems()
        {
            var items = await _inventoryRepository.GetAllInventoryItemsAsync();
            return Ok(items);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInventoryItemById(int id)
        {
            var item = await _inventoryRepository.GetInventoryItemByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
                public async Task<IActionResult> AddInventoryItem([FromBody] InventoryItemDto itemDto)
        {
            await _inventoryRepository.AddInventoryItemAsync(itemDto);
            return CreatedAtAction(nameof(GetInventoryItemById), new { id = itemDto.ItemId }, itemDto);
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInventoryItem(int id, [FromBody] InventoryItemDto itemDto)
        {
            await _inventoryRepository.UpdateInventoryItemAsync(id, itemDto);
            return NoContent();
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInventoryItemById(int id)
        {
            await _inventoryRepository.DeleteInventoryItemAsync(id);
            return NoContent();
        }
    }
}