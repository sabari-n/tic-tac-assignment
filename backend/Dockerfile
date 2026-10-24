# ---- build: restore + compile the whole solution ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only project files first so `dotnet restore` is cached until a
# dependency changes, not on every source edit.
COPY TicTacToe.slnx ./
COPY src/TicTacToe.Domain/TicTacToe.Domain.csproj src/TicTacToe.Domain/
COPY src/TicTacToe.Api/TicTacToe.Api.csproj src/TicTacToe.Api/
COPY tests/TicTacToe.Tests/TicTacToe.Tests.csproj tests/TicTacToe.Tests/
RUN dotnet restore TicTacToe.slnx

COPY . .
RUN dotnet build TicTacToe.slnx -c Release --no-restore

# ---- test: run with `docker compose --profile test run --rm api-tests` ----
FROM build AS test
CMD ["dotnet", "test", "TicTacToe.slnx", "-c", "Release", "--no-build", "--logger", "console;verbosity=normal"]

# ---- publish: framework-dependent output of the API only ----
FROM build AS publish
RUN dotnet publish src/TicTacToe.Api/TicTacToe.Api.csproj -c Release --no-build -o /app/publish

# ---- final: small runtime image, no SDK ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Non-root user provided by the official .NET images.
USER $APP_UID
# .NET 8+ images listen on 8080 by default (ASPNETCORE_HTTP_PORTS).
EXPOSE 8080
ENTRYPOINT ["dotnet", "TicTacToe.Api.dll"]
