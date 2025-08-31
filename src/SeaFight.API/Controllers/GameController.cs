using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SeaFight.Infrastructure;
using SeaFight.API.DTOs;
using SeaFight.API.Hubs;
using SeaFight.Domain.Models;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using SeaFight.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Microsoft.IdentityModel.Tokens;

namespace SeaFight.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GameController : ControllerBase
    {
        private readonly IHubContext<GameHub> _hubContext;
        private readonly SeaFightDbContext _context;

        public GameController(IHubContext<GameHub> hubContext, SeaFightDbContext context)
        {
            _hubContext = hubContext;
            _context = context;
        }


        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse<GameSessionResponse>>> CreateGameSession([FromBody] CreateGameSessionRequest request) 
        {
            try
            {
                var user = new UserModel
                {
                    Name = request.PlayerName,
                };
                _context.User.Add(user);
                await _context.SaveChangesAsync();

                var game = new GameModel
                {
                    Player1Id = user.Id,
                    Status = GameModel.GameStatus.WaitingForPlayer,
                };

                _context.Game.Add(game);
                await _context.SaveChangesAsync();

                var hub = _hubContext.Clients.All;
                var gameId = game.Id.ToString();

                var response = new GameSessionResponse
                {
                    GameId = gameId,
                    PlayerId = user.Id,
                    PlayerName = user.Name,
                    ConnectionToken = GenerateConnectionToken(user.Id, gameId)
                };
                return Ok(new ApiResponse<GameSessionResponse>
                {
                    Succes = true,
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<GameSessionResponse>
                {
                    Succes = false,
                    Error = ex.Message
                });
            }
        }
        [HttpPost("join")]
        public async Task<ActionResult<ApiResponse<JoinGameResponse>>> JoinGameSession([FromBody] JoinGameRequest request)
        {
            try
            {
                var game = await _context.Game
                    .Include(g => g.Player1)
                    .FirstOrDefaultAsync(g => g.Id == Guid.Parse(request.GameId));
                if (game == null)
                {
                    return NotFound(new ApiResponse<JoinGameResponse>
                    {
                        Succes = false,
                        Error = "Game was not found"
                    });
                }
                if (game.Status != GameModel.GameStatus.WaitingForPlayer)
                {
                    return BadRequest(new ApiResponse<JoinGameResponse>
                    {
                        Succes = false,
                        Error = "Game already started"
                    });
                }

                var player2 = new UserModel { Name = request.PlayerName };
                _context.User.Add(player2);

                game.Player2Id = player2.Id;
                game.Status = GameModel.GameStatus.ShipsPlacing;
                await _context.SaveChangesAsync();

                var response = new JoinGameResponse
                {
                    GameId = request.GameId,
                    PlayerId = player2.Id,
                    PlayerName = game.Player1.Name,
                    ConnectionToken = GenerateConnectionToken(player2.Id, request.GameId)
                };
                return Ok(new ApiResponse<JoinGameResponse>
                {
                    Succes = true,
                    Data = response
                });

            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<JoinGameResponse>
                {
                    Succes = false,
                    Error = ex.Message

                });
            }
        }
        [HttpGet("active")]
        public ActionResult<ApiResponse<List<ActiveGameResponse>>> GetActiveGames()
        {
            try
            {
                var activeGames = _context.Game
                    .Where(g => g.Status == GameModel.GameStatus.WaitingForPlayer)
                    .Include(g => g.Player1)
                    .Select(g => new ActiveGameResponse
                    {
                        GameId = g.Id.ToString(),
                        Player1Name = g.Player1.Name,
                        CreatedAt = g.CreatedAt
                    }).ToList();

                return Ok(new ApiResponse<List<ActiveGameResponse>>
                {
                    Succes = true,
                    Data = activeGames
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<List<ActiveGameResponse>>
                {
                    Succes = false,
                    Error = ex.Message
                });
            }
        }
        private string GenerateConnectionToken(Guid userId, string gameId)
        {
            return $"{userId}:{gameId}:{DateTime.UtcNow.Ticks}";
        }
    }
}

