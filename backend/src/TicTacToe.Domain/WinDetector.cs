namespace TicTacToe.Domain;

public sealed record WinResult(Player Winner, IReadOnlyList<int> Cells);

/// <summary>
/// Board cells are indexed 0..8, row by row: index = row * 3 + col.
/// </summary>
public static class WinDetector
{
    public static readonly IReadOnlyList<int[]> Lines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8], // rows
        [0, 3, 6], [1, 4, 7], [2, 5, 8], // columns
        [0, 4, 8], [2, 4, 6],            // diagonals
    ];

    public static WinResult? FindWinner(IReadOnlyList<Player?> board)
    {
        foreach (var line in Lines)
        {
            var first = board[line[0]];
            if (first is not null && board[line[1]] == first && board[line[2]] == first)
            {
                return new WinResult(first.Value, line);
            }
        }

        return null;
    }

    public static bool IsFull(IReadOnlyList<Player?> board) => board.All(cell => cell is not null);
}
