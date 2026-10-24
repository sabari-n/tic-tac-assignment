namespace TicTacToe.Domain;

public sealed record ScoreSnapshot(int XWins, int OWins, int Draws);

/// <summary>
/// Session-level score totals. Shared by all requests, so every access is locked.
/// Deciding *when* to record (exactly once per completed game) is the caller's job.
/// </summary>
public sealed class Scoreboard
{
    private readonly Lock _lock = new();
    private int _xWins;
    private int _oWins;
    private int _draws;

    public ScoreSnapshot Snapshot()
    {
        lock (_lock)
        {
            return new ScoreSnapshot(_xWins, _oWins, _draws);
        }
    }

    public void Record(Game game)
    {
        lock (_lock)
        {
            switch (game.Status)
            {
                case GameStatus.Won when game.Winner == Player.X:
                    _xWins++;
                    break;
                case GameStatus.Won when game.Winner == Player.O:
                    _oWins++;
                    break;
                case GameStatus.Draw:
                    _draws++;
                    break;
                default:
                    throw new InvalidOperationException("Only a completed game can be recorded.");
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _xWins = _oWins = _draws = 0;
        }
    }
}
