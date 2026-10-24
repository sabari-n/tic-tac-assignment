import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ScoreboardPanel } from './scoreboard-panel';

describe('ScoreboardPanel', () => {
  let fixture: ComponentFixture<ScoreboardPanel>;
  const text = (testId: string) =>
    (fixture.nativeElement as HTMLElement).querySelector(`[data-testid="${testId}"]`)?.textContent?.trim();

  beforeEach(() => {
    fixture = TestBed.createComponent(ScoreboardPanel);
    fixture.componentRef.setInput('score', { xWins: 3, oWins: 1, draws: 2 });
    fixture.detectChanges();
  });

  it('shows X wins, O wins and draws', () => {
    expect([text('x-wins'), text('o-wins'), text('draws')]).toEqual(['3', '1', '2']);
  });

  it('emits resetScores when Reset Scoreboard is clicked', () => {
    const reset = vi.fn();
    fixture.componentInstance.resetScores.subscribe(reset);

    (fixture.nativeElement as HTMLElement).querySelector('button')!.click();

    expect(reset).toHaveBeenCalledTimes(1);
  });
});
