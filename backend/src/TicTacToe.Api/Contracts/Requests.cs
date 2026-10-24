using System.ComponentModel.DataAnnotations;
using TicTacToe.Domain;

namespace TicTacToe.Api.Contracts;

// Parameters are nullable + [Required] so a missing field is a 400,
// instead of silently defaulting to the enum's first value / 0.
// (MVC requires validation attributes on the record's constructor parameters.)

public sealed record CreateGameRequest([Required] GameMode? Mode);

/// <summary>Row and Col are 0-based. The game id comes from the route.</summary>
public sealed record MoveRequest(
    [Required] Player? Player,
    [Required] int? Row,
    [Required] int? Col);
