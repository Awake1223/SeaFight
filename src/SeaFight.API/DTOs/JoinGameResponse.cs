namespace SeaFight.API.DTOs
{
    public class JoinGameResponse
    {
        public string GameId { get; set; }
        public Guid PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string OpponentName { get; set; }
        public string ConnectionToken { get; set; }
    }
}
