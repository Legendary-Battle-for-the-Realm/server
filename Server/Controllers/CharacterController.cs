using Microsoft.AspNetCore.Mvc;
using Server.Services;
using Shared.Models;
using Microsoft.AspNetCore.Authorization;

namespace Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CharacterController : ControllerBase
    {
        private readonly CharacterService _characterService;
        private readonly DataSyncService _dataSyncService;
        private readonly CardSyncService _cardSyncService;
        public CharacterController(CharacterService characterService, DataSyncService dataSyncService, CardSyncService cardSyncService)
        {
            _characterService = characterService;
            _dataSyncService = dataSyncService;
            _cardSyncService = cardSyncService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var characters = await _characterService.GetAllCharactersAsync();
            return Ok(characters);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var character = await _characterService.GetCharacterByIdAsync(id);
            if (character == null) return NotFound();
            return Ok(character);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Character character)
        {
            var created = await _characterService.CreateCharacterAsync(character);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Character character)
        {
            var updated = await _characterService.UpdateCharacterAsync(id, character);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _characterService.DeleteCharacterAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }
        // [HttpPost("sync")]
        // public async Task<IActionResult> SyncCharacters()
        // {
        //     try
        //     {
        //         await _dataSyncService.SyncCharactersFromJsonAsync();
        //         return Ok("Characters synced successfully.");
        //     }
        //     catch (Exception ex)
        //     {
        //         return BadRequest($"Error syncing characters: {ex.Message}");
        //     }
        // }
        // [HttpPost("sync-cards")]
        // public async Task<IActionResult> SyncCards()
        // {
        //     try
        //     {
        //         await _cardSyncService.SyncCardsFromJsonAsync();
        //         return Ok("Đồng bộ thẻ thành công.");
        //     }
        //     catch (Exception ex)
        //     {
        //         return BadRequest($"Lỗi khi đồng bộ thẻ: {ex.Message}");
        //     }
        // }
    }
}