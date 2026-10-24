# Tic Tac Toe — Angular + .NET

A browser-based Tic Tac Toe game. The Angular frontend talks to a .NET Web API over REST, and the backend owns all game state: board, turns, move history, result and scoreboard. You can play two-player or against a rule-based computer opponent, undo moves, and keep a running scoreboard.

## Quick start

The only prerequisite is **Docker Desktop** (or Docker Engine with Compose v2).

```bash
docker compose up --build
```

| What | URL |
|---|---|
| Game | http://localhost:4200 |
| API docs (Swagger UI) | http://localhost:5000/swagger |
| OpenAPI document | http://localhost:5000/openapi/v1.json |

To stop everything, run `docker compose down`. Game state is held in memory, so it resets when the API container restarts.

---

## 1. Project overview

```
tic-tak/
├── docker-compose.yml          api + web services; api-tests / web-tests under the "test" profile
├── backend/                    .NET 10 solution (TicTacToe.slnx) — see backend/README.md
│   ├── Dockerfile              build → test → publish → aspnet runtime
│   ├── src/TicTacToe.Domain/   pure game rules — no ASP.NET dependency
│   ├── src/TicTacToe.Api/      controllers, DTOs, in-memory store, error mapping
│   └── tests/TicTacToe.Tests/  xUnit: domain, service and HTTP integration tests
└── frontend/                   Angular 21 app
    ├── Dockerfile              npm ci → test → build → nginx runtime
    ├── nginx.conf              serves the SPA and proxies /api → api:8080
    └── src/app/
        ├── core/               models, GameApi (HTTP), GameStore (signals)
        └── components/         Board, StatusBar, MoveHistory, ScoreboardPanel
```

**Request flow:**

```
Browser ──/api/...──▶ nginx (web:80) ──proxy──▶ ASP.NET Core (api:8080)
                                                     │
                       GamesController ─▶ GameService ─▶ Game (domain rules)
                                              │
                                              └─▶ Scoreboard (singleton)
```

The browser only ever calls its own origin, so no CORS configuration is needed when running in Docker.

The backend architecture is explained in [backend/README.md](backend/README.md#architecture): its layers, the life of a request, how state is shared safely between requests, and how errors become HTTP responses.

## 2. Tech stack

| Layer | Technology |
|---|---|
| Frontend | Angular 21 (LTS), TypeScript 5.9, standalone components, signals, SCSS |
| Frontend tests | Vitest + jsdom (Angular 21 default), `HttpTestingController` |
| Backend | .NET 10 (LTS), ASP.NET Core Web API with controllers |
| API docs | Built-in OpenAPI (`Microsoft.AspNetCore.OpenApi`) + Swagger UI |
| Backend tests | xUnit, `WebApplicationFactory` for HTTP integration tests |
| Storage | In-memory (`ConcurrentDictionary`) |
| Runtime | Docker multi-stage images: `aspnet:10.0` and `nginx:stable-alpine` |

## 3. Features implemented

**Game board**
- A 3×3 grid of clickable cells.
- A cell locks once it's taken.
- The whole board locks when the game ends.

**Turns**
- X starts and players alternate.
- The current player is always shown.
- An invalid move never changes whose turn it is.

**Win and draw detection**
- Detects wins on rows, columns and both diagonals.
- On a win: the winner is shown, the winning cells are highlighted, further moves are blocked, and the scoreboard updates once.
- On a draw: a draw message is shown, further moves are blocked, and the scoreboard updates once.

**Reset Game**
- Clears the board, the history and the result.
- X starts again.
- The scoreboard is not changed.

**Move history**
- A table of move number, player and position, e.g. "Row 1, Column 1".
- Updates after every move.

**Undo Last Move**
- In two-player mode it removes one move.
- In computer mode it removes the computer's reply and your move before it.
- It is disabled when there is nothing to undo or the game has ended (see section 10).

**Scoreboard**
- Counts X wins, O wins and draws.
- Served by the backend.
- Has its own Reset Scoreboard button.

**Computer mode**
- The human plays X and the computer plays O. The computer moves automatically in the same request as your move.
- It follows this priority: win, block, center, corner, any free cell.

**Validation**
- The backend rejects these moves with a clear error:
  - outside the board
  - on a taken cell
  - after the game is over
  - by the wrong player
- The UI shows the error message in a dismissible banner.

## 4. Running the backend locally

**With Docker (recommended):**

```bash
docker compose up --build api        # http://localhost:5000/swagger
```

**Without Docker** (needs the [.NET 10 SDK](https://dotnet.microsoft.com/download)):

```bash
cd backend
dotnet run --project src/TicTacToe.Api    # http://localhost:5000 (launchSettings "http" profile)
```

## 5. Running the frontend locally

**With Docker (recommended):** `docker compose up --build` starts the frontend and the backend together at http://localhost:4200.

**Without Docker** (needs Node 24 LTS with npm 11). Start the backend first (section 4), then:

```bash
cd frontend
npm ci
npm start                            # http://localhost:4200
```

`ng serve` uses [proxy.conf.json](frontend/proxy.conf.json) to forward `/api` to `http://localhost:5000`, which mirrors the nginx proxy used in Docker.

## 6. API endpoint summary

The base path is `/api`. Enums are sent as strings, and `row`/`col` start at **0**.

| Method | Endpoint | Purpose | Success | Errors |
|---|---|---|---|---|
| POST | `/api/games` | Create a game. Body: `{ "mode": "TwoPlayer" \| "VsComputer" }` | 201 + `Location` | 400 missing or invalid mode |
| GET | `/api/games/{id}` | Get the current game state | 200 | 404 |
| POST | `/api/games/{id}/moves` | Submit a move. Body: `{ "player": "X", "row": 0, "col": 2 }` | 200 | 400 · 404 · 409 |
| POST | `/api/games/{id}/undo` | Undo the last move (two-player) or last move pair (computer) | 200 | 404 · 409 |
| POST | `/api/games/{id}/reset` | Start the game over with the same id and mode; scoreboard unchanged | 200 | 404 |
| GET | `/api/scoreboard` | Get the scoreboard | 200 | – |
| POST | `/api/scoreboard/reset` | Reset all counts to zero | 200 | – |

**Game state response** (returned by every `/api/games` endpoint):

```json
{
  "id": "7539b36f-14d8-456d-80e9-20646a3afffd",
  "mode": "VsComputer",
  "status": "InProgress",
  "currentPlayer": "X",
  "winner": null,
  "board": ["X", null, null, null, "O", null, null, null, null],
  "winningCells": [],
  "moves": [
    { "number": 1, "player": "X", "row": 0, "col": 0 },
    { "number": 2, "player": "O", "row": 1, "col": 1 }
  ],
  "canUndo": true,
  "scoreboard": { "xWins": 0, "oWins": 0, "draws": 0 }
}
```

- `board` is 9 cells, row by row: `index = row * 3 + col`.
- `status` is one of `InProgress`, `Won`, `Draw`.
- The scoreboard is embedded so one response is enough to render the whole screen.

**Errors** use [RFC 7807 ProblemDetails](https://www.rfc-editor.org/rfc/rfc7807) with a machine-readable `code`:

```json
{ "status": 409, "title": "Action not allowed", "detail": "Cell (0, 0) is already taken by X.", "code": "CellOccupied" }
```

| HTTP | `code` | Meaning |
|---|---|---|
| 400 | `OutOfBounds` (or validation errors) | The request itself is malformed |
| 404 | `GameNotFound` | Unknown game id |
| 409 | `CellOccupied`, `WrongPlayer`, `GameAlreadyCompleted`, `NothingToUndo` | The request is valid but not allowed in the current state |

Runnable examples for every endpoint, including the invalid cases, are in [TicTacToe.Api.http](backend/src/TicTacToe.Api/TicTacToe.Api.http). You can run them with the VS Code REST Client extension, Rider, or Visual Studio.

## 7. Running tests

```bash
docker compose --profile test run --rm --build api-tests   # backend: 56 tests
docker compose --profile test run --rm --build web-tests   # frontend: 32 tests
```

Without Docker: `cd backend && dotnet test`, and `cd frontend && npm run test:ci`.

**Backend tests (xUnit)**

| File | Covers |
|---|---|
| `GameTests` | Valid move, each invalid move (out of board, taken cell, wrong player, after completion), turn switching, row/column/diagonal wins with winning cells, draw, reset, undo in two-player mode, undo in computer mode, undo disabled, computer stops after a human win |
| `ComputerPlayerTests` | One test per priority rule (win > block > center > corner > any) |
| `ScoreboardTests` | Counting, rejecting unfinished games, reset |
| `GameServiceTests` | Scoreboard updates **exactly once** per completed game, reset keeps the score, O win recorded |
| `ApiTests` | Real HTTP: 201/200/400/404/409 status codes, string enums, ProblemDetails `code` |

**Frontend tests (Vitest)**

| File | Covers |
|---|---|
| `game-api.spec` | Each method calls the documented HTTP method, URL and body |
| `game-store.spec` | State replaced from the server, index → row/col, double-click guard, error messages |
| `board`, `status-bar`, `move-history`, `scoreboard-panel` specs | Rendering, locked cells, winning-cell highlight, messages, 1-based positions, emitted events |
| `app.spec` | App + store + API against a fake backend: click → request → render; Undo disabled state; win locks the board; error alert |

## 8. AI tools and prompt summary

> **Note to the reviewer:** this section is written from my perspective as the candidate. The factual record below (what was asked, generated and changed) is accurate. The reflections marked ✍️ are mine to complete in my own words.

**Tool used:** Claude Code (Anthropic) in VS Code, in a step-by-step workflow. I asked for one step at a time, then reviewed, ran and committed each step before continuing. The git history follows those steps one commit per step.

**How I converted the requirement into a specification**
1. I gave the AI `requirement.txt` as the single source of truth and asked for a plan, not code.
2. The AI asked me to decide the open points before any code was written:
   - the .NET version
   - Option A or B for undo after completion
   - controllers vs. minimal APIs
   - the computer-mode undo edge case
3. I added constraints of my own: run everything with Docker and Docker Compose, use LTS versions, and keep the plan in the repo as [plan.md](plan.md).
4. That plan has 10 steps (0–9). Each step lists the reasoning behind it and a check to run.

**Prompts used (summarised).** The full list, word for word and in order, mapped to plan steps and commits, is in [PROMPTS.md](PROMPTS.md).

| # | Prompt (paraphrased) | Outcome |
|---|---|---|
| 1 | "Read requirement.txt as the source of truth. I want to build this step by step and understand the engineering decisions." | Requirements analysis, clarifying questions, implementation plan |
| 2 | "Use Docker and Docker Compose for execution." | Plan reworked: multi-stage Dockerfiles, nginx reverse proxy, test profiles |
| 3 | "What Angular version is used?" → "I will go with LTS." | Angular 21 LTS pinned instead of the newest major |
| 4 | "Write the plan.md in the folder." | [plan.md](plan.md) |
| 5 | "Step 0 … Step 9", one at a time | Code for each step, followed by an explanation and verification commands |
| 6 | "The boxes are uneven in the board" (with a screenshot) | CSS grid fix (see below) |

**What the AI generated:** the solution structure and Dockerfiles, the domain model and rules, the API layer, both test suites, the Angular data layer and components, and a first draft of this README.

**What I changed, caught or decided manually**
- **Version choices:** I chose LTS releases over the newest Angular major (Angular 21 instead of 22).
- **Undo policy:** I chose Option A, and "remove until it is X's turn" for computer-mode undo.
- **Board layout bug:** I found it during manual testing. The board rows had different heights, and the X/O looked faded. Root cause: the grid had no row sizes, and the global `button:disabled` opacity applied to taken cells.
- ✍️ *Add any code you rewrote yourself, or review comments you pushed back on.*

**Problems found during development, and how they were resolved**

| Problem | Caught by | Resolution |
|---|---|---|
| `[property: Required]` on record DTOs made MVC throw 500 at runtime | HTTP integration tests (unit tests could not see it) | Put validation attributes on the record's constructor parameters |
| `npm install` crashed (`edgesOut` null) under the npm 10 that ships with Node 22 | Docker scaffold step | Moved to Node 24 LTS (npm 11) rather than using `--legacy-peer-deps`, which disables peer-dependency checks |
| `cond ? 4 : null` didn't compile inside a `??` chain | Compiler | Used the same `FirstEmpty` helper for the center cell |
| Uneven board rows / faded marks | Manual browser test | CSS grid row sizes + override `opacity` on disabled cells |

**Parts I reviewed most carefully** ✍️ *(confirm or edit)*
- `Game.Undo` and the move-count rule for computer mode.
- The "score is recorded exactly once" reasoning in `GameService.MakeMove`.
- The computer priority order in `ComputerPlayer`, compared against the requirement list.
- The HTTP status mapping (400 vs 409) in `GameExceptionHandler`.

## 9. Design decisions

| Decision | Why |
|---|---|
| **Separate `Domain` project** with no ASP.NET dependency | Game rules are plain C#. They're easy to unit test and to read on screen. The API layer only adds HTTP, locking and persistence. |
| **The move list is the single source of truth** | The board, current player and status are all derived from `Moves`. Undo removes moves and replays the rest, so history and board can't drift apart. |
| **Whose turn it is comes from the move count** (X always starts) | Undo restores the correct turn with no extra bookkeeping. |
| **The computer replies inside `Game.MakeMove`** | It can't move after the game ends, because the "is the game over?" check sits right before the call. The behavior is unit-testable, and one HTTP request returns both moves. |
| **Score recorded once** | `MakeMove` throws on a finished game, so a game that is finished right after a successful move just changed from `InProgress` to finished. That happens exactly once per game. |
| **One lock per game** (`lock (game)`) | A double-click or two tabs can't interleave moves on the same game, and different games never block each other. |
| **Domain exceptions + one `IExceptionHandler`** | The controllers have no try/catch. The domain throws a `GameRuleException` with a code, and a single place maps it to 400/404/409 ProblemDetails. |
| **400 vs 409** | 400 means the request is malformed (outside the board, missing field). 409 means the request is valid but not allowed in the current state (taken cell, wrong turn, game over, nothing to undo). |
| **Separate request/response classes (DTOs) instead of returning domain types** | The API contract and the game rules can change independently. |
| **Full state in every response, scoreboard embedded** | The UI re-renders from one response, with no follow-up requests. |
| **The frontend contains no game rules** | `GameStore` replaces its state with each server response. It only converts board index ↔ row/col and disables controls while a request is in flight. |
| **Signals for state, RxJS for HTTP** | Signals are Angular 21's default for state: no subscriptions to clean up. HTTP calls stay as one-off Observables. |
| **Presentational components + one container (`App`)** | Board, StatusBar, MoveHistory and ScoreboardPanel only take inputs and emit outputs, which makes them simple to test. All wiring is visible in `app.html`. |
| **nginx reverse proxy for `/api`** | Same origin, so no CORS and no API URL baked into the bundle. This mirrors a typical production setup. |
| **Multi-stage Docker images** | SDK and Node are only in the build stages, and the runtime images are small (`aspnet`, `nginx`). The `test` stages run the same build that ships. |
| **Deterministic computer player** | No randomness, so every priority rule has an exact unit test. |

## 10. Clarifications and assumptions

- **Scoreboard and Undo: Option A was chosen.** Once a game is won or drawn, Undo is disabled (the server reports `canUndo: false` and returns 409). The scoreboard result for that game is final.
- **Computer-mode undo:** moves are removed until it is X's turn again. Normally that is the computer's reply plus the human move before it, as in the requirement's example. If the human's last move ended the game, Undo is disabled anyway (Option A).
- **Move request includes the game ID** through the route (`/api/games/{id}/moves`), not the body. The body carries `player`, `row` and `col`.
- **Positions start at 0 in the API** and are shown starting at 1 in the UI ("Row 1, Column 1"), matching the requirement's example.
- **Reset Game** starts over in the same session: same game id and mode, empty board, X to move. The scoreboard is unchanged.
- **Changing the mode** in the UI creates a new game in that mode, since a game's mode is fixed at creation.
- **"Session-level scoreboard"** is read as one scoreboard per running API process, shared by all games and browsers. There are no user accounts in scope.
- **Human is always X and moves first** in computer mode, as the requirement specifies.
- **The computer replies in the same request as the human's move**, so there's no separate "computer move" endpoint.

## 11. Known limitations

- **In-memory state.** Games and the scoreboard are lost when the API restarts, and finished games are never removed from memory. Acceptable for an exercise; not for production.
- **Single instance only.** Because state is in process memory, running multiple API replicas behind a load balancer would split games across replicas.
- **One global scoreboard.** All players share one scoreboard, with no authentication or per-user tracking.
- **Page reload starts a new game.** The frontend doesn't store the current game id.
- **No end-to-end browser tests.** jsdom can't check layout, which is why the uneven-board bug was found manually.
- **The computer is not perfect.** It follows the required priority rules, so a fork beats it (this is covered by a test).

## 12. Future improvements

- SQLite (EF Core) with a Docker volume, so games and the scoreboard survive restarts. The `IGameStore` interface is the extension point.
- Remove abandoned games from memory after a period of inactivity.
- Store the game id in `sessionStorage` and reload it with `GET /api/games/{id}` after a page refresh.
- Playwright end-to-end tests run against `docker compose up`.
- A "hard" computer mode using minimax.
- Per-player or per-session scoreboards, plus authentication.
- A CI pipeline (GitHub Actions) running both test profiles and building the images on every push.
- Health-check-based `depends_on` in compose, and structured logging.
