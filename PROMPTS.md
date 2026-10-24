# AI Prompt Log

These are the prompts I used to build this project with **Claude Code** (Anthropic), running in the VS Code extension. They are listed in order and quoted exactly as I typed them, typos included.

I worked one step at a time: I asked for a single step, reviewed the code and explanation, ran the verification commands myself, and committed before asking for the next step. The steps refer to [plan.md](plan.md).

For a summary of what was generated and what I changed, see [README section 8](README.md#8-ai-tools-and-prompt-summary).

---

## Phase 1: Requirement analysis and planning

### Prompt 1: Initial brief

> I need to build a browser-based Tic Tac Toe application for a technical assessment. The application must be served through a backend API.
>
> First, read the complete requirement.txt file in my project workspace. Treat that file as the source of truth for the assignment.
>
> My goal is to implement the application step by step, carefully reviewing and executing each step myself. I want to understand the engineering decisions rather than blindly accept generated code.

**Result:** the AI read `requirement.txt` and checked which tools were installed. Node and git were present; the .NET SDK and Angular CLI were not. Before writing any code, it asked me to decide the open design questions below.

### Decisions I made (answers to the AI's clarifying questions)

| Question | My choice |
|---|---|
| .NET version for the backend | .NET 9 / 10, which became **.NET 10 LTS** |
| Clarification 2: Undo after a game is completed | **Option A**: Undo is disabled after a win or draw |
| API style | **Controllers** (not minimal APIs) |
| Computer-mode undo edge case | **Remove moves until it is X's turn** again |

### Prompt 2: Change of execution approach

I rejected the first plan and asked for this instead:

> use docker and docker compose for execution.

**Result:** the plan was reworked around Docker:
- Multi-stage Dockerfiles for the API and the web app.
- An nginx reverse proxy that forwards `/api` to the API, so the browser never makes cross-origin calls.
- Docker Compose with opt-in `test` profiles.
- No local SDK or CLI needed.

### Prompt 3

> write the plan.md in the folder

**Result:** asked for the plan to be saved in the repo. It was written once the plan was approved (see Prompt 5).

### Prompt 4

> what is the angular version used

**Result:** the AI checked the npm registry. The latest Angular was 22.2.4, and the long-term-support (LTS) versions were 21.2.28 and 20.3.41.

### Prompt 5

> I will go with lts.

**Result:** the frontend was pinned to **Angular 21 LTS** (CLI 21.2.28). After I approved the plan, [plan.md](plan.md) was written to the repo.

---

## Phase 2: Implementation, one step per prompt

| # | Prompt (verbatim) | Plan step | What was produced | Commit |
|---|---|---|---|---|
| 6 | `provide gitignore` | Step 0 | Root `.gitignore` for .NET, Node/Angular, editors and `.env` | `d8376d8` |
| 7 | `Done with step0` | Step 1 | .NET 10 solution (Domain, Api, Tests), `Program.cs`, backend Dockerfile, compose `api` service | `b8bce5e` |
| 8 | `start with step2` | Step 2 | Domain model: `Game`, `WinDetector`, `ComputerPlayer`, `Scoreboard`, `GameRuleException` | `fc0ce6a` |
| 9 | `start step 3` | Step 3 | 39 xUnit domain tests covering every required test case | `8945181` |
| 10 | `step 4` | Step 4 | REST API: `GameService`, in-memory store, controllers, DTOs, ProblemDetails error handler, service and HTTP integration tests (56 total) | `63c4e49` |
| 11 | `step5` | Step 5 | Angular 21 skeleton, frontend Dockerfile, `nginx.conf` reverse proxy, compose `web` service | `58d636d` |
| 12 | `step 6` | Step 6 | `models.ts`, `GameApi` (HTTP), `GameStore` (signals) and their specs | `1a214b8` |
| 13 | `step 7` | Step 7 | UI: `Board`, `StatusBar`, `MoveHistory`, `ScoreboardPanel`, `App` layout, theme tokens | `1a214b8` |

### Prompt 14: Bug report from manual testing

I attached a screenshot of the running app and wrote:

> the boxes are uneven in the board

**Result:** the AI diagnosed two CSS problems:
- The grid had no row sizes, so the row containing an "X" grew taller than the others.
- The global `button:disabled` opacity was making placed marks look faded.

It fixed both in `board.scss`. The fix was committed with Step 8 in `6db94e5`.

### Implementation continued

| # | Prompt (verbatim) | Plan step | What was produced | Commit |
|---|---|---|---|---|
| 15 | `step 8` | Step 8 | Component tests for the board, status bar, move history and scoreboard panel, plus App integration tests (32 frontend tests) | `6db94e5` |
| 16 | `step 9` | Step 9 | Root `README.md` with all 12 required sections; frontend README turned into a pointer | `ae47981` |

---

## Phase 3: Documentation

| # | Prompt (verbatim) | What was produced | Commit |
|---|---|---|---|
| 17 | `also add readme in .net application` | [backend/README.md](backend/README.md): projects, run, API, tests, configuration | `ae47981` |
| 18 | `explain architecture aswell.` | An Architecture section in the backend README: layer diagram, request sequence diagram, state and concurrency, error flow, composition root | `ae47981` |
| 19 | `add the md file with list promps, I have used to develop this` | This file | — |

---

## Notes on how I used the AI

- **Plan before code.** The first prompts produced only analysis, questions and a plan. No code was written until I had made the design decisions and approved the plan.
- **One step per prompt.** I kept each step small enough to review, run and commit on its own.
- **I verified each step myself.** Every step ended with commands I ran: Docker builds, test runs, or checks in the browser or Swagger.
- **I fed back what I saw.** Prompt 14 is a bug I found by playing the game in the browser. Automated tests in jsdom can't check layout, so they missed it.
