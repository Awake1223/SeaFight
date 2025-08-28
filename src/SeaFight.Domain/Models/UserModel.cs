
namespace SeaFight.Domain.Models
{
    public class UserModel
    {
        public Guid Id { get; set; }  = Guid.NewGuid();
        public string Name { get; set; } 
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства для связи "один ко многим"
        public virtual ICollection<GameModel> GamesAsPlayer1 { get; set; } 
        public virtual ICollection<GameModel> GamesAsPlayer2 { get; set; } 
        public virtual ICollection<GameModel> Wins { get; set; }

        public virtual ICollection<GameShotModel> Shots { get; set; } = new List<GameShotModel>();

        public virtual ICollection<ShipModel> Ships { get; set; } = new List<ShipModel>();
    }
}
