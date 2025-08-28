using Microsoft.AspNetCore.SignalR;
using SeaFight.API.Models;
using SeaFight.Infrastructure;

namespace SeaFight.API.Hubs
{
    public class GameHub : Hub
    {

        private readonly SeaFightDbContext _context;

        private static readonly Dictionary<string, GameSession> _activeGames = new();

        public GameHub(SeaFightDbContext context)
        {
            _context = context;
        }

        public async Task<string> CreateGame(string playerName)
        {
            var gameId = Guid.NewGuid().ToString();
            _activeGames[gameId] = new GameSession { Player1Name = playerName };
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            return gameId;
        }

        public async Task<bool> JoinGame (string gameId, string playerNmae)
        {
            if (_activeGames.TryGetValue(gameId, out var game))
            {
                game.Player2Name = playerNmae;
                await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
                await Clients.Groups(gameId).SendAsync("Game started");
                return true;
            }
            return false;
        }

        public async Task<bool> PlaceShips(string gameId, List<ShipPlacement> ships)
        {
           
            if (_activeGames.TryGetValue(gameId, out var game))
            {
                game.Player1Ships = ships;
                return true;
            }
            return false;
        }


        public async Task<bool> Shoot (string gameId, int x, int y )
        {
            
        }
    }
}
