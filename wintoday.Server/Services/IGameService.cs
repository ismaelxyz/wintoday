using wintoday.Server.Services.Models;

namespace wintoday.Server.Services;

public interface IGameService
{
    Task<PlayerBalanceDto> LoginAsync(LoginRequest request, CancellationToken ct); // Create or return existing
    Task<SpinResultDto> SpinAsync(string playerName, CancellationToken ct); // Prepares a round for player
    Task<BetOutcomeDto> CommitBetAsync(CommitBetRequest request, CancellationToken ct); // Commit bet to an existing round
    Task<PlayerBalanceDto> GetPlayerAsync(string name, CancellationToken ct);
    Task<IReadOnlyList<BetHistoryItemDto>> GetBetHistoryAsync(string playerName, int take, CancellationToken ct);
    Task<SessionSaveResultDto> SaveSessionAsync(SaveSessionRequest request, CancellationToken ct);
}
