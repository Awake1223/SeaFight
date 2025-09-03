using static SeaFight.Domain.Models.ShipModel;

namespace SeaFight.API.DTOs
{
    public class ShipPlacementRequest
    {
        public ShipType Type { get; set; }
        public bool IsHorizontal { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
    }

    public enum ShipType
    {
        Battleship = 4,
        Cruiser = 3,
        Destroyer = 2,
        Submarine = 1
    }
}
