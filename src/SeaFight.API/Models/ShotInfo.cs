namespace SeaFight.API.Models
{
    public class ShotInfo
    {
        public string PlayerConnectionId { get; set; }
        public int X {  get; set; }
        public int Y { get; set; }
        public bool IsHit { get; set; }
        public bool IsSunk { get; set; } 
        public DateTime ShotTime { get; set; } = DateTime.UtcNow;
    }
}
