using Microsoft.AspNetCore.Mvc;
using Server.Services;
using Server.GameLogic;
using Microsoft.AspNetCore.Authorization;

namespace Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MatchController : ControllerBase
    {
        private readonly MatchService _matchService;

        public MatchController(MatchService matchService)
        {
            _matchService = matchService;
        }

        [HttpPost]
        public IActionResult CreateMatch([FromBody] CreateMatchRequest request)
        {
            var matchState = _matchService.CreateMatch(request.MatchId, request.PlayerOrder);
            return Ok(matchState);
        }

        [HttpGet("{matchId}")]
        public IActionResult GetMatchState(int matchId)
        {
            var matchState = _matchService.GetMatchState(matchId);
            if (matchState == null) return NotFound();
            return Ok(matchState);
        }

        [HttpPut("{matchId}")]
        public IActionResult UpdateMatchState(int matchId, [FromBody] MatchState matchState)
        {
            _matchService.UpdateMatchState(matchId, matchState);
            return Ok();
        }
    }

    public class CreateMatchRequest
    {
        public int MatchId { get; set; }
        public required List<string> PlayerOrder { get; set; }
    }
}