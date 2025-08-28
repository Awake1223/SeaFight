
using System.Numerics;

namespace SeaFight.Domain.Models
{
    public class GameShotModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime ShotTime { get; set; } = DateTime.UtcNow;

        // Координаты выстрела
        public int CoordinateX { get; set; }
        public int CoordinateY { get; set; }

        // Результат выстрела
        public ShotResult Result { get; set; } // Попал? Может, даже потопил?

        // Связь: К какой игре принадлежит этот выстрел
        public virtual GameModel Game { get; set; }
        public Guid GameId { get; set; } 

        // Связь: Какой игрок совершил этот выстрел
        public virtual UserModel Player { get; set; }
        public Guid PlayerId { get; set; } // Внешний ключ на Player


        public enum ShotResult
        {
            Miss,   // Промах
            Hit,    // Попадание
            Sunk    // Корабль потоплен 
        }
    }
}
