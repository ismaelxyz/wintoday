using Microsoft.AspNetCore.Mvc;
using wintoday.Server.Services;
using wintoday.Server.Services.Models;

namespace wintoday.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController(IGameService gameService) : ControllerBase
{
    [HttpPost("spin/{playerName}")]
    public async Task<ActionResult<SpinResultDto>> Spin(string playerName, CancellationToken ct)
    {
        try { return Ok(await gameService.SpinAsync(playerName, ct)); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
    }

    [HttpPost("commit-bet")]
    public async Task<ActionResult<BetOutcomeDto>> CommitBet([FromBody] CommitBetRequest request, CancellationToken ct)
    {
        try { return Ok(await gameService.CommitBetAsync(request, ct)); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("history/{playerName}")]
    public async Task<ActionResult<IReadOnlyList<BetHistoryItemDto>>> History(string playerName, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        try { return Ok(await gameService.GetBetHistoryAsync(playerName, take, ct)); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
    }

    [HttpPost("save-session")]
    public async Task<ActionResult<SessionSaveResultDto>> SaveSession([FromBody] SaveSessionRequest request, CancellationToken ct)
    {
        try { return Ok(await gameService.SaveSessionAsync(request, ct)); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
