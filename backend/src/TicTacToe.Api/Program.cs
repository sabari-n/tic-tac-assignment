const string DevFrontendCors = "DevFrontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Consistent RFC 7807 error bodies for every 4xx/5xx response.
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

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
