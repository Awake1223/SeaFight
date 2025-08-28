
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeaFight.Domain.Models;


namespace SeaFight.Infrastructure.Configurations
{
    public class ShipConfigure : IEntityTypeConfiguration<ShipModel>
    {
        public void Configure(EntityTypeBuilder<ShipModel> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type).IsRequired();
            builder.Property(x => x.IsHorizontal).IsRequired();
            builder.Property(x => x.StartX).IsRequired();
            builder.Property(x => x.StartY).IsRequired();

            
            // builder.Property(x => x.Hits).IsRequired().HasDefaultValue(0);

            // Связь с игрой
            builder.HasOne(x => x.Game)
                   .WithMany(g => g.Ships)
                   .HasForeignKey(x => x.GameId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Связь с игроком
            builder.HasOne(x => x.Player)
                   .WithMany(u => u.Ships)
                   .HasForeignKey(x => x.PlayerId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
