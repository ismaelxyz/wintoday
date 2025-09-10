using System.ComponentModel.DataAnnotations.Schema;

namespace wintoday.Server.Domain.Entities;

public enum BalanceTransactionType
{
    InitialFunds = 1,
    BetWager = 2,
    BetPayout = 3,
    ManualSaveDelta = 4
}

public class BalanceTransaction
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public Guid? BetId { get; set; }
    public Bet? Bet { get; set; }

    public BalanceTransactionType Type { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; } // Positive adds funds, negative subtracts

    [Column(TypeName = "decimal(18,2)")]
    public decimal BalanceAfter { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
