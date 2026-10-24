namespace TicTacToe.Domain;

/// <summary>One entry in the move history. Row and Col are 0-based.</summary>
public sealed record Move(int Number, Player Player, int Row, int Col);
