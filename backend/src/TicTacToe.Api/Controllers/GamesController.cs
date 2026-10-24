using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/games")]
[Produces("application/json")]
public sealed class GamesController(GameService games) : ControllerBase
{
    /// <summary>Create a new game session.</summary>
    [HttpPost]
    [ProducesResponseType<GameStateResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<GameStateResponse> Create(CreateGameRequest request)
    {
        var state = games.Create(request.Mode!.Value);
        return CreatedAtAction(nameof(Get), new { id = state.Id }, state);
    }

    /// <summary>Get the current game state.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<GameStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public GameStateResponse Get(Guid id) => games.Get(id);

    /// <summary>Submit a move. In computer mode the response already includes the computer's reply.</summary>
    [HttpPost("{id:guid}/moves")]
    [ProducesResponseType<GameStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public GameStateResponse Move(Guid id, MoveRequest request) =>
        games.MakeMove(id, request.Player!.Value, request.Row!.Value, request.Col!.Value);

    /// <summary>Undo the last move (two-player) or the last move pair (computer mode).</summary>
    [HttpPost("{id:guid}/undo")]
    [ProducesResponseType<GameStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public GameStateResponse Undo(Guid id) => games.Undo(id);

    /// <summary>Clear the board and history; the scoreboard is unchanged.</summary>
    [HttpPost("{id:guid}/reset")]
    [ProducesResponseType<GameStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public GameStateResponse Reset(Guid id) => games.Reset(id);
}
