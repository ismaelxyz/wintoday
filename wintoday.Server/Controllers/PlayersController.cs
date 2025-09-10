using Microsoft.AspNetCore.Mvc;
using wintoday.Server.Services;
using wintoday.Server.Services.Models;

namespace wintoday.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController(IGameService gameService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<PlayerBalanceDto>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await gameService.LoginAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<PlayerBalanceDto>> Get(string name, CancellationToken ct)
    {
        try
        {
            var result = await gameService.GetPlayerAsync(name, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}
