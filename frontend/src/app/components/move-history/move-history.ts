import { Component, input } from '@angular/core';
import { Move } from '../../core/models';

@Component({
  selector: 'app-move-history',
  templateUrl: './move-history.html',
  styleUrl: './move-history.scss',
})
export class MoveHistory {
  readonly moves = input<Move[]>([]);
}
