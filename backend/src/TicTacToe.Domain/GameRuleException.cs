namespace TicTacToe.Domain;

public enum GameRuleError
{
    GameAlreadyCompleted,
    WrongPlayer,
    OutOfBounds,
    CellOccupied,
    NothingToUndo,
}

/// <summary>
/// Thrown when an action breaks a game rule. The <see cref="Error"/> code lets the
/// API layer map each case to an HTTP status without parsing messages.
/// </summary>
public sealed class GameRuleException(GameRuleError error, string message) : Exception(message)
{
    public GameRuleError Error { get; } = error;
}
