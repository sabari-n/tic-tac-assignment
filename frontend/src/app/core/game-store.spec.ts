import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { GameStore } from './game-store';
import { GameState } from './models';

function gameState(overrides: Partial<GameState> = {}): GameState {
  return {
    id: 'g1',
    mode: 'TwoPlayer',
    status: 'InProgress',
    currentPlayer: 'X',
    winner: null,
    board: Array(9).fill(null),
    winningCells: [],
    moves: [],
    canUndo: false,
    scoreboard: { xWins: 0, oWins: 0, draws: 0 },
    ...overrides,
  };
}

describe('GameStore', () => {
  let store: GameStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    store = TestBed.inject(GameStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  function startGame(state = gameState()): void {
    store.newGame(state.mode);
    http.expectOne('/api/games').flush(state);
  }

  it('newGame stores the server state and scoreboard', () => {
    startGame(gameState({ scoreboard: { xWins: 2, oWins: 1, draws: 0 } }));

    expect(store.state()?.id).toBe('g1');
    expect(store.scoreboard()).toEqual({ xWins: 2, oWins: 1, draws: 0 });
    expect(store.loading()).toBe(false);
  });

  it('play converts the board index to row/col and sends the current player', () => {
    startGame(gameState({ currentPlayer: 'O' }));

    store.play(7); // row 2, col 1

    const req = http.expectOne('/api/games/g1/moves');
    expect(req.request.body).toEqual({ player: 'O', row: 2, col: 1 });
    req.flush(gameState());
  });

  it('ignores a second action while a request is in flight', () => {
    startGame();

    store.play(0);
    store.play(1);

    expect(store.loading()).toBe(true);
    http.expectOne('/api/games/g1/moves').flush(gameState());
    expect(store.loading()).toBe(false);
  });

  it('shows the ProblemDetails detail when the API rejects an action', () => {
    startGame();

    store.play(0);
    http.expectOne('/api/games/g1/moves').flush(
      { title: 'Action not allowed', detail: 'Cell (0, 0) is already taken by X.', code: 'CellOccupied' },
      { status: 409, statusText: 'Conflict' },
    );

    expect(store.error()).toBe('Cell (0, 0) is already taken by X.');
    expect(store.state()?.id).toBe('g1'); // previous state kept
    expect(store.loading()).toBe(false);
  });

  it('canUndo follows the server flag', () => {
    startGame(gameState({ canUndo: true }));

    expect(store.canUndo()).toBe(true);
  });

  it('resetScoreboard updates only the scoreboard', () => {
    startGame(gameState({ scoreboard: { xWins: 3, oWins: 0, draws: 1 } }));

    store.resetScoreboard();
    http.expectOne('/api/scoreboard/reset').flush({ xWins: 0, oWins: 0, draws: 0 });

    expect(store.scoreboard()).toEqual({ xWins: 0, oWins: 0, draws: 0 });
    expect(store.state()?.id).toBe('g1');
  });
});
