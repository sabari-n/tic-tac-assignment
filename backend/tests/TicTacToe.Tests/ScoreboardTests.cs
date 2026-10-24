using TicTacToe.Domain;
using static TicTacToe.Tests.TestHelpers;

namespace TicTacToe.Tests;

public class ScoreboardTests
{
    private static Game XWins() => Play((0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

    private static Game OWins() => Play((0, 0), (1, 0), (0, 1), (1, 1), (2, 2), (1, 2));

    private static Game Draw() => Play((0, 0), (0, 1), (0, 2), (1, 1), (1, 0), (1, 2), (2, 1), (2, 0), (2, 2));

    [Fact]
    public void Record_CountsEachResultType()
    {
        var scoreboard = new Scoreboard();

        scoreboard.Record(XWins());
        scoreboard.Record(XWins());
        scoreboard.Record(OWins());
        scoreboard.Record(Draw());

        Assert.Equal(new ScoreSnapshot(XWins: 2, OWins: 1, Draws: 1), scoreboard.Snapshot());
    }

    [Fact]
    public void Record_InProgressGame_IsRejected()
    {
        var scoreboard = new Scoreboard();

        Assert.Throws<InvalidOperationException>(() => scoreboard.Record(Play((0, 0))));
        Assert.Equal(new ScoreSnapshot(0, 0, 0), scoreboard.Snapshot());
    }

    [Fact]
    public void Reset_ZeroesAllCounts()
    {
        var scoreboard = new Scoreboard();
        scoreboard.Record(XWins());
        scoreboard.Record(Draw());

        scoreboard.Reset();

        Assert.Equal(new ScoreSnapshot(0, 0, 0), scoreboard.Snapshot());
    }
}
