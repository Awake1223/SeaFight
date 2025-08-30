using System.Diagnostics.Eventing.Reader;

namespace SeaFight.API.Models
{
    public class GameSession
    {
        public string GameId { get; set; }
        public string Player1ConnectionId { get; set; }
        public string Player2ConnectionId { get; set; }
        public string Player1Name { get; set; }
        public string Player2Name { get; set; }
        public string CurrentPlayerConnectionId { get; set; }

        public Guid? Player1Id { get; set; }
        public Guid? Player2Id { get; set; }

        public List<ShipPlacement> Player1Ships { get; set; } = new();
        public List<ShipPlacement> Player2Ships { get; set; } = new();
        public List<ShotInfo> Shots { get; set; } = new();

        public bool IsGameStarted { get; set; }
        public bool IsGameFinished { get; set; }
        public string Winner { get; set; }
        public string WinnerConnectionId { get; set; }
    }
}
