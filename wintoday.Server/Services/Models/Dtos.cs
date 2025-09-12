using wintoday.Server.Domain.Enums;

namespace wintoday.Server.Services.Models;

public record SpinResultDto(Guid RoundId, int Number, RouletteColor Color, DateTime CreatedAtUtc);

public record BetOutcomeDto(Guid RoundId, Guid BetId, int Number, RouletteColor Color, decimal Wager, decimal Profit, decimal NewBalance, bool Won, string BetType, object? Criteria);

// History item (simplified: balance after not returned; can be derived if needed by querying transactions)
public record BetHistoryItemDto(Guid RoundId, Guid BetId, int Number, RouletteColor Color, DateTime RoundCreatedAtUtc, decimal Wager, decimal Profit, bool Won, string BetType, object? Criteria);

// Session save (batch of bets played client-side before persisting)
public class SaveSessionRequest
{
    public string PlayerName { get; set; } = string.Empty;
    public List<SaveSessionBetItem> Bets { get; set; } = new();
}

public class SaveSessionBetItem
{
    public decimal Wager { get; set; }
    public string BetType { get; set; } = string.Empty; // color | colorParity | exact
    public string? Color { get; set; }
    public bool? IsEven { get; set; }
    public int? Number { get; set; }
    public int NumberResult { get; set; } // Resulting wheel number (0-36)
    public string ColorResult { get; set; } = string.Empty; // Resulting wheel color string
    public DateTime? PlayedAtUtc { get; set; } // Optional client timestamp
}

public record SessionSaveResultDto(decimal StartingFunds, decimal EndingFunds, IReadOnlyList<BetOutcomeDto> Outcomes);

public record PlayerBalanceDto(string Name, decimal Funds);

public class LoginRequest
{
    public string PlayerName { get; set; } = string.Empty;
}

public class CommitBetRequest
{
    public Guid RoundId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public decimal Wager { get; set; }
    public string BetType { get; set; } = string.Empty; // color | colorParity | exact
    public string? Color { get; set; }
    public bool? IsEven { get; set; }
    public int? Number { get; set; }
}
