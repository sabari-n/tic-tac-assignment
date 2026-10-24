import { Component, input, output } from '@angular/core';
import { Scoreboard } from '../../core/models';

@Component({
  selector: 'app-scoreboard-panel',
  templateUrl: './scoreboard-panel.html',
  styleUrl: './scoreboard-panel.scss',
})
export class ScoreboardPanel {
  readonly score = input.required<Scoreboard>();
  readonly disabled = input(false);

  readonly resetScores = output<void>();
}
