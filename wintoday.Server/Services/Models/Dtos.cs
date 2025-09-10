using wintoday.Server.Domain.Enums;

namespace wintoday.Server.Services.Models;

public record SpinResultDto(Guid RoundId, int Number, RouletteColor Color, DateTime CreatedAtUtc);

public record BetOutcomeDto(Guid RoundId, Guid BetId, int Number, RouletteColor Color, decimal Wager, decimal Profit, decimal NewBalance, bool Won, string BetType, object? Criteria);

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
