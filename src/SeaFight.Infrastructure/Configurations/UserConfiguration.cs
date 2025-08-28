
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeaFight.Domain.Models;

namespace SeaFight.Infrastructure.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<UserModel>
    {
        public void Configure(EntityTypeBuilder<UserModel> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(20);
            builder.Property(x => x.JoinedAt).IsRequired();

            // Явно конфигурируем все связи
            builder.HasMany(x => x.GamesAsPlayer1)
                .WithOne(g => g.Player1)
                .HasForeignKey(g => g.Player1Id)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.GamesAsPlayer2)
                .WithOne(g => g.Player2)
                .HasForeignKey(g => g.Player2Id)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Wins)
                .WithOne(g => g.Winner)
                .HasForeignKey(g => g.WinnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Добавляем конфигурацию для остальных коллекций
            builder.HasMany(x => x.Shots)
                .WithOne(s => s.Player)
                .HasForeignKey(s => s.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Ships)
                .WithOne(s => s.Player)
                .HasForeignKey(s => s.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
