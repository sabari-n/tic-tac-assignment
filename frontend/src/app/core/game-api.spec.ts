import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { GameApi } from './game-api';

describe('GameApi', () => {
  let api: GameApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(GameApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('createGame POSTs the mode to /api/games', () => {
    api.createGame('VsComputer').subscribe();

    const req = http.expectOne({ method: 'POST', url: '/api/games' });
    expect(req.request.body).toEqual({ mode: 'VsComputer' });
  });

  it('getGame GETs /api/games/{id}', () => {
    api.getGame('abc').subscribe();

    http.expectOne({ method: 'GET', url: '/api/games/abc' });
  });

  it('move POSTs player, row and col to /api/games/{id}/moves', () => {
    api.move('abc', 'O', 2, 1).subscribe();

    const req = http.expectOne({ method: 'POST', url: '/api/games/abc/moves' });
    expect(req.request.body).toEqual({ player: 'O', row: 2, col: 1 });
  });

  it('undo, reset and scoreboard endpoints use the documented routes', () => {
    api.undo('abc').subscribe();
    api.reset('abc').subscribe();
    api.getScoreboard().subscribe();
    api.resetScoreboard().subscribe();

    http.expectOne({ method: 'POST', url: '/api/games/abc/undo' });
    http.expectOne({ method: 'POST', url: '/api/games/abc/reset' });
    http.expectOne({ method: 'GET', url: '/api/scoreboard' });
    http.expectOne({ method: 'POST', url: '/api/scoreboard/reset' });
  });
});
