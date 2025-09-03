using System.ComponentModel.DataAnnotations;
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

        public async Task<PlaceShipsResponse> PlaceShips(PlaceShipsRequest request)
        {
            try
            {
                if (!_activeGames.TryGetValue(request.GameId, out var gameSession))
                {
                    return new PlaceShipsResponse
                    {
                        Success = false,
                        Error = "Game not found"
                    };
                }

                // Определяем игрока
                var isPlayer1 = Context.ConnectionId == gameSession.Player1ConnectionId;
                var playerId = isPlayer1 ? gameSession.Player1Id : gameSession.Player2Id;

                if (!playerId.HasValue)
                {
                    return new PlaceShipsResponse
                    {
                        Success = false,
                        Error = "Player not found"
                    };
                }

                // 1. Валидация расстановки
                var validationResult = ValidateShipPlacement(request.Ships);
                if (!validationResult.IsValid)
                {
                    return new PlaceShipsResponse
                    {
                        Success = false,
                        Error = "Invalid ship placement",
                        ValidationErrors = validationResult.ErrorMessage()
                    };
                }

                // 2. Сохраняем корабли в БД через Domain модель
                await SaveShipsToDatabase(request.GameId, playerId.Value, request.Ships);

                // 3. Обновляем игровую сессию
                if (isPlayer1)
                {
                    gameSession.Player1ShipsValidated = true;
                }
                else
                {
                    gameSession.Player2ShipsValidated = true;
                }

                // 4. Уведомляем о готовности игрока
                await Clients.Group(request.GameId).SendAsync("PlayerReady",
                    isPlayer1 ? gameSession.Player1Name : gameSession.Player2Name);

                // 5. Если оба игрока готовы - начинаем игру
                if (gameSession.Player1ShipsValidated && gameSession.Player2ShipsValidated)
                {
                    gameSession.IsGameStarted = true;
                    gameSession.CurrentPlayerConnectionId = gameSession.Player1ConnectionId;

                    await Clients.Group(request.GameId).SendAsync("AllPlayersReady");
                    await Clients.Client(gameSession.CurrentPlayerConnectionId).SendAsync("YourTurn");
                }

                return new PlaceShipsResponse
                {
                    Success = true,
                    Message = "Ships placed successfully"
                };
            }
            catch (Exception ex)
            {
                return new PlaceShipsResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }


        private async Task SaveShipsToDatabase(string gameId, Guid playerId, List<ShipPlacementRequest> shipsDto)
        {
            var gameGuid = Guid.Parse(gameId);

            // Удаляем предыдущую расстановку (если игрок переставлял)
            var existingShips = _context.Ship
                .Where(s => s.GameId == gameGuid && s.PlayerId == playerId);
            _context.Ship.RemoveRange(existingShips);

            // Создаем новые корабли через Domain модель
            foreach (var shipDto in shipsDto)
            {
                var ship = new ShipModel
                {
                    GameId = gameGuid,
                    PlayerId = playerId,
                    Type = (ShipModel.ShipType)shipDto.Type,
                    IsHorizontal = shipDto.IsHorizontal,
                    StartX = shipDto.StartX,
                    StartY = shipDto.StartY
                };
                _context.Ship.Add(ship);
            }

            await _context.SaveChangesAsync();
        }
        private bool HasAdjacentShips(List<ShipPlacementRequest> ships)
        {
            var allCoordinates = new HashSet<(int, int)>();
            var shipCoordinates = new Dictionary<ShipPlacementRequest, List<(int, int)>>();

            // Сначала собираем все координаты кораблей
            foreach (var ship in ships)
            {
                var coordinates = new List<(int, int)>();
                var size = (int)ship.Type;

                for (int i = 0; i < size; i++)
                {
                    var x = ship.IsHorizontal ? ship.StartX + i : ship.StartX;
                    var y = ship.IsHorizontal ? ship.StartY : ship.StartY + i;

                    coordinates.Add((x, y));
                    allCoordinates.Add((x, y));
                }
                shipCoordinates[ship] = coordinates;
            }

            // Проверяем соседние клетки для каждого корабля
            foreach (var ship in ships)
            {
                foreach (var coord in shipCoordinates[ship])
                {
                    // Проверяем все 8 направлений вокруг клетки
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue; // Пропускаем саму клетку

                            var neighborX = coord.Item1 + dx;
                            var neighborY = coord.Item2 + dy;

                            // Если соседняя клетка занята другим кораблем и не является частью этого же корабля
                            if (allCoordinates.Contains((neighborX, neighborY)) &&
                                !shipCoordinates[ship].Contains((neighborX, neighborY)))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private Models.ValidationResult ValidateShipPlacement(List<ShipPlacementRequest> ships)
        {
            var errors = new List<string>();

            // 1. Проверка количества кораблей (1x4, 2x3, 3x2, 4x1)
            var shipCounts = ships.GroupBy(s => s.Type)
                                 .ToDictionary(g => g.Key, g => g.Count());

            var expectedCounts = new Dictionary<ShipType, int>
            {
                [ShipType.Battleship] = 1,
                [ShipType.Cruiser] = 2,
                [ShipType.Destroyer] = 3,
                [ShipType.Submarine] = 4
            };

            foreach (var expected in expectedCounts)
            {
                if (!shipCounts.TryGetValue(expected.Key, out var actual) || actual != expected.Value)
                {
                    errors.Add($"Expected {expected.Value} {expected.Key}(s), got {actual}");
                }
            }

            // 2. Проверка координат
            foreach (var ship in ships)
            {
                if (!IsShipWithinBoard(ship))
                {
                    errors.Add($"{ship.Type} at ({ship.StartX},{ship.StartY}) is outside the board");
                }
            }

            // 3. Проверка пересечений
            if (HasOverlappingShips(ships))
            {
                errors.Add("Ships cannot overlap");
            }

            // 4. Проверка расстояния между кораблями
            if (HasAdjacentShips(ships))
            {
                errors.Add("Ships must be at least 1 cell apart");
            }

            return new Models.ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        private bool IsShipWithinBoard(ShipPlacementRequest ship)
        {
            var size = (int)ship.Type;
            var endX = ship.IsHorizontal ? ship.StartX + size - 1 : ship.StartX;
            var endY = ship.IsHorizontal ? ship.StartY : ship.StartY + size - 1;

            return ship.StartX >= 0 && ship.StartY >= 0 &&
                   endX < 10 && endY < 10;
        }

        private bool HasOverlappingShips(List<ShipPlacementRequest> ships)
        {
            var allCoordinates = new HashSet<(int, int)>();

            foreach (var ship in ships)
            {
                var size = (int)ship.Type;
                for (int i = 0; i < size; i++)
                {
                    var x = ship.IsHorizontal ? ship.StartX + i : ship.StartX;
                    var y = ship.IsHorizontal ? ship.StartY : ship.StartY + i;

                    if (!allCoordinates.Add((x, y)))
                    {
                        return true;
                    }
                }
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

        private ShotResultDto CheckHit(List<ShipPlacement> ships, int x, int y) //Проверка попадания
        {
            foreach (var ship in ships) // Делаем цикл для проверки попадания в корабль
            {
                if (IsCoordinateOnShip(ship, x, y)) 
                {
                    // Проверяем, не стреляли ли уже сюда
                    if (!ship.HitCoordinates.Any(c => c.X == x && c.Y == y))
                    {
                        ship.HitCoordinates.Add(new Coordinate { X = x, Y = y });
                    }
                    return new ShotResultDto { IsHit = true, X = x, Y = y }; //Возращаем Dtos с попаданием
                }
            }
            return new ShotResultDto { IsHit = false, X = x, Y = y }; // Возвращаем Dtos с промахом
        }
        private bool IsCoordinateOnShip(ShipPlacement ship, int x, int y) // метод по расстановке кораблей
        {
            if (ship.IsHorizontal) //Проверка на горизонтальный корабль
            {
                return y == ship.StartY && 
                       x >= ship.StartX &&
                       x < ship.StartX + ship.Size;
            }
            else // Иначе вертикальный
            {
                return x == ship.StartX &&
                       y >= ship.StartY &&
                       y < ship.StartY + ship.Size;
            }
        }

        private async Task SaveShotToDatabase(string gameId, Guid? targetPlayerId, int x, int y, ShotResultDto result) //Метод сохранения попадания в базу данных
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

        private bool CheckIfShipSunk(List<ShipPlacement> ships, int x, int y) //Метод по проверки корабля который утонул
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

        private async Task FinishGame(string gameId, GameSession gameSession, string winnerConnectionId)// Метод по завершению игры
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
