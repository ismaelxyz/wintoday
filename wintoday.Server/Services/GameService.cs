using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using wintoday.Server.Domain.Entities;
using wintoday.Server.Domain.Enums;
using wintoday.Server.Infrastructure;
using wintoday.Server.Services.Models;

namespace wintoday.Server.Services;

public class GameService(GameDbContext db, IOptions<GameOptions> options, ILogger<GameService> logger) : IGameService
{
    private readonly GameOptions _opts = options.Value;
    private readonly ILogger<GameService> _logger = logger;

    public async Task<PlayerBalanceDto> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.PlayerName)) throw new ArgumentException("PlayerName required");
        var normalized = request.PlayerName.Trim().ToUpperInvariant();
        var player = await db.Players.FirstOrDefaultAsync(p => p.NormalizedName == normalized, ct);
        if (player == null)
        {
            player = new Player { Name = request.PlayerName.Trim(), NormalizedName = normalized, Funds = _opts.InitialFunds };
            db.Players.Add(player);
            db.Transactions.Add(new BalanceTransaction
            {
                Player = player,
                Amount = _opts.InitialFunds,
                BalanceAfter = player.Funds,
                Type = BalanceTransactionType.InitialFunds
            });
            await db.SaveChangesAsync(ct);
        }
        return new PlayerBalanceDto(player.Name, player.Funds);
    }

    public async Task<SpinResultDto> SpinAsync(string playerName, CancellationToken ct)
    {
        var player = await FindPlayerOrThrow(playerName, ct);
        var number = Random.Shared.Next(0, 37); // 0-36
        // Color now independent random (only Red or Black) regardless of number
        var color = Random.Shared.Next(0, 2) == 0 ? RouletteColor.Red : RouletteColor.Black;
        var round = new GameRound { PlayerId = player.Id, Number = number, Color = color };
        db.Rounds.Add(round);
        await db.SaveChangesAsync(ct);
        return new SpinResultDto(round.Id, number, color, round.CreatedAtUtc);
    }

    public async Task<BetOutcomeDto> CommitBetAsync(CommitBetRequest request, CancellationToken ct)
    {
        if (request.Wager <= 0) throw new ArgumentException("Wager must be > 0");
        var player = await FindPlayerOrThrow(request.PlayerName, ct);
        var round = await db.Rounds.Include(r => r.Bets).FirstOrDefaultAsync(r => r.Id == request.RoundId && r.PlayerId == player.Id, ct);
        if (round == null) throw new InvalidOperationException("Round not found for player");
        if (round.BetCommitted) throw new InvalidOperationException("Bet already committed for this round");
        if (player.Funds < request.Wager) throw new InvalidOperationException("Insufficient funds");

        var (betType, criteriaDesc, profit, won) = EvaluateBet(round.Number, round.Color, request);

        using var trx = await db.Database.BeginTransactionAsync(ct);
        await db.Entry(player).ReloadAsync(ct);
        if (player.Funds < request.Wager) throw new InvalidOperationException("Insufficient funds");

        player.Funds -= request.Wager; // Deduct wager immediately
        db.Transactions.Add(new BalanceTransaction
        {
            Player = player,
            Amount = -request.Wager,
            BalanceAfter = player.Funds,
            Type = BalanceTransactionType.BetWager
        });

        var bet = new Bet
        {
            PlayerId = player.Id,
            RoundId = round.Id,
            Type = betType,
            Color = ParseColor(request.Color),
            IsEven = request.IsEven,
            Number = request.Number,
            Wager = request.Wager,
            ResultStatus = won ? BetResultStatus.Won : BetResultStatus.Lost,
            Payout = won ? profit : 0m
        };
        db.Bets.Add(bet);
        round.BetCommitted = true;

        if (won && profit > 0)
        {
            // Add back original wager + profit
            player.Funds += request.Wager + profit;
            db.Transactions.Add(new BalanceTransaction
            {
                Player = player,
                Bet = bet,
                Amount = request.Wager + profit,
                BalanceAfter = player.Funds,
                Type = BalanceTransactionType.BetPayout
            });
        }

        await db.SaveChangesAsync(ct);
        await trx.CommitAsync(ct);

        return new BetOutcomeDto(round.Id, bet.Id, round.Number, round.Color, request.Wager, won ? profit : 0m, player.Funds, won, betType.ToString(), criteriaDesc);
    }

    private async Task<Player> FindPlayerOrThrow(string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Player name required");
        var normalized = name.Trim().ToUpperInvariant();
        var player = await db.Players.FirstOrDefaultAsync(p => p.NormalizedName == normalized, ct);
        if (player == null) throw new UnauthorizedAccessException("Player not registered");
        return player;
    }

    private static RouletteColor? ParseColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return null;
        return color.ToLowerInvariant() switch
        {
            "red" => RouletteColor.Red,
            "black" => RouletteColor.Black,
            _ => null
        };
    }

    private static (BetType betType, object? criteriaDesc, decimal profit, bool won) EvaluateBet(int spunNumber, RouletteColor spunColor, CommitBetRequest request)
    {
        var colorInput = ParseColor(request.Color);
        switch (request.BetType.ToLowerInvariant())
        {
            case "color":
                if (colorInput is null) throw new ArgumentException("Color required");
                var winColor = spunColor == colorInput;
                var profitColor = winColor ? request.Wager / 2m : 0m;
                return (BetType.Color, new { color = colorInput.ToString() }, profitColor, winColor);
            case "colorparity":
            case "color_parity":
                if (colorInput is null || request.IsEven is null) throw new ArgumentException("Color and parity required");
                var isEven = spunNumber % 2 == 0;
                var winParity = spunColor == colorInput && isEven == request.IsEven;
                var profitParity = winParity ? request.Wager : 0m;
                return (BetType.ColorParity, new { color = colorInput.ToString(), isEven = request.IsEven }, profitParity, winParity);
            case "exact":
                if (colorInput is null || request.Number is null) throw new ArgumentException("Color and number required");
                var winExact = spunNumber == request.Number && spunColor == colorInput;
                var profitExact = winExact ? request.Wager * 3m : 0m;
                return (BetType.ExactNumberAndColor, new { color = colorInput.ToString(), number = request.Number }, profitExact, winExact);
            default:
                throw new ArgumentException("Unsupported bet type");
        }
    }

    public async Task<PlayerBalanceDto> GetPlayerAsync(string name, CancellationToken ct)
    {
        var player = await FindPlayerOrThrow(name, ct);
        return new PlayerBalanceDto(player.Name, player.Funds);
    }

    public async Task<IReadOnlyList<BetHistoryItemDto>> GetBetHistoryAsync(string playerName, int take, CancellationToken ct)
    {
        var player = await FindPlayerOrThrow(playerName, ct);
        take = take <= 0 || take > 200 ? 50 : take; // sane bounds
        var raw = await (from b in db.Bets
                         join r in db.Rounds on b.RoundId equals r.Id
                         where b.PlayerId == player.Id
                         orderby r.CreatedAtUtc descending
                         select new
                         {
                             r.Id,
                             BetId = b.Id,
                             RoundNumber = r.Number,
                             RoundColor = r.Color,
                             r.CreatedAtUtc,
                             b.Wager,
                             Payout = b.Payout ?? 0m,
                             Won = b.ResultStatus == BetResultStatus.Won,
                             Type = b.Type,
                             BetColor = b.Color,
                             b.IsEven,
                             BetNumber = b.Number
                         }).Take(take).ToListAsync(ct);

        var list = raw.Select(x => new BetHistoryItemDto(
            x.Id,
            x.BetId,
            x.RoundNumber,
            x.RoundColor,
            x.CreatedAtUtc,
            x.Wager,
            x.Payout,
            x.Won,
            x.Type.ToString(),
            x.Type switch
            {
                BetType.Color => new { color = x.BetColor!.Value.ToString() } as object,
                BetType.ColorParity => new { color = x.BetColor!.Value.ToString(), isEven = x.IsEven } as object,
                BetType.ExactNumberAndColor => new { color = x.BetColor!.Value.ToString(), number = x.BetNumber } as object,
                _ => null!
            }
        )).ToList();
        return list;
    }

    public async Task<SessionSaveResultDto> SaveSessionAsync(SaveSessionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.PlayerName)) throw new ArgumentException("PlayerName required");
        if (request.Bets.Count == 0) throw new ArgumentException("No bets to save");
        var player = await FindPlayerOrThrow(request.PlayerName, ct);
        var startingFunds = player.Funds;

        using var trx = await db.Database.BeginTransactionAsync(ct);
        await db.Entry(player).ReloadAsync(ct);
        var outcomes = new List<BetOutcomeDto>(request.Bets.Count);

        foreach (var b in request.Bets)
        {
            if (b.Wager <= 0) throw new ArgumentException("Invalid wager in batch");
            if (player.Funds < b.Wager) throw new InvalidOperationException("Insufficient funds during session save");
            player.Funds -= b.Wager;
            db.Transactions.Add(new BalanceTransaction
            {
                Player = player,
                Amount = -b.Wager,
                BalanceAfter = player.Funds,
                Type = BalanceTransactionType.BetWager
            });

            // Build a synthetic round using provided result
            var parsedColorResult = ParseColor(b.ColorResult);
            if (parsedColorResult is null) throw new ArgumentException("Invalid ColorResult");
            var round = new GameRound { PlayerId = player.Id, Number = b.NumberResult, Color = parsedColorResult.Value, BetCommitted = true };
            db.Rounds.Add(round);

            // Reuse evaluation logic by constructing a CommitBetRequest analog
            var commitReq = new CommitBetRequest
            {
                PlayerName = request.PlayerName,
                RoundId = round.Id,
                Wager = b.Wager,
                BetType = b.BetType,
                Color = b.Color,
                IsEven = b.IsEven,
                Number = b.Number
            };
            var (betType, criteriaDesc, profit, won) = EvaluateBet(b.NumberResult, parsedColorResult.Value, commitReq);

            var bet = new Bet
            {
                PlayerId = player.Id,
                RoundId = round.Id,
                Type = betType,
                Color = ParseColor(b.Color),
                IsEven = b.IsEven,
                Number = b.Number,
                Wager = b.Wager,
                ResultStatus = won ? BetResultStatus.Won : BetResultStatus.Lost,
                Payout = won ? profit : 0m
            };
            db.Bets.Add(bet);

            if (won && profit > 0)
            {
                player.Funds += b.Wager + profit;
                db.Transactions.Add(new BalanceTransaction
                {
                    Player = player,
                    Bet = bet,
                    Amount = b.Wager + profit,
                    BalanceAfter = player.Funds,
                    Type = BalanceTransactionType.BetPayout
                });
            }

            outcomes.Add(new BetOutcomeDto(round.Id, bet.Id, round.Number, round.Color, b.Wager, won ? profit : 0m, player.Funds, won, betType.ToString(), criteriaDesc));
        }

        await db.SaveChangesAsync(ct);
        await trx.CommitAsync(ct);

        return new SessionSaveResultDto(startingFunds, player.Funds, outcomes);
    }
}
