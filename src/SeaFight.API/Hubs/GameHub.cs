using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SeaFight.API.DTOs;
using SeaFight.API.Models;
using SeaFight.Domain.Models;
using SeaFight.Infrastructure;
using static SeaFight.Domain.Models.GameModel;
using static SeaFight.Domain.Models.GameShotModel;

namespace SeaFight.API.Hubs
{
    public class GameHub : Hub
    {

        private readonly SeaFightDbContext _context;

        private static readonly Dictionary<string, GameSession> _activeGames = new(); //Это внутренняя память сервера для активных игр

        public GameHub(SeaFightDbContext context)
        {
            _context = context;
        }

        public async Task<string> CreateGame(string playerName) //Метод по созданию игры
        {
            var gameId = Guid.NewGuid().ToString(); //Инициализация id новой игры 

            
            var player = new UserModel { Name = playerName }; // создание пользователя, имя которого = playerName
            _context.User.Add(player); // в таблицу User записывается нащ игрок
            await _context.SaveChangesAsync(); // сохраняем мзменения

            _activeGames[gameId] = new GameSession //Создаём сессию которая имеет следующие поля
            {
                GameId = gameId, //id игры
                Player1Name = playerName, //имя пользователя
                Player1ConnectionId = Context.ConnectionId, //id подключения 
                Player1Id = player.Id, // id пользователя
                CurrentPlayerConnectionId = Context.ConnectionId //Присваиваем Id подключения определенному пользователю то есть Plyer1
            };

            await Groups.AddToGroupAsync(Context.ConnectionId, gameId); // Группирует gameId с ConnectionId 
            return gameId;
        }

        public async Task<bool> JoinGame(string gameId, string playerName) // Метод по присоединению к игре
        {
            if (_activeGames.TryGetValue(gameId, out var game)) //Проверка на существоввание игры 
            {
                
                var player = new UserModel { Name = playerName }; // Создание пользователя 
                _context.User.Add(player); // Запись в бд
                await _context.SaveChangesAsync(); // Сохранение

                game.Player2Name = playerName; //Присваивание 2 игроку имя 
                game.Player2ConnectionId = Context.ConnectionId; // присваивание 2 игроку connectionId
                game.Player2Id = player.Id;
                game.IsGameStarted = true;  //Поскольку второй игрок присоединился то мы начинаем игру

                await Groups.AddToGroupAsync(Context.ConnectionId, gameId); //Группируем gameId с connectionId
                await Clients.Group(gameId).SendAsync("GameStarted", gameId); //Эта строчка отправляет клиентам , которые находятся в одном gameId сообщение. Clients - все подключенные клиенты
                return true;
            }
            return false; //Ииначе метод false
        }

        public async Task<bool> PlaceShips(string gameId, List<ShipPlacement> ships) //метод по расстановке кораблей, который должен включать в себя gameId и список моделей ЭРасположение кораблей"
        {
            if (_activeGames.TryGetValue(gameId, out var game)) // Проверка на существование игры
            {
                
                if (Context.ConnectionId == game.Player1ConnectionId)  //Проверка подключения 1 игрока 
                {
                    game.Player1Ships = ships; 
                }
                else //Проверка подключения второго игрока
                {
                    game.Player2Ships = ships;
                }

              
                if (game.Player1Ships.Count > 0 && game.Player2Ships.Count > 0) //Минимальная проверка(поставили ли хотя бы 1 корабль, нужно будет сделать логику)
                {
                    await Clients.Group(gameId).SendAsync("AllPlayersReady"); 
                }

                return true;
            }
            return false;
        }


        public async Task<ShotResultDto> Shoot(string gameId, int x, int y) // Метод который отвечает за реализацию выстрела (Использует Dto ShotResult)
        {
            if (!_activeGames.TryGetValue(gameId, out var gameSession)) //Проверка существования игры 
            {
                return new ShotResultDto { Error = "Game not found" };
            }

            
            if (Context.ConnectionId != gameSession.CurrentPlayerConnectionId) // Проверка "чей ход"
            {
                return new ShotResultDto { Error = "Not your turn" };
            }

           
            if (!gameSession.IsGameStarted) //Проверка на старт игры
            {
                return new ShotResultDto { Error = "Game not started" }; 
            }

            
            if (gameSession.IsGameFinished) //Проверка на окончание игры 
            {
                return new ShotResultDto { Error = "Game already finished" };
            }

            // Определяем цель (противник)
            var isPlayer1Shooting = Context.ConnectionId == gameSession.Player1ConnectionId;


            List<ShipPlacement> targetShips;
            Guid? targetPlayerId;

            if (Context.ConnectionId == gameSession.Player1ConnectionId)
            {
                // Стреляет Player1 -> цель = Player2
                targetShips = gameSession.Player2Ships;
                targetPlayerId = gameSession.Player2Id;
            }
            else
            {
                // Стреляет Player2 -> цель = Player1  
                targetShips = gameSession.Player1Ships;
                targetPlayerId = gameSession.Player1Id;
            }


            var shotResult = CheckHit(targetShips, x, y);

            
            if (shotResult.IsHit)
            {
                shotResult.IsSunk = CheckIfShipSunk(targetShips, x, y);

                // Проверяем, не окончена ли игра (все корабли потоплены)
                shotResult.IsGameOver = CheckIfGameOver(targetShips);

                if (shotResult.IsGameOver)
                {
                    await FinishGame(gameId, gameSession, Context.ConnectionId);
                }
            }

            // Сохраняем выстрел в БД
            await SaveShotToDatabase(gameId, targetPlayerId, x, y, shotResult);

            // Сохраняем выстрел в сессии
            gameSession.Shots.Add(new ShotInfo
            {
                PlayerConnectionId = Context.ConnectionId,
                X = x,
                Y = y,
                IsHit = shotResult.IsHit,
                IsSunk = shotResult.IsSunk
            });

            // Передаем ход другому игроку если промах или игра продолжается
            if (!shotResult.IsHit || !shotResult.IsGameOver)
            {
                gameSession.CurrentPlayerConnectionId = isPlayer1Shooting
                    ? gameSession.Player2ConnectionId
                    : gameSession.Player1ConnectionId;

                await Clients.Client(gameSession.CurrentPlayerConnectionId)
                    .SendAsync("YourTurn");
            }

            // Отправляем результат всем игрокам
            await Clients.Group(gameId).SendAsync("ShotResult", shotResult);

            return shotResult;
        }

        private ShotResultDto CheckHit(List<ShipPlacement> ships, int x, int y)
        {
            foreach (var ship in ships)
            {
                if (IsCoordinateOnShip(ship, x, y))
                {
                    // Проверяем, не стреляли ли уже сюда
                    if (!ship.HitCoordinates.Any(c => c.X == x && c.Y == y))
                    {
                        ship.HitCoordinates.Add(new Coordinate { X = x, Y = y });
                    }
                    return new ShotResultDto { IsHit = true, X = x, Y = y };
                }
            }
            return new ShotResultDto { IsHit = false, X = x, Y = y };
        }
        private bool IsCoordinateOnShip(ShipPlacement ship, int x, int y)
        {
            if (ship.IsHorizontal)
            {
                return y == ship.StartY &&
                       x >= ship.StartX &&
                       x < ship.StartX + ship.Size;
            }
            else
            {
                return x == ship.StartX &&
                       y >= ship.StartY &&
                       y < ship.StartY + ship.Size;
            }
        }

        private async Task SaveShotToDatabase(string gameId, Guid? targetPlayerId, int x, int y, ShotResultDto result)
        {
            var shot = new GameShotModel
            {
                Id = Guid.NewGuid(),
                GameId = Guid.Parse(gameId),
                PlayerId = targetPlayerId,
                CoordinateX = x,
                CoordinateY = y,
                Result = result.IsHit ? ShotResult.Hit : ShotResult.Miss,
                ShotTime = DateTime.UtcNow
            };

            _context.GameShot.Add(shot);
            await _context.SaveChangesAsync();
        }

        private bool CheckIfShipSunk(List<ShipPlacement> ships, int x, int y)
        {
            // Находим корабль, в который попали
            var hitShip = ships.FirstOrDefault(ship =>
                ship.HitCoordinates.Any(c => c.X == x && c.Y == y));

            // Проверяем, потоплен ли он (все клетки поражены)
            return hitShip != null && hitShip.HitCoordinates.Count >= hitShip.Size;
        }

        private bool CheckIfGameOver(List<ShipPlacement> ships)
        {
            // Игра окончена, если все корабли потоплены
            return ships.All(ship => ship.IsSunk);
        }

        private async Task FinishGame(string gameId, GameSession gameSession, string winnerConnectionId)
        {
            var winnerName = winnerConnectionId == gameSession.Player1ConnectionId
                ? gameSession.Player1Name
                : gameSession.Player2Name;

            // Сохраняем результат в БД
            var game = await _context.Game
                .Include(g => g.Player1)
                .Include(g => g.Player2)
                .FirstOrDefaultAsync(g => g.Id == Guid.Parse(gameId));

            if (game != null)
            {
                game.Status = GameStatus.Finished;
                game.Winner = winnerConnectionId == gameSession.Player1ConnectionId
                    ? game.Player1
                    : game.Player2;
                await _context.SaveChangesAsync();
            }

            // Обновляем сессию
            gameSession.IsGameFinished = true;
            gameSession.Winner = winnerName;
            gameSession.WinnerConnectionId = winnerConnectionId;

            // Уведомляем игроков
            await Clients.Group(gameId).SendAsync("GameOver", winnerName);

            // Можно удалить игру из активных или оставить для истории
            // _activeGames.Remove(gameId);
        }

        public async Task<bool> AreBothPlayersReady(string gameId)
        {
            if (_activeGames.TryGetValue(gameId, out var gameSession))
            {
                return gameSession.Player1Ships.Count > 0 &&
                       gameSession.Player2Ships.Count > 0 &&
                       gameSession.IsGameStarted;
            }
            return false;
        }
        public GameSession GetGameState(string gameId)
        {
            if (_activeGames.TryGetValue(gameId, out var gameSession))
            {
                return gameSession;
            }
            return null;
        }

        public async Task<bool> Surrender(string gameId)
        {
            if (_activeGames.TryGetValue(gameId, out var gameSession))
            {
                var surrenderingPlayerId = Context.ConnectionId;
                var winnerConnectionId = surrenderingPlayerId == gameSession.Player1ConnectionId
                    ? gameSession.Player2ConnectionId
                    : gameSession.Player1ConnectionId;

                await FinishGame(gameId, gameSession, winnerConnectionId);
                return true;
            }
            return false;
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Находим игры, где участвовал отключившийся игрок
            var gamesToRemove = _activeGames
                .Where(g => g.Value.Player1ConnectionId == Context.ConnectionId ||
                           g.Value.Player2ConnectionId == Context.ConnectionId)
                .ToList();

            foreach (var game in gamesToRemove)
            {
                var gameSession = game.Value;
                var opponentConnectionId = gameSession.Player1ConnectionId == Context.ConnectionId
                    ? gameSession.Player2ConnectionId
                    : gameSession.Player1ConnectionId;

                if (opponentConnectionId != null)
                {
                    await Clients.Client(opponentConnectionId).SendAsync("OpponentDisconnected");
                }

                _activeGames.Remove(game.Key);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
