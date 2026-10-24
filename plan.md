# Tic Tac Toe Assessment: Implementation Plan

## Context
This plan builds a technical-assessment submission from `requirement.txt`, which is the source of truth: an Angular + TypeScript frontend and a .NET Web API backend talking over REST. The backend owns all game state: board, turns, move history, status, and scoreboard. Required features are 2-player and vs-computer modes, undo by mode, a scoreboard, tests, and a README for the panel. The workspace currently holds only `requirement.txt`.

**Everything is built, tested, and run with Docker + Docker Compose** (Docker 28.4 and Compose v2.39 are installed). The .NET SDK and the Angular CLI are *not* installed locally, and they don't need to be: scaffolding, builds, and tests all run in containers. The panel only needs Docker, and runs one command: `docker compose up --build`.

The user wants to build this **step by step**, reviewing and running each step themselves. Each step below is a small, reviewable unit with its *why* and a *verify* check. Implementation pauses after each step.

### Decisions (confirmed with user)
| Topic | Choice | Rationale to state in README |
|---|---|---|
| Execution | **Docker Compose** (api + web), multi-stage Dockerfiles | One command for reviewers; no local SDK or version mismatch |
| Frontend | **Angular 21 LTS** (CLI pinned to 21.2.28), standalone components + signals, Vitest, `node:24-alpine` image (npm 11) | Newest Angular LTS line, pinned so scaffolding is reproducible. Node 24 (Active LTS) because the npm 10 bundled with Node 22 crashes resolving this lockfile (`edgesOut` bug) |
| Backend | **.NET 10 (LTS)** Web API, **Controllers** | Current LTS; controllers are familiar and give explicit routing |
| Undo after completion | **Option A: disabled** | Scoreboard stays final; no "un-scoring" logic |
| Computer-mode undo | **Pop moves until it is X's turn** (normally O+X) | Works for every reachable state |
| Storage | In-memory (`ConcurrentDictionary` + per-game lock) | Allowed by the spec; state resets when the container restarts (documented) |
| Who plays O in Computer mode | **Backend**, in the same `POST /moves` request | Backend is the source of truth; no extra round trip; computer can never play after game end |
| Browser → API routing | **nginx in the web container reverse-proxies `/api` → `api:8080`** | Same origin, so no CORS needed in Docker; the frontend uses relative `/api` URLs |

---

## Repository layout
```
tic-tak/
  README.md
  .gitignore
  docker-compose.yml           # services: api, web (+ test services under a "test" profile)
  backend/
    Dockerfile                 # stages: build -> test -> publish -> runtime (aspnet:10.0)
    .dockerignore
    TicTacToe.slnx                # .NET 10 default XML solution format
    src/TicTacToe.Domain/      # pure game rules, no ASP.NET dependency
    src/TicTacToe.Api/         # controllers, DTOs, in-memory store, DI
    tests/TicTacToe.Tests/     # xUnit tests (domain + service)
  frontend/
    Dockerfile                 # stages: deps -> test -> build (node:24-alpine) -> nginx runtime
    nginx.conf                 # serves the SPA + proxies /api to the api service
    .dockerignore
    (Angular app)
```
**Why a separate Domain project:** the rules (win detection, undo, computer AI) are plain C# with no web dependencies. That makes them easy to unit test and easy to explain on screen.

**Why multi-stage Dockerfiles:** the SDK and Node toolchains stay in the build stages, so the final images are small runtime images (`aspnet:10.0` and `nginx:alpine`). A dedicated `test` stage lets tests run in the same container image that builds the app.

---

## Step 0: Repo and Docker basics
- `git init`, add a `.gitignore` (bin/, obj/, node_modules/, dist/, .angular/), make the first commit, then create the GitHub repo.
- Scaffolding commands use throwaway containers with the project folder bind-mounted. For example:
  - `docker run --rm -v "${PWD}/backend:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet new ...`
  - `docker run --rm -it -v "${PWD}:/app" -w /app node:22 npx -y @angular/cli@21.2.28 new frontend --skip-install ...`
- **Verify:** `docker run --rm mcr.microsoft.com/dotnet/sdk:10.0 dotnet --version` prints 10.x.

## Step 1: Backend skeleton + Dockerfile
- Using the SDK container: `dotnet new sln`, `classlib` (Domain), `webapi --use-controllers` (Api), `xunit` (Tests), and project references.
- Program.cs sets up controllers, OpenAPI/Swagger, `ProblemDetails`, a `/health` endpoint, and listening on port 8080. A CORS policy for `http://localhost:4200` is kept only for the optional non-Docker `ng serve` workflow.
- `backend/Dockerfile`:
  - `build`: restore (copy the csproj files first to cache layers), then build.
  - `test`: `dotnet test`.
  - `publish`, then `final` on `aspnet:10.0`, exposing 8080.
- Minimal `docker-compose.yml` with just the `api` service (`8080:8080` mapped to host 5000 or similar).
- **Verify:** `docker compose up --build api`, then open Swagger at http://localhost:5000/swagger.

## Step 2: Domain model (TicTacToe.Domain)
- `Player` enum (X, O); `GameMode` (TwoPlayer, VsComputer); `GameStatus` (InProgress, Won, Draw).
- `Move` record: Number, Player, Row, Col (0-based internally, shown 1-based in the UI).
- `Game` class:
  - Holds `Id`, `Mode`, `Board` (Player?[9]), `CurrentPlayer`, `Status`, `Winner`, `WinningCells`, `Moves` list.
  - `MakeMove(player, row, col)` validates in this order: game finished, wrong player, out of bounds, occupied. It throws a `GameRuleException(GameRuleError, message)`, and undo errors use the same exception. On success it places the mark, records the move, and re-evaluates status. **In VsComputer mode the computer's O reply is also applied inside `MakeMove`**, so the whole rule set lives in the domain and can be tested without the API.
  - `CurrentPlayer` is **derived from the move count** (even means X), so undo restores the turn automatically.
  - `Undo()` throws when there are no moves or the game is finished (Option A). In TwoPlayer mode it pops 1 move. In VsComputer mode it pops until `CurrentPlayer == X` and at least one move is removed. **The board is rebuilt from the remaining moves**, which keeps history and board consistent by construction. Status is then recalculated.
  - `Reset()` clears the board and moves, sets status to InProgress and CurrentPlayer to X, and keeps the same Id and Mode.
- `WinDetector`: 8 static lines; returns the winner and winning cells, or a draw when the board is full.
- `ComputerPlayer.ChooseMove(board)`: priority is win, then block, then center (4), then corners (0,2,6,8), then the first empty cell. It is deterministic, which makes it testable.
- `Scoreboard` class: XWins, OWins, Draws; `Record(game)`; `Reset()`; guarded by a lock.
- **Why rebuild the board on undo:** the move list is the single source of truth, so there is no separate undo stack that can drift out of sync.

## Step 3: Domain tests (TicTacToe.Tests)
Write these tests now, before the API, to cover every required test case:
valid move, each invalid move (out of board, occupied, after completion, wrong player), turn switching, row/column/both-diagonal wins plus winning cells, draw, reset, undo two-player (O's move removed and it is O's turn again), undo computer (O+X removed and it is X's turn), undo disabled with no moves and after completion, and each computer priority rule (one test per rule).
- **Verify:** `docker compose --profile test run --rm --build api-tests` (a compose service that targets the `test` stage, which runs `dotnet test` as its CMD). All tests should pass.

## Step 4: Application service and API (TicTacToe.Api)
- `IGameStore` / `InMemoryGameStore` (singleton `ConcurrentDictionary<Guid, Game>`); `Scoreboard` is registered as a singleton.
- `GameService` runs each operation under a per-game lock:
  - `Create(mode)`
  - `Get(id)`
  - `Move(id, player, row, col)`: call `game.MakeMove` (which already includes the computer reply). **Record the score only when the status changes from InProgress to Won/Draw**, because that transition happens exactly once per game, which satisfies "update once". (With Option A, Undo can't reopen a finished game, and Reset starts a new game.)
  - `Undo`, `Reset`
- DTOs: `CreateGameRequest { mode }`, `MoveRequest { player, row, col }` (the game ID comes from the route), and `GameStateResponse { id, board[9] ("X"/"O"/null), currentPlayer, mode, status, winner, winningCells[], moves[], canUndo, scoreboard }`. The scoreboard is embedded so one response renders the whole screen.
- Controllers:

| Method | Route | Success | Errors |
|---|---|---|---|
| POST | /api/games | 201 + state | 400 bad mode |
| GET | /api/games/{id} | 200 | 404 |
| POST | /api/games/{id}/moves | 200 | 400 invalid input (out of board), 409 rule violation (occupied, finished, wrong player), 404 |
| POST | /api/games/{id}/undo | 200 | 409 nothing to undo / finished, 404 |
| POST | /api/games/{id}/reset | 200 | 404 |
| GET | /api/scoreboard | 200 | – |
| POST | /api/scoreboard/reset | 200 | – |

- Errors are returned as `ProblemDetails` with a readable `detail` that the UI shows.
- Add a few service tests: the scoreboard updates once, a computer move is applied after the human's move, and no computer move happens after X wins.
- Add HTTP integration tests (`WebApplicationFactory<Program>`) for status codes, string enums, and the ProblemDetails `code` extension.
- **Verify:** rebuild the api container, then play a full game through Swagger or a `.http` file (`TicTacToe.Api.http`, committed as API documentation).

## Step 5: Angular skeleton + Dockerfile + nginx
- Scaffold via the node:24-alpine container: `ng new frontend --standalone --style=scss --routing=false --skip-install`, then `provideHttpClient()` in app.config. All API calls use the relative base URL `/api`.
- `frontend/nginx.conf`: serve `dist/.../browser`, use `try_files $uri /index.html`, and add `location /api/ { proxy_pass http://api:8080; }`.
- `frontend/Dockerfile`:
  - `deps` (`npm ci`), `test` (`ng test` headless, no watch), `build` (`ng build`).
  - Runtime stage: `nginx:alpine` with the copied dist and nginx.conf.
- Add the `web` service to compose (`4200:80`, `depends_on: api`). Also add a `web-tests` service under the `test` profile.
- Optional: `proxy.conf.json` for anyone who prefers `ng serve` outside Docker (documented as secondary).
- **Verify:** `docker compose up --build` serves the default Angular page at http://localhost:4200, and http://localhost:4200/api/scoreboard returns JSON through the proxy.

## Step 6: Frontend API layer
- `models.ts` mirrors the response DTOs. `GameApiService` has `createGame`, `getGame`, `move`, `undo`, `reset`, `getScoreboard`, and `resetScoreboard`, all returning Observables.
- `GameStore` (a signal-based service) holds `state`, `error`, and `loading`. Every action replaces `state` with the backend response; **no rules are computed on the client.**
- Files follow the Angular v20+ naming (`core/game-api.ts` → `GameApi`, `core/game-store.ts` → `GameStore`). Specs for both are written here (HttpTestingController), so Step 8 focuses on components.

## Step 7: UI components
- `BoardComponent`: a 3×3 grid of buttons. A button is disabled when the cell is occupied, the game is over, or a request is loading. Winning cells get a `.win` class.
- `StatusBar`: shows the mode selector (switching mode calls createGame), the current player, and the winner or draw message.
- `MoveHistoryComponent`: a table of move number, player, and "Row r, Column c".
- `ScoreboardComponent`: X wins, O wins, and draws, plus a Reset Scoreboard button.
- Action buttons: Reset Game, and Undo Last Move (disabled when `!state.canUndo`).
- A simple responsive CSS grid for laptop screens, with an error banner for API errors.
- **Verify manually:** run `docker compose up --build` and go through every acceptance criterion in both modes.

## Step 8: Frontend tests
Write tests using Angular 21's default test runner (Vitest), run in the Dockerfile `test` stage:
- `GameApiService` calls the correct URLs (via `HttpTestingController`).
- `BoardComponent` renders X/O, disables occupied cells, and highlights winning cells.
- The Undo button is disabled when `canUndo` is false.
- **Verify:** `docker compose --profile test run --rm web-tests`.

## Step 9: README and submission
Write the README with the 12 required sections:
- **Quick start:** `docker compose up --build`, then open http://localhost:4200 (Swagger at http://localhost:5000/swagger).
- **Running tests:** `docker compose --profile test run --rm api-tests` and `docker compose --profile test run --rm web-tests`.
- "Run backend/frontend locally": the compose commands for each service, plus an optional non-Docker section (dotnet run / ng serve).
- The decision table above, and the API table with request/response examples.
- Assumptions: 0-based row/col in the API, Reset keeps the same game ID and mode, changing mode creates a new game, and the scoreboard is global to the api container because it's held in memory.
- Known limitations: in-memory state is lost when the container restarts, no auth, single instance.
- Future improvements: SQLite with a volume, minimax AI, SignalR.
- The AI workflow and prompt summary.

Then push to GitHub.

---

## Verification (end-to-end)
1. `docker compose --profile test run --rm api-tests` should be all green, and so should `docker compose --profile test run --rm web-tests`.
2. `docker compose up --build`, then open http://localhost:4200.
3. Two-player: play a row win and check the highlight, scoreboard +1, locked board, and Undo disabled. Play a draw. Undo mid-game should remove one move and give the turn back. Reset should keep the score.
4. Computer mode: O replies instantly. Set up a fork to see the computer block or win. Undo should remove 2 moves and return to X's turn.
5. Send invalid moves via the `.http` file against http://localhost:5000 (occupied, out of range, wrong player, after the game is over) and check for 400/409 with ProblemDetails.
6. `docker compose down`, then `up` again: the scoreboard resets to zero, which confirms the documented in-memory limitation.
