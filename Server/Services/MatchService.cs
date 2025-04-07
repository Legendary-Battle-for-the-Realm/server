namespace Server.Services
{
    public class MatchService
    {
        private readonly Dictionary<int, string> _matches = new Dictionary<int, string>();

        public void CreateMatch(int matchId, string matchData)
        {
            _matches[matchId] = matchData;
        }

        public string GetMatchState(int matchId)
        {
            return _matches.TryGetValue(matchId, out var data) ? data : null;
        }
    }
}