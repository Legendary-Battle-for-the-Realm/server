using Microsoft.AspNetCore.Mvc;
using GameServer.Services;
using Shared.Models;

namespace GameServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayerController : ControllerBase
    {
        private readonly RoomService _roomService;

        public PlayerController(RoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpPost("join-room")]
        public IActionResult JoinRoom([FromBody] JoinRoomRequest request)
        {
            var result = _roomService.JoinRoom(request.RoomId, request.PlayerId);
            if (result)
            {
                return Ok("Joined room successfully.");
            }
            return BadRequest("Failed to join room.");
        }
    }

    public class JoinRoomRequest
    {
        public int RoomId { get; set; }
        public int PlayerId { get; set; }
    }
}