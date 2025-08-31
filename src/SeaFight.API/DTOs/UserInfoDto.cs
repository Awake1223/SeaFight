namespace SeaFight.API.DTOs
{
    public class UserInfoDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
    }
}
