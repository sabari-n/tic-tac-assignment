using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/scoreboard")]
[Produces("application/json")]
public sealed class ScoreboardController(GameService games) : ControllerBase
{
    /// <summary>Get the session scoreboard.</summary>
    [HttpGet]
    [ProducesResponseType<ScoreboardResponse>(StatusCodes.Status200OK)]
    public ScoreboardResponse Get() => games.GetScoreboard();

    /// <summary>Reset all scoreboard counts to zero.</summary>
    [HttpPost("reset")]
    [ProducesResponseType<ScoreboardResponse>(StatusCodes.Status200OK)]
    public ScoreboardResponse Reset() => games.ResetScoreboard();
}
