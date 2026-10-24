using TicTacToe.Domain;
using static TicTacToe.Tests.TestHelpers;

namespace TicTacToe.Tests;

public class GameTests
{
    // ---------- New game / valid move / turns ----------

    [Fact]
    public void NewGame_StartsEmpty_WithXToMove()
    {
        var game = new Game(GameMode.TwoPlayer);

        Assert.All(game.Board, cell => Assert.Null(cell));
        Assert.Empty(game.Moves);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.False(game.CanUndo);
    }

    [Fact]
    public void ValidMove_PlacesMark_AndRecordsHistory()
    {
        var game = new Game(GameMode.TwoPlayer);

        game.MakeMove(Player.X, 1, 2);

        Assert.Equal(Player.X, game.Board[1 * 3 + 2]);
        Assert.Equal(new Move(1, Player.X, 1, 2), Assert.Single(game.Moves));
        Assert.True(game.CanUndo);
    }

    [Fact]
    public void Turns_AlternateAfterEachValidMove()
    {
        var game = new Game(GameMode.TwoPlayer);

        game.MakeMove(Player.X, 0, 0);
        Assert.Equal(Player.O, game.CurrentPlayer);

        game.MakeMove(Player.O, 1, 1);
        Assert.Equal(Player.X, game.CurrentPlayer);
    }

    // ---------- Invalid moves (state must not change) ----------

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(3, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 3)]
    public void Move_OutsideBoard_IsRejected(int row, int col)
    {
        var game = new Game(GameMode.TwoPlayer);

        var ex = Assert.Throws<GameRuleException>(() => game.MakeMove(Player.X, row, col));

        Assert.Equal(GameRuleError.OutOfBounds, ex.Error);
        Assert.Empty(game.Moves);
        Assert.Equal(Player.X, game.CurrentPlayer);
    }

    [Fact]
    public void Move_OnOccupiedCell_IsRejected_AndTurnDoesNotChange()
    {
        var game = Play((0, 0));

        var ex = Assert.Throws<GameRuleException>(() => game.MakeMove(Player.O, 0, 0));

        Assert.Equal(GameRuleError.CellOccupied, ex.Error);
        Assert.Equal(Player.X, game.Board[0]);
        Assert.Single(game.Moves);
        Assert.Equal(Player.O, game.CurrentPlayer);
    }

    [Fact]
    public void Move_ByWrongPlayer_IsRejected()
    {
        var game = new Game(GameMode.TwoPlayer);

        var ex = Assert.Throws<GameRuleException>(() => game.MakeMove(Player.O, 0, 0));

        Assert.Equal(GameRuleError.WrongPlayer, ex.Error);
        Assert.Empty(game.Moves);
        Assert.Equal(Player.X, game.CurrentPlayer);
    }

    [Fact]
    public void Move_AfterGameCompleted_IsRejected()
    {
        var game = Play((0, 0), (1, 0), (0, 1), (1, 1), (0, 2)); // X wins row 0

        var ex = Assert.Throws<GameRuleException>(() => game.MakeMove(game.CurrentPlayer, 2, 2));

        Assert.Equal(GameRuleError.GameAlreadyCompleted, ex.Error);
        Assert.Equal(5, game.Moves.Count);
        Assert.Null(game.Board[8]);
    }

    // ---------- Win detection ----------

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RowWin_IsDetected(int row)
    {
        var other = (row + 1) % 3;

        var game = Play((row, 0), (other, 0), (row, 1), (other, 1), (row, 2));

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.Equal([row * 3, row * 3 + 1, row * 3 + 2], game.WinningCells);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ColumnWin_IsDetected(int col)
    {
        var other = (col + 1) % 3;

        var game = Play((0, col), (0, other), (1, col), (1, other), (2, col));

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.Equal([col, col + 3, col + 6], game.WinningCells);
    }

    [Fact]
    public void MainDiagonalWin_IsDetected()
    {
        var game = Play((0, 0), (0, 1), (1, 1), (0, 2), (2, 2));

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal([0, 4, 8], game.WinningCells);
    }

    [Fact]
    public void AntiDiagonalWin_IsDetected()
    {
        var game = Play((0, 2), (0, 0), (1, 1), (0, 1), (2, 0));

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal([2, 4, 6], game.WinningCells);
    }

    [Fact]
    public void PlayerO_CanWin()
    {
        var game = Play((0, 0), (1, 0), (0, 1), (1, 1), (2, 2), (1, 2));

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.O, game.Winner);
        Assert.Equal([3, 4, 5], game.WinningCells);
    }

    // ---------- Draw ----------

    [Fact]
    public void FullBoardWithoutWinner_IsDraw()
    {
        // X O X
        // X O O
        // O X X
        var game = Play((0, 0), (0, 1), (0, 2), (1, 1), (1, 0), (1, 2), (2, 1), (2, 0), (2, 2));

        Assert.Equal(GameStatus.Draw, game.Status);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
        Assert.False(game.CanUndo);
    }

    // ---------- Reset ----------

    [Fact]
    public void Reset_ClearsBoardHistoryAndResult_KeepsIdAndMode()
    {
        var game = Play(GameMode.VsComputer, (0, 0));
        var id = game.Id;

        game.Reset();

        Assert.Equal(id, game.Id);
        Assert.Equal(GameMode.VsComputer, game.Mode);
        Assert.All(game.Board, cell => Assert.Null(cell));
        Assert.Empty(game.Moves);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(GameStatus.InProgress, game.Status);
    }

    [Fact]
    public void Reset_AfterWin_ClearsWinnerAndAllowsPlay()
    {
        var game = Play((0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

        game.Reset();

        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
        game.MakeMove(Player.X, 0, 0); // does not throw
    }

    // ---------- Undo: two-player ----------

    [Fact]
    public void Undo_TwoPlayer_RemovesOnlyLastMove_AndRestoresTurn()
    {
        var game = Play((0, 0), (1, 1)); // X plays, O plays

        game.Undo();

        Assert.Equal(new Move(1, Player.X, 0, 0), Assert.Single(game.Moves));
        Assert.Null(game.Board[4]);
        Assert.Equal(Player.O, game.CurrentPlayer); // O's turn again
    }

    [Fact]
    public void Undo_WithNoMoves_IsRejected()
    {
        var game = new Game(GameMode.TwoPlayer);

        var ex = Assert.Throws<GameRuleException>(game.Undo);

        Assert.Equal(GameRuleError.NothingToUndo, ex.Error);
    }

    [Fact]
    public void Undo_AfterGameCompleted_IsRejected() // Option A
    {
        var game = Play((0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

        var ex = Assert.Throws<GameRuleException>(game.Undo);

        Assert.Equal(GameRuleError.GameAlreadyCompleted, ex.Error);
        Assert.False(game.CanUndo);
        Assert.Equal(GameStatus.Won, game.Status);
    }

    // ---------- Computer mode ----------

    [Fact]
    public void ComputerMode_ComputerRepliesAutomatically()
    {
        var game = Play(GameMode.VsComputer, (0, 0));

        Assert.Equal(2, game.Moves.Count);
        Assert.Equal(new Move(2, Player.O, 1, 1), game.Moves[1]); // takes center
        Assert.Equal(Player.X, game.CurrentPlayer);
    }

    [Fact]
    public void ComputerMode_HumanCannotPlayO()
    {
        var game = Play(GameMode.VsComputer, (0, 0));

        var ex = Assert.Throws<GameRuleException>(() => game.MakeMove(Player.O, 2, 2));

        Assert.Equal(GameRuleError.WrongPlayer, ex.Error);
    }

    [Fact]
    public void ComputerMode_ComputerDoesNotMoveAfterHumanWins()
    {
        // X forks the computer: X(0,0) O(1,1) X(2,2) O(0,2) X(2,0) O blocks (2,1) X wins col 0.
        var game = Play(GameMode.VsComputer, (0, 0), (2, 2), (2, 0), (1, 0));

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.Equal(7, game.Moves.Count);
        Assert.Equal(Player.X, game.Moves[^1].Player);
    }

    [Fact]
    public void Undo_ComputerMode_RemovesComputerAndHumanMove()
    {
        var game = Play(GameMode.VsComputer, (0, 0)); // X plays, O computer plays

        game.Undo();

        Assert.Empty(game.Moves);
        Assert.All(game.Board, cell => Assert.Null(cell));
        Assert.Equal(Player.X, game.CurrentPlayer);
    }

    [Fact]
    public void Undo_ComputerMode_KeepsEarlierHistoryAccurate()
    {
        var game = Play(GameMode.VsComputer, (0, 0), (0, 1)); // O center, then O blocks (0,2)

        game.Undo();

        Assert.Equal(
            [new Move(1, Player.X, 0, 0), new Move(2, Player.O, 1, 1)],
            game.Moves);
        Assert.Null(game.Board[1]);
        Assert.Null(game.Board[2]);
        Assert.Equal(Player.X, game.CurrentPlayer);
    }
}
