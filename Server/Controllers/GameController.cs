using Microsoft.AspNetCore.Mvc;
using Server.Services;
using Shared.Models;

namespace Server.Controllers
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
        public async Task<IActionResult> StartGame([FromBody] int roomId)
        {
            var result = await _gameService.StartGameAsync(roomId);
            if (result)
            {
                return Ok("Game started successfully.");
            }
            return BadRequest("Failed to start game.");
        }

        [HttpPost("draw-card")]
        public async Task<IActionResult> DrawCard([FromBody] DrawCardRequest request)
        {
            var card = await _gameService.DrawCardAsync(request.RoomId, request.PlayerId);
            if (card != null)
            {
                return Ok(card);
            }
            return BadRequest("Failed to draw card.");
        }

        [HttpPost("use-card")]
        public async Task<IActionResult> UseCard([FromBody] UseCardRequest request)
        {
            var result = await _gameService.UseCardAsync(request.RoomId, request.PlayerId, request.CardId);
            if (result)
            {
                return Ok("Card used successfully.");
            }
            return BadRequest("Failed to use card.");
        }

        [HttpPost("pass-turn")]
        public async Task<IActionResult> PassTurn([FromBody] PassTurnRequest request)
        {
            var result = await _gameService.PassTurnAsync(request.RoomId, request.PlayerId);
            if (result)
            {
                return Ok("Turn passed successfully.");
            }
            return BadRequest("Failed to pass turn.");
        }

        [HttpPost("check-win")]
        public async Task<IActionResult> CheckWinCondition([FromBody] int roomId)
        {
            var winner = await _gameService.CheckWinConditionAsync(roomId);
            if (winner != null)
            {
                return Ok(new { WinnerId = winner.Id, WinnerName = winner.Name });
            }
            return Ok("Game continues.");
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

    public class PassTurnRequest
    {
        public int RoomId { get; set; }
        public int PlayerId { get; set; }
    }
}