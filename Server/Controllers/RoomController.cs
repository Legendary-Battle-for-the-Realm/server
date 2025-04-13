using Microsoft.AspNetCore.Mvc;
using Server.Services;
using Shared.Models;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomController : ControllerBase
    {
        private readonly RoomService _roomService;

        public RoomController(RoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
        {
            var room = await _roomService.CreateRoomAsync(request.MaxPlayers);
            return Ok(room);
        }
    }

    public class CreateRoomRequest
    {
        public int MaxPlayers { get; set; }
    }
}