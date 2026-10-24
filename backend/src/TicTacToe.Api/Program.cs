using System.Text.Json.Serialization;
using TicTacToe.Api.Errors;
using TicTacToe.Api.Services;
using TicTacToe.Domain;

const string DevFrontendCors = "DevFrontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    // Enums travel as "X", "VsComputer", "InProgress" rather than 0/1/2.
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

// Consistent RFC 7807 error bodies for every 4xx/5xx response.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GameExceptionHandler>();
builder.Services.AddHealthChecks();

// State lives in memory for the lifetime of the process, so these are singletons.
builder.Services.AddSingleton<IGameStore, InMemoryGameStore>();
builder.Services.AddSingleton<Scoreboard>();
builder.Services.AddSingleton<GameService>();

// Only needed when running `ng serve` outside Docker. In Docker, nginx proxies
// /api on the same origin, so the browser never makes a cross-origin call.
builder.Services.AddCors(options =>
    options.AddPolicy(DevFrontendCors, policy => policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// API docs are always on: documenting the contract is part of the assignment.
app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "TicTacToe API v1"));

app.UseCors(DevFrontendCors);

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Exposes the entry point to WebApplicationFactory in the integration tests.
public partial class Program;
