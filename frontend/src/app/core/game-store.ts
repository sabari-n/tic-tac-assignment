import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, finalize } from 'rxjs';
import { GameApi } from './game-api';
import { GameMode, GameState, ProblemDetails, Scoreboard } from './models';

/**
 * UI state for the single game on screen. The backend is the source of truth:
 * every action replaces `state` with the server's response. No game rules are
 * computed here — only what the view needs to render and disable controls.
 */
@Injectable({ providedIn: 'root' })
export class GameStore {
  private readonly api = inject(GameApi);

  private readonly _state = signal<GameState | null>(null);
  private readonly _scoreboard = signal<Scoreboard>({ xWins: 0, oWins: 0, draws: 0 });
  private readonly _error = signal<string | null>(null);
  private readonly _loading = signal(false);

  readonly state = this._state.asReadonly();
  readonly scoreboard = this._scoreboard.asReadonly();
  readonly error = this._error.asReadonly();
  readonly loading = this._loading.asReadonly();

  readonly mode = computed<GameMode>(() => this._state()?.mode ?? 'TwoPlayer');
  readonly isCompleted = computed(() => {
    const status = this._state()?.status;
    return status === 'Won' || status === 'Draw';
  });
  readonly canUndo = computed(() => !!this._state()?.canUndo && !this._loading());

  newGame(mode: GameMode): void {
    this.run(this.api.createGame(mode), (state) => this.setState(state));
  }

  /** Plays the cell at board index 0..8 for whoever's turn it is. */
  play(index: number): void {
    const state = this._state();
    if (!state) return;
    const row = Math.floor(index / 3);
    const col = index % 3;
    this.run(this.api.move(state.id, state.currentPlayer, row, col), (s) => this.setState(s));
  }

  undo(): void {
    const state = this._state();
    if (!state) return;
    this.run(this.api.undo(state.id), (s) => this.setState(s));
  }

  reset(): void {
    const state = this._state();
    if (!state) return;
    this.run(this.api.reset(state.id), (s) => this.setState(s));
  }

  resetScoreboard(): void {
    this.run(this.api.resetScoreboard(), (score) => this._scoreboard.set(score));
  }

  dismissError(): void {
    this._error.set(null);
  }

  private setState(state: GameState): void {
    this._state.set(state);
    this._scoreboard.set(state.scoreboard);
  }

  /**
   * Runs one request at a time. Ignoring clicks while a request is in flight
   * stops a fast double-click from sending a second move for the same turn.
   */
  private run<T>(request: Observable<T>, onSuccess: (value: T) => void): void {
    if (this._loading()) return;
    this._loading.set(true);
    this._error.set(null);
    request.pipe(finalize(() => this._loading.set(false))).subscribe({
      next: onSuccess,
      error: (err: unknown) => this._error.set(toMessage(err)),
    });
  }
}

function toMessage(err: unknown): string {
  if (err instanceof HttpErrorResponse) {
    if (err.status === 0) return 'Cannot reach the server. Is the API running?';
    const problem = err.error as ProblemDetails | null;
    return problem?.detail ?? problem?.title ?? `Request failed (${err.status}).`;
  }
  return 'Unexpected error.';
}
