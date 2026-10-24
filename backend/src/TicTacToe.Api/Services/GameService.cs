using TicTacToe.Api.Contracts;
using TicTacToe.Domain;

namespace TicTacToe.Api.Services;

public sealed class GameNotFoundException(Guid id) : Exception($"Game '{id}' was not found.");

/// <summary>
/// Coordinates the store, the domain and the scoreboard. Game rules live in
/// <see cref="Game"/>; this class only adds locking and score recording.
/// </summary>
public sealed class GameService(IGameStore store, Scoreboard scoreboard)
{
    public GameStateResponse Create(GameMode mode)
    {
        var game = new Game(mode);
        store.Add(game);
        return ToResponse(game);
    }

    public GameStateResponse Get(Guid id) => WithLock(id, _ => { });

    public GameStateResponse MakeMove(Guid id, Player player, int row, int col) =>
        WithLock(id, game =>
        {
            game.MakeMove(player, row, col);

            // MakeMove throws on a completed game, so reaching here means the game was
            // InProgress before this call. If it is completed now, this request is the
            // single InProgress -> Won/Draw transition: record the result exactly once.
            if (game.IsCompleted)
            {
                scoreboard.Record(game);
            }
        });

    public GameStateResponse Undo(Guid id) => WithLock(id, game => game.Undo());

    public GameStateResponse Reset(Guid id) => WithLock(id, game => game.Reset());

    public ScoreboardResponse GetScoreboard() => ScoreboardResponse.From(scoreboard.Snapshot());

    public ScoreboardResponse ResetScoreboard()
    {
        scoreboard.Reset();
        return GetScoreboard();
    }

    // Game is not thread-safe; two quick clicks (or tabs) on the same game must not
    // interleave. The lock is per game, so different games never block each other.
    private GameStateResponse WithLock(Guid id, Action<Game> action)
    {
        if (!store.TryGet(id, out var game))
        {
            throw new GameNotFoundException(id);
        }

        lock (game)
        {
            action(game);
            return ToResponse(game);
        }
    }

    private GameStateResponse ToResponse(Game game) => GameStateResponse.From(game, scoreboard.Snapshot());
}
