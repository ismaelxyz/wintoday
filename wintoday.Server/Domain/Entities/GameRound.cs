using wintoday.Server.Domain.Enums;

namespace wintoday.Server.Domain.Entities;

public class GameRound
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int Number { get; set; } // 0-36
    public RouletteColor Color { get; set; } // Random independent color (Red/Black) even for 0
    public bool BetCommitted { get; set; } = false; // Prevent multiple commits
    public ICollection<Bet> Bets { get; set; } = new List<Bet>();
}
