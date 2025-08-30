namespace SeaFight.API.Models
{
    public class ShipPlacement
    {
        public int Size { get; set; }
        public bool IsHorizontal { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }

        public List<Coordinate> HitCoordinates { get; set; } = new List<Coordinate>();

        public bool IsSunk => HitCoordinates.Count >= Size;
    }
}
