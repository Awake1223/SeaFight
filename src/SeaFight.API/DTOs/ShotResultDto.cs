namespace SeaFight.API.DTOs
{
    public class ShotResultDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsHit { get; set; }
        public bool IsSunk { get; set; }
        public bool IsGameOver { get; set; }
        public string Error { get; set; }
    }
}
