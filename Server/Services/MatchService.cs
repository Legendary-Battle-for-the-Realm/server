using System.Collections.Generic;
using System;
using Server.GameLogic;

namespace Server.Services
{
    public class MatchService
    {
        private readonly Dictionary<int, MatchState> _matches = new Dictionary<int, MatchState>();

        public MatchState CreateMatch(int matchId, List<string> playerOrder)
        {
            var matchState = new MatchState
            {
                MatchId = matchId,
                PlayerOrder = playerOrder,
                CurrentPlayerId = playerOrder[0],
                TurnIndex = 0,
                RoundNumber = 1,
                ActionUsage = new Dictionary<string, (bool, bool)>(),
                AdditionalState = new Dictionary<string, object>(),
                LastUpdated = DateTime.UtcNow
            };
            _matches[matchId] = matchState;
            return matchState;
        }

        public MatchState GetMatchState(int matchId)
        {
            return _matches.TryGetValue(matchId, out var match) ? match : null;
        }

        public void UpdateMatchState(int matchId, MatchState updatedState)
        {
            if (_matches.ContainsKey(matchId))
            {
                _matches[matchId] = updatedState;
                _matches[matchId].LastUpdated = DateTime.UtcNow;
            }
        }
    }
}