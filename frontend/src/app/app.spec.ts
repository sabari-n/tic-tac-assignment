import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { App } from './app';
import { GameState } from './core/models';
import { board, gameState } from './testing/fixtures';

/**
 * Integration of App + GameStore + GameApi against a fake HTTP backend:
 * checks that the UI renders exactly what the server returns.
 */
describe('App', () => {
  let fixture: ComponentFixture<App>;
  let http: HttpTestingController;

  const el = () => fixture.nativeElement as HTMLElement;
  const button = (label: string) =>
    [...el().querySelectorAll<HTMLButtonElement>('button')].find((b) => b.textContent?.trim() === label)!;

  async function start(state: GameState = gameState()) {
    fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    http.expectOne({ method: 'POST', url: '/api/games' }).flush(state);
    await fixture.whenStable();
    fixture.detectChanges();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('creates a two-player game on start and renders the title', async () => {
    fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const req = http.expectOne({ method: 'POST', url: '/api/games' });
    expect(req.request.body).toEqual({ mode: 'TwoPlayer' });
    req.flush(gameState());

    expect(el().querySelector('h1')?.textContent).toContain('Tic Tac Toe');
  });

  it('disables Undo when the server says there is nothing to undo', async () => {
    await start(gameState({ canUndo: false }));

    expect(button('Undo Last Move').disabled).toBe(true);
  });

  it('enables Undo and calls the undo endpoint', async () => {
    await start(gameState({ canUndo: true, board: board('X........') }));

    button('Undo Last Move').click();

    http.expectOne({ method: 'POST', url: '/api/games/g1/undo' }).flush(gameState());
  });

  it('sends a move when a cell is clicked and renders the response', async () => {
    await start();

    el().querySelectorAll<HTMLButtonElement>('button.cell')[4].click();
    http.expectOne('/api/games/g1/moves').flush(
      gameState({ board: board('....X....'), currentPlayer: 'O', canUndo: true }),
    );
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el().querySelectorAll('button.cell')[4].textContent?.trim()).toBe('X');
    expect(el().querySelector('.message')?.textContent).toContain("Player O's turn");
  });

  it('locks the board after a win and highlights the winning cells', async () => {
    await start(gameState({ status: 'Won', winner: 'X', board: board('XXXOO....'), winningCells: [0, 1, 2] }));

    const cells = [...el().querySelectorAll<HTMLButtonElement>('button.cell')];
    expect(cells.every((c) => c.disabled)).toBe(true);
    expect(cells.filter((c) => c.classList.contains('win'))).toHaveLength(3);
    expect(button('Undo Last Move').disabled).toBe(true);
  });

  it('shows the API error message in an alert', async () => {
    await start();

    el().querySelectorAll<HTMLButtonElement>('button.cell')[0].click();
    http.expectOne('/api/games/g1/moves').flush(
      { detail: 'Cell (0, 0) is already taken by X.' },
      { status: 409, statusText: 'Conflict' },
    );
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el().querySelector('[role="alert"]')?.textContent).toContain('Cell (0, 0) is already taken by X.');
  });
});
