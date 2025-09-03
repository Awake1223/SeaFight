namespace SeaFight.API.DTOs
{
    public class PlaceShipsRequest
    {
        public string GameId { get; set; }
        public List<ShipPlacementRequest> Ships { get; set; }
    }

}
