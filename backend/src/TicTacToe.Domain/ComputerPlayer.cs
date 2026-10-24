namespace TicTacToe.Domain;

/// <summary>
/// Rule-based opponent. Deterministic (no randomness) so every choice is testable.
/// </summary>
public static class ComputerPlayer
{
    public const Player Mark = Player.O;

    private const int Center = 4;
    private static readonly int[] Corners = [0, 2, 6, 8];

    /// <summary>Returns the board index (0..8) the computer plays.</summary>
    public static int ChooseMove(IReadOnlyList<Player?> board, Player me = Mark)
    {
        if (WinDetector.IsFull(board))
        {
            throw new InvalidOperationException("No empty cell available.");
        }

        // Priority order from the requirement:
        return FindCompletingCell(board, me)               // 1. win
            ?? FindCompletingCell(board, me.Opponent())    // 2. block
            ?? FirstEmpty(board, [Center])                 // 3. center
            ?? FirstEmpty(board, Corners)                  // 4. corner
            ?? FirstEmpty(board, Enumerable.Range(0, 9))!.Value; // 5. any
    }

    /// <summary>A cell that would complete a line of three for <paramref name="player"/>.</summary>
    private static int? FindCompletingCell(IReadOnlyList<Player?> board, Player player)
    {
        foreach (var line in WinDetector.Lines)
        {
            var mine = line.Count(i => board[i] == player);
            var empty = line.Where(i => board[i] is null).ToArray();
            if (mine == 2 && empty.Length == 1)
            {
                return empty[0];
            }
        }

        return null;
    }

    private static int? FirstEmpty(IReadOnlyList<Player?> board, IEnumerable<int> candidates)
    {
        foreach (var i in candidates)
        {
            if (board[i] is null)
            {
                return i;
            }
        }

        return null;
    }
}
