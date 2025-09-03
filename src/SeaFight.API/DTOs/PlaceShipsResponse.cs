namespace SeaFight.API.DTOs
{
    public class PlaceShipsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();

        // Дополнительная информация о текущем статусе
        public bool BothPlayersReady { get; set; }
        public string GameStatus { get; set; }
    }
}
