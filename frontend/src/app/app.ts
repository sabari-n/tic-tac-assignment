import { Component, OnInit, computed, inject } from '@angular/core';
import { Board } from './components/board/board';
import { MoveHistory } from './components/move-history/move-history';
import { ScoreboardPanel } from './components/scoreboard-panel/scoreboard-panel';
import { StatusBar } from './components/status-bar/status-bar';
import { GameStore } from './core/game-store';
import { Player } from './core/models';

const EMPTY_BOARD: (Player | null)[] = Array(9).fill(null);

/** Container: wires the store's signals and actions to presentational components. */
@Component({
  selector: 'app-root',
  imports: [Board, MoveHistory, ScoreboardPanel, StatusBar],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  protected readonly store = inject(GameStore);

  protected readonly board = computed(() => this.store.state()?.board ?? EMPTY_BOARD);
  protected readonly boardLocked = computed(
    () => !this.store.state() || this.store.isCompleted() || this.store.loading(),
  );

  ngOnInit(): void {
    this.store.newGame('TwoPlayer');
  }
}
