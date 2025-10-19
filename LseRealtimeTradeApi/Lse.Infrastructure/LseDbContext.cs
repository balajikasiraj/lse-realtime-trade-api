using Lse.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lse.Infrastructure
{
    public class LseDbContext : DbContext
    {
        public LseDbContext(DbContextOptions<LseDbContext> options) : base(options)
        {
        }

        public DbSet<Trade> Trades { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Trade>(eb =>
            {
                eb.HasKey(t => t.Id);
                eb.Property(t => t.Ticker).IsRequired();
                eb.Property(t => t.Price).HasColumnType("decimal(18,4)");
                eb.Property(t => t.Quantity).HasColumnType("decimal(18,2)"); // Reduced decimal places for quantity
                eb.Property(t => t.BrokerId).IsRequired();
                eb.Property(t => t.Timestamp).IsRequired();
            });
        }
    }
}
