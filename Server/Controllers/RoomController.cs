using Microsoft.AspNetCore.Mvc;
using GameServer.Services;
using Shared.Models;

namespace GameServer.Controllers
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
        public IActionResult CreateRoom([FromBody] CreateRoomRequest request)
        {
            var room = _roomService.CreateRoom(request.MaxPlayers);
            return Ok(room);
        }
    }

    public class CreateRoomRequest
    {
        public int MaxPlayers { get; set; }
    }
}