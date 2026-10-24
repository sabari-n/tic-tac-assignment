using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using TicTacToe.Domain;

namespace TicTacToe.Api.Services;

/// <summary>Abstraction so the in-memory store could be swapped for SQLite later.</summary>
public interface IGameStore
{
    void Add(Game game);

    bool TryGet(Guid id, [NotNullWhen(true)] out Game? game);
}

public sealed class InMemoryGameStore : IGameStore
{
    private readonly ConcurrentDictionary<Guid, Game> _games = new();

    public void Add(Game game) => _games[game.Id] = game;

    public bool TryGet(Guid id, [NotNullWhen(true)] out Game? game) => _games.TryGetValue(id, out game);
}
