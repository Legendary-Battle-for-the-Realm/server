using Microsoft.AspNetCore.Mvc;
using GameServer.Services;
using Shared.Models;

namespace GameServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {
        private readonly GameService _gameService;

        public GameController(GameService gameService)
        {
            _gameService = gameService;
        }

        [HttpPost("start")]
        public IActionResult StartGame([FromBody] int roomId)
        {
            var result = _gameService.StartGame(roomId);
            if (result)
            {
                return Ok("Game started successfully.");
            }
            return BadRequest("Failed to start game.");
        }

        [HttpPost("draw-card")]
        public IActionResult DrawCard([FromBody] DrawCardRequest request)
        {
            var card = _gameService.DrawCard(request.RoomId, request.PlayerId);
            if (card != null)
            {
                return Ok(card);
            }
            return BadRequest("Failed to draw card.");
        }

        [HttpPost("use-card")]
        public IActionResult UseCard([FromBody] UseCardRequest request)
        {
            var result = _gameService.UseCard(request.RoomId, request.PlayerId, request.CardId);
            if (result)
            {
                return Ok("Card used successfully.");
            }
            return BadRequest("Failed to use card.");
        }
    }

    public class DrawCardRequest
    {
        public int RoomId { get; set; }
        public int PlayerId { get; set; }
    }

    public class UseCardRequest
    {
        public int RoomId { get; set; }
        public int PlayerId { get; set; }
        public int CardId { get; set; }
    }
}