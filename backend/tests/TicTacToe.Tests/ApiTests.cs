using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TicTacToe.Tests;

/// <summary>
/// End-to-end HTTP tests against the real pipeline (routing, JSON, validation,
/// error mapping). The app is shared by the class, so no test asserts absolute
/// scoreboard values.
/// </summary>
public class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<JsonElement> CreateGame(string mode = "TwoPlayer")
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { mode });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadJson(response);
    }

    private Task<HttpResponseMessage> Move(JsonElement game, string player, int row, int col) =>
        _client.PostAsJsonAsync($"/api/games/{game.GetProperty("id")}/moves", new { player, row, col });

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task CreateGame_Returns201_WithLocationAndInitialState()
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { mode = "TwoPlayer" });
        var game = await ReadJson(response);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.EndsWith($"/api/games/{game.GetProperty("id")}", response.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("X", game.GetProperty("currentPlayer").GetString());
        Assert.Equal("InProgress", game.GetProperty("status").GetString());
        Assert.Equal(9, game.GetProperty("board").GetArrayLength());
        Assert.False(game.GetProperty("canUndo").GetBoolean());
    }

    [Fact]
    public async Task CreateGame_WithoutMode_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ValidMove_Returns200_WithUpdatedState()
    {
        var game = await CreateGame();

        var response = await Move(game, "X", 1, 1);
        var state = await ReadJson(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("X", state.GetProperty("board")[4].GetString());
        Assert.Equal("O", state.GetProperty("currentPlayer").GetString());
        Assert.Equal(1, state.GetProperty("moves").GetArrayLength());
    }

    [Fact]
    public async Task ComputerMode_ResponseIncludesComputerReply()
    {
        var game = await CreateGame("VsComputer");

        var state = await ReadJson(await Move(game, "X", 0, 0));

        var moves = state.GetProperty("moves");
        Assert.Equal(2, moves.GetArrayLength());
        Assert.Equal("O", moves[1].GetProperty("player").GetString());
        Assert.Equal("X", state.GetProperty("currentPlayer").GetString());
    }

    [Theory]
    [InlineData("O", 0, 0, HttpStatusCode.Conflict, "WrongPlayer")]
    [InlineData("X", 3, 0, HttpStatusCode.BadRequest, "OutOfBounds")]
    public async Task InvalidMove_ReturnsProblemDetails(string player, int row, int col, HttpStatusCode status, string code)
    {
        var game = await CreateGame();

        var response = await Move(game, player, row, col);
        var problem = await ReadJson(response);

        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(code, problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Move_OnOccupiedCell_Returns409()
    {
        var game = await CreateGame();
        await Move(game, "X", 0, 0);

        var response = await Move(game, "O", 0, 0);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("CellOccupied", (await ReadJson(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Undo_WithNoMoves_Returns409()
    {
        var game = await CreateGame();

        var response = await _client.PostAsync($"/api/games/{game.GetProperty("id")}/undo", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("NothingToUndo", (await ReadJson(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task UnknownGame_Returns404()
    {
        var response = await _client.GetAsync($"/api/games/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ScoreboardEndpoints_ReturnCounts()
    {
        var reset = await ReadJson(await _client.PostAsync("/api/scoreboard/reset", null));
        var get = await ReadJson(await _client.GetAsync("/api/scoreboard"));

        Assert.Equal(0, reset.GetProperty("xWins").GetInt32());
        Assert.True(get.TryGetProperty("oWins", out _));
        Assert.True(get.TryGetProperty("draws", out _));
    }
}
