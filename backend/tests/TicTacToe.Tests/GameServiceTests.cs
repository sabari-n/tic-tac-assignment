using TicTacToe.Api.Contracts;
using TicTacToe.Api.Services;
using TicTacToe.Domain;

namespace TicTacToe.Tests;

public class GameServiceTests
{
    private readonly GameService _service = new(new InMemoryGameStore(), new Scoreboard());

    private GameStateResponse PlayTwoPlayer(params (int Row, int Col)[] cells)
    {
        var state = _service.Create(GameMode.TwoPlayer);
        foreach (var (row, col) in cells)
        {
            state = _service.MakeMove(state.Id, state.CurrentPlayer, row, col);
        }

        return state;
    }

    [Fact]
    public void CompletedGame_UpdatesScoreboardExactlyOnce()
    {
        var state = PlayTwoPlayer((0, 0), (1, 0), (0, 1), (1, 1), (0, 2)); // X wins

        Assert.Equal(new ScoreboardResponse(1, 0, 0), state.Scoreboard);

        // Further actions on the completed game must not count it again.
        Assert.Throws<GameRuleException>(() => _service.MakeMove(state.Id, state.CurrentPlayer, 2, 2));
        Assert.Throws<GameRuleException>(() => _service.Undo(state.Id));
        _service.Get(state.Id);

        Assert.Equal(new ScoreboardResponse(1, 0, 0), _service.GetScoreboard());
    }

    [Fact]
    public void DrawnGame_IncrementsDraws()
    {
        var state = PlayTwoPlayer((0, 0), (0, 1), (0, 2), (1, 1), (1, 0), (1, 2), (2, 1), (2, 0), (2, 2));

        Assert.Equal(new ScoreboardResponse(0, 0, 1), state.Scoreboard);
    }

    [Fact]
    public void ComputerWin_IsRecordedForO()
    {
        var state = _service.Create(GameMode.VsComputer);

        // X(0,1) O center; X(1,0) O corner (0,0); X(2,1) ignores the threat, O wins on 0-4-8.
        state = _service.MakeMove(state.Id, Player.X, 0, 1);
        state = _service.MakeMove(state.Id, Player.X, 1, 0);
        state = _service.MakeMove(state.Id, Player.X, 2, 1);

        Assert.Equal(GameStatus.Won, state.Status);
        Assert.Equal(Player.O, state.Winner);
        Assert.Equal(new ScoreboardResponse(0, 1, 0), state.Scoreboard);
    }

    [Fact]
    public void ResetGame_KeepsScoreboard()
    {
        var state = PlayTwoPlayer((0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

        state = _service.Reset(state.Id);

        Assert.Empty(state.Moves);
        Assert.Equal(new ScoreboardResponse(1, 0, 0), state.Scoreboard);
    }

    [Fact]
    public void ResetScoreboard_ZeroesCounts()
    {
        PlayTwoPlayer((0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

        Assert.Equal(new ScoreboardResponse(0, 0, 0), _service.ResetScoreboard());
    }

    [Fact]
    public void InvalidMove_DoesNotTouchScoreboard()
    {
        var state = PlayTwoPlayer((0, 0));

        Assert.Throws<GameRuleException>(() => _service.MakeMove(state.Id, Player.O, 0, 0));

        Assert.Equal(new ScoreboardResponse(0, 0, 0), _service.GetScoreboard());
    }

    [Fact]
    public void UnknownGame_Throws()
    {
        Assert.Throws<GameNotFoundException>(() => _service.Get(Guid.NewGuid()));
    }
}
