

namespace SeaFight.Domain.Models
{
    public class GameModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public GameStatus Status { get; set; } = GameStatus.WaitingForPlayer;

        public virtual UserModel Player1 { get; set; } // Создатель игры
        public virtual UserModel Player2 { get; set; } //Присоединяющийся

        public Guid? Player1Id { get; set; }
        public Guid? Player2Id { get; set; }


        public virtual UserModel Winner { get; set; }
        public Guid? WinnerId { get; set; }

        public virtual ICollection<GameShotModel> Shots { get; set; } = new List<GameShotModel>();

        public virtual ICollection<ShipModel> Ships { get; set; } = new List<ShipModel>();

        public enum GameStatus
        {
            WaitingForPlayer, // Ожидает второго игрока
            ShipsPlacing,     // Игроки расставляют корабли
            InProgress,       // Идет игра
            Finished          // Игра завершена
        }

    }
}
