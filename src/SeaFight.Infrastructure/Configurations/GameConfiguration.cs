
using SeaFight.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SeaFight.Infrastructure.Configurations
{
    public class GameConfiguration : IEntityTypeConfiguration<GameModel>
    {
        public void Configure(EntityTypeBuilder<GameModel> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.Status).IsRequired().HasConversion<string>();
            builder.Property(x => x.Player1Id).IsRequired(false);
            builder.Property(x => x.Player2Id).IsRequired(false);
            builder.Property(x => x.WinnerId).IsRequired(false);

            // Настройка связи с Player1
            builder.HasOne(x => x.Player1)
                   .WithMany(u => u.GamesAsPlayer1)
                   .HasForeignKey(x => x.Player1Id)
                   .OnDelete(DeleteBehavior.Restrict);

            // Настройка связи с Player2
            builder.HasOne(x => x.Player2)
                   .WithMany(u => u.GamesAsPlayer2)
                   .HasForeignKey(x => x.Player2Id)
                   .OnDelete(DeleteBehavior.Restrict);

            // Настройка связи с Winner
            builder.HasOne(x => x.Winner)
                   .WithMany(u => u.Wins)
                   .HasForeignKey(x => x.WinnerId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Настройка связи с выстрелами
            builder.HasMany(x => x.Shots)
                   .WithOne(s => s.Game)
                   .HasForeignKey(s => s.GameId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Настройка связи с кораблями
            builder.HasMany(x => x.Ships)
                   .WithOne(s => s.Game)
                   .HasForeignKey(s => s.GameId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
