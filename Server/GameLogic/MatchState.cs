using System;
using System.Collections.Generic;

namespace Server.GameLogic
{
    public class MatchState
    {
        public int MatchId { get; set; }
        public List<string> PlayerOrder { get; set; } = new List<string>();
        public string CurrentPlayerId { get; set; } = string.Empty;
        public int TurnIndex { get; set; }
        public int RoundNumber { get; set; }
        public Dictionary<string, (bool ActionCardUsed, bool SkillUsed)> ActionUsage { get; set; } = new Dictionary<string, (bool, bool)>();
        public Dictionary<string, object> AdditionalState { get; set; } = new Dictionary<string, object>();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}