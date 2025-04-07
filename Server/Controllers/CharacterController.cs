using Microsoft.AspNetCore.Mvc;
using Server.Services;
using Shared.Models; // Giả sử bạn có DTO hoặc model trong Shared

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CharacterController : ControllerBase
    {
        private readonly CharacterService _characterService;

        public CharacterController(CharacterService characterService)
        {
            _characterService = characterService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Character>>> GetCharacters()
        {
            var characters = await _characterService.GetCharactersAsync();
            return Ok(characters);
        }
    }
}