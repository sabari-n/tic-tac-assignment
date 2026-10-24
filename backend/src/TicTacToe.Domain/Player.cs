namespace TicTacToe.Domain;

public enum Player
{
    X,
    O,
}

public static class PlayerExtensions
{
    public static Player Opponent(this Player player) => player == Player.X ? Player.O : Player.X;
}
