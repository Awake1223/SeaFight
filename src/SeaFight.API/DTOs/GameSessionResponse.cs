namespace SeaFight.API.DTOs
{
    public class GameSessionResponse
    {
        public string GameId { get; set; }
        public Guid PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string ConnectionToken { get; set; }
        public string OpponentName { get; set; }

        public bool Player1ShipsValidated { get; set; }
        public bool Player2ShipsValidated { get; set; }
    }
}
