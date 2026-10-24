using TicTacToe.Domain;

namespace TicTacToe.Api.Contracts;

/// <summary>Everything the UI needs to render one screen, in one response.</summary>
public sealed record GameStateResponse(
    Guid Id,
    GameMode Mode,
    GameStatus Status,
    Player CurrentPlayer,
    Player? Winner,
    IReadOnlyList<Player?> Board,
    IReadOnlyList<int> WinningCells,
    IReadOnlyList<MoveResponse> Moves,
    bool CanUndo,
    ScoreboardResponse Scoreboard)
{
    public static GameStateResponse From(Game game, ScoreSnapshot score) => new(
        game.Id,
        game.Mode,
        game.Status,
        game.CurrentPlayer,
        game.Winner,
        game.Board.ToArray(),
        game.WinningCells.ToArray(),
        game.Moves.Select(m => new MoveResponse(m.Number, m.Player, m.Row, m.Col)).ToArray(),
        game.CanUndo,
        ScoreboardResponse.From(score));
}

/// <summary>Row and Col are 0-based.</summary>
public sealed record MoveResponse(int Number, Player Player, int Row, int Col);

public sealed record ScoreboardResponse(int XWins, int OWins, int Draws)
{
    public static ScoreboardResponse From(ScoreSnapshot score) => new(score.XWins, score.OWins, score.Draws);
}
