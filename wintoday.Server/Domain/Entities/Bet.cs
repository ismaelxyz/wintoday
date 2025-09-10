using System.ComponentModel.DataAnnotations.Schema;
using wintoday.Server.Domain.Enums;

namespace wintoday.Server.Domain.Entities;

public class Bet
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    public Guid RoundId { get; set; }
    public GameRound Round { get; set; } = null!;

    public BetType Type { get; set; }
    public RouletteColor? Color { get; set; }
    public bool? IsEven { get; set; } // Only for ColorParity bets
    public int? Number { get; set; } // Only for ExactNumberAndColor

    [Column(TypeName = "decimal(18,2)")]
    public decimal Wager { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Payout { get; set; } // Profit (not including original wager). Null until resolved.

    public BetResultStatus ResultStatus { get; set; } = BetResultStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
