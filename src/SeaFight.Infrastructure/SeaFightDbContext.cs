
using Microsoft.EntityFrameworkCore;
using SeaFight.Domain.Models;
using SeaFight.Infrastructure.Configurations;

namespace SeaFight.Infrastructure
{
    public class SeaFightDbContext : DbContext
    {
        public SeaFightDbContext(DbContextOptions options) : base(options) 
        {
        }

        public DbSet<UserModel> User { get; set; }
        public DbSet<GameModel> Game { get; set; }
        public DbSet<GameShotModel> GameShot { get; set; }
        public DbSet<ShipModel> Ship { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new GameConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new GameShotConfigure());
            modelBuilder.ApplyConfiguration(new ShipConfigure());

        }
    }
}
