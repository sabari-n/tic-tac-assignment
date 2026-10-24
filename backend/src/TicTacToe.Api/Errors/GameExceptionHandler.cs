using Microsoft.AspNetCore.Diagnostics;
using TicTacToe.Api.Services;
using TicTacToe.Domain;

namespace TicTacToe.Api.Errors;

/// <summary>
/// Maps domain exceptions to RFC 7807 ProblemDetails responses:
/// 404 unknown game, 400 malformed move (outside the board), 409 move/undo not allowed now.
/// Anything else falls through to the default 500 handler.
/// </summary>
public sealed class GameExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, code) = exception switch
        {
            GameNotFoundException => (StatusCodes.Status404NotFound, "Game not found", "GameNotFound"),
            GameRuleException { Error: GameRuleError.OutOfBounds } rule => (StatusCodes.Status400BadRequest, "Invalid move", rule.Error.ToString()),
            GameRuleException rule => (StatusCodes.Status409Conflict, "Action not allowed", rule.Error.ToString()),
            _ => (0, string.Empty, string.Empty),
        };

        if (status == 0)
        {
            return false;
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = status,
                Title = title,
                Detail = exception.Message,
                Extensions = { ["code"] = code },
            },
        });
    }
}
