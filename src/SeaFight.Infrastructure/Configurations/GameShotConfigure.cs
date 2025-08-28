
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeaFight.Domain.Models;

namespace SeaFight.Infrastructure.Configurations
{
    public class GameShotConfigure : IEntityTypeConfiguration<GameShotModel>
    {
        public void Configure(EntityTypeBuilder<GameShotModel> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ShotTime).IsRequired();
            builder.Property(x => x.CoordinateX).IsRequired();
            builder.Property(x => x.CoordinateY).IsRequired();
            builder.Property(x => x.Result).IsRequired().HasConversion<string>();

            // Связь с игрой
            builder.HasOne(x => x.Game)
                   .WithMany(g => g.Shots)
                   .HasForeignKey(x => x.GameId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Связь с игроком
            builder.HasOne(x => x.Player)
                   .WithMany(u => u.Shots)
                   .HasForeignKey(x => x.PlayerId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
