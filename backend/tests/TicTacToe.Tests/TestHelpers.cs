using TicTacToe.Domain;

namespace TicTacToe.Tests;

internal static class TestHelpers
{
    /// <summary>
    /// Plays the given cells in order, each by whoever's turn it is.
    /// In computer mode pass only the human (X) moves; O replies automatically.
    /// </summary>
    public static Game Play(GameMode mode, params (int Row, int Col)[] cells)
    {
        var game = new Game(mode);
        foreach (var (row, col) in cells)
        {
            game.MakeMove(game.CurrentPlayer, row, col);
        }

        return game;
    }

    public static Game Play(params (int Row, int Col)[] cells) => Play(GameMode.TwoPlayer, cells);

    /// <summary>"XO.X....." -> board, read row by row; '.' is empty.</summary>
    public static Player?[] ParseBoard(string cells) =>
        cells.Select(c => c switch
        {
            'X' => (Player?)Player.X,
            'O' => Player.O,
            _ => null,
        }).ToArray();
}
