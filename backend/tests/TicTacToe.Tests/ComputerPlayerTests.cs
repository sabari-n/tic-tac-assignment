using TicTacToe.Domain;
using static TicTacToe.Tests.TestHelpers;

namespace TicTacToe.Tests;

/// <summary>One test per rule in the required priority order. Boards are row by row.</summary>
public class ComputerPlayerTests
{
    [Fact]
    public void Priority1_PlaysWinningMove_EvenWhenXCouldAlsoWin()
    {
        var board = ParseBoard(
            "OO." +
            "XX." +
            "X..");

        Assert.Equal(2, ComputerPlayer.ChooseMove(board)); // win, not block at 5
    }

    [Fact]
    public void Priority2_BlocksX()
    {
        var board = ParseBoard(
            "XX." +
            ".O." +
            "...");

        Assert.Equal(2, ComputerPlayer.ChooseMove(board));
    }

    [Fact]
    public void Priority3_TakesCenter()
    {
        var board = ParseBoard(
            "X.." +
            "..." +
            "...");

        Assert.Equal(4, ComputerPlayer.ChooseMove(board));
    }

    [Fact]
    public void Priority4_TakesCorner()
    {
        var board = ParseBoard(
            "..." +
            ".X." +
            "...");

        Assert.Equal(0, ComputerPlayer.ChooseMove(board));
    }

    [Fact]
    public void Priority5_TakesAnyAvailableCell()
    {
        // Center and corners taken; no line can be won or must be blocked.
        var board = ParseBoard(
            "XOX" +
            ".X." +
            "OXO");

        Assert.Equal(3, ComputerPlayer.ChooseMove(board));
    }

    [Fact]
    public void FullBoard_Throws()
    {
        var board = ParseBoard("XOXXOOOXX");

        Assert.Throws<InvalidOperationException>(() => ComputerPlayer.ChooseMove(board));
    }
}
