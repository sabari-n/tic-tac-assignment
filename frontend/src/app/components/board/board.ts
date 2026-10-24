import { Component, computed, input, output } from '@angular/core';
import { Player } from '../../core/models';

@Component({
  selector: 'app-board',
  templateUrl: './board.html',
  styleUrl: './board.scss',
})
export class Board {
  /** 9 cells, row by row. */
  readonly board = input.required<(Player | null)[]>();
  readonly winningCells = input<number[]>([]);
  /** Locks every cell (game completed, request in flight, no game yet). */
  readonly disabled = input(false);

  readonly cellClick = output<number>();

  private readonly winning = computed(() => new Set(this.winningCells()));

  protected isWinning(index: number): boolean {
    return this.winning().has(index);
  }

  protected label(index: number, cell: Player | null): string {
    const position = `Row ${Math.floor(index / 3) + 1}, Column ${(index % 3) + 1}`;
    return `${position}, ${cell ?? 'empty'}`;
  }
}
