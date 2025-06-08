using CarInsuranceBot.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarInsuranceBot.Infrastructure.Data
{
    public class BotDbContext : DbContext
    {
        public BotDbContext(DbContextOptions<BotDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserSession> UserSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(e => e.ChatId);

                entity.Ignore(e => e.AdditionalData);

                entity.Property(e => e.PassportData).HasMaxLength(1000);
                entity.Property(e => e.PolicyNumber).HasMaxLength(100);

            });
        }
    }
}
