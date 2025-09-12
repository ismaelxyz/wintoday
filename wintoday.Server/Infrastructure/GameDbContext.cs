using Microsoft.EntityFrameworkCore;
using wintoday.Server.Domain.Entities;
using wintoday.Server.Domain.Enums;

namespace wintoday.Server.Infrastructure;

public class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<GameRound> Rounds => Set<GameRound>();
    public DbSet<Bet> Bets => Set<Bet>();
    public DbSet<BalanceTransaction> Transactions => Set<BalanceTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(e =>
        {
            e.HasIndex(p => p.NormalizedName).IsUnique();
            e.Property(p => p.Funds).HasPrecision(18, 2);
            // RowVersion previously used [Timestamp]; removed concurrency token because PostgreSQL doesn't auto-populate it.
            e.Property(p => p.RowVersion)
                .HasColumnType("bytea")
                .HasDefaultValueSql("'\\x'::bytea"); // empty bytea default
        });

        modelBuilder.Entity<Bet>(e =>
        {
            e.HasOne(b => b.Player).WithMany(p => p.Bets).HasForeignKey(b => b.PlayerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(b => b.Round).WithMany(r => r.Bets).HasForeignKey(b => b.RoundId).OnDelete(DeleteBehavior.Cascade);
            e.Property(b => b.Wager).HasPrecision(18, 2);
            e.Property(b => b.Payout).HasPrecision(18, 2);
        });

        modelBuilder.Entity<GameRound>(e =>
        {
            e.HasOne(r => r.Player).WithMany().HasForeignKey(r => r.PlayerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BalanceTransaction>(e =>
        {
            e.HasOne(t => t.Player).WithMany(p => p.Transactions).HasForeignKey(t => t.PlayerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Bet).WithMany().HasForeignKey(t => t.BetId).OnDelete(DeleteBehavior.Cascade);
            e.Property(t => t.Amount).HasPrecision(18, 2);
            e.Property(t => t.BalanceAfter).HasPrecision(18, 2);
        });
    }
}
