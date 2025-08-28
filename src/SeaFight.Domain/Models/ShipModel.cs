
using System.Numerics;

namespace SeaFight.Domain.Models
{
    public class ShipModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public ShipType Type { get; set; } 
        public bool IsHorizontal { get; set; } // Ориентация

        // Начальная координата корабля
        public int StartX { get; set; }
        public int StartY { get; set; }

        // Связь: К какому игроку в какой игре принадлежит этот корабль
        public virtual GameModel Game { get; set; }
        public Guid GameId { get; set; }

        public virtual UserModel Player { get; set; }
        public Guid PlayerId { get; set; }

        
        public virtual ICollection<GameShotModel> Hits { get; set; } = new List<GameShotModel>();


        public enum ShipType
        {
            Battleship = 4, // Линейный (4 палубы)
            Cruiser = 3,    // Крейсер (3 палубы)
            Destroyer = 2,  // Эсминец (2 палубы)
            Submarine = 1   // Подлодка (1 палуба)
        }
    }
}
