import { Component, computed, input, output } from '@angular/core';
import { GameMode, GameState, Player } from '../../core/models';

@Component({
  selector: 'app-status-bar',
  templateUrl: './status-bar.html',
  styleUrl: './status-bar.scss',
})
export class StatusBar {
  readonly state = input<GameState | null>(null);
  readonly disabled = input(false);

  readonly modeChange = output<GameMode>();

  protected readonly modes: { value: GameMode; label: string }[] = [
    { value: 'TwoPlayer', label: 'Two Player' },
    { value: 'VsComputer', label: 'Vs Computer' },
  ];

  protected readonly message = computed(() => {
    const state = this.state();
    if (!state) return 'Starting game…';

    const name = (player: Player) =>
      state.mode === 'VsComputer' ? `Player ${player} (${player === 'X' ? 'you' : 'computer'})` : `Player ${player}`;

    switch (state.status) {
      case 'Won':
        return `${name(state.winner!)} wins!`;
      case 'Draw':
        return "It's a draw!";
      default:
        return `${name(state.currentPlayer)}'s turn`;
    }
  });

  protected select(mode: GameMode): void {
    if (mode !== this.state()?.mode) {
      this.modeChange.emit(mode);
    }
  }
}
