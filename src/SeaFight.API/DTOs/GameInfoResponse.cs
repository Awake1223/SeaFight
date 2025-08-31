namespace SeaFight.API.DTOs
{
    public class GameInfoResponse
    {
        public string GameId { get; set; }
        public string Status { get; set; }
        public UserInfoDto Player1 { get; set; }
        public UserInfoDto Player2 { get; set; }
        public UserInfoDto Winner { get; set; }
    }
}
