import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GameState } from '../../core/models';
import { gameState } from '../../testing/fixtures';
import { StatusBar } from './status-bar';

describe('StatusBar', () => {
  let fixture: ComponentFixture<StatusBar>;

  function render(state: GameState | null) {
    fixture.componentRef.setInput('state', state);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    return {
      message: el.querySelector('.message')?.textContent?.trim(),
      modeButtons: [...el.querySelectorAll<HTMLButtonElement>('.modes button')],
    };
  }

  beforeEach(() => {
    fixture = TestBed.createComponent(StatusBar);
  });

  it("shows whose turn it is", () => {
    expect(render(gameState({ currentPlayer: 'O' })).message).toBe("Player O's turn");
  });

  it('shows the winner', () => {
    expect(render(gameState({ status: 'Won', winner: 'X' })).message).toBe('Player X wins!');
  });

  it('names the human and computer in computer mode', () => {
    expect(render(gameState({ mode: 'VsComputer', status: 'Won', winner: 'O' })).message)
      .toBe('Player O (computer) wins!');
  });

  it('shows a draw message', () => {
    expect(render(gameState({ status: 'Draw' })).message).toBe("It's a draw!");
  });

  it('marks the selected game mode', () => {
    const { modeButtons } = render(gameState({ mode: 'VsComputer' }));

    expect(modeButtons.map((b) => b.getAttribute('aria-checked'))).toEqual(['false', 'true']);
  });

  it('emits a mode change only when a different mode is picked', () => {
    const changed = vi.fn();
    fixture.componentInstance.modeChange.subscribe(changed);
    const { modeButtons } = render(gameState({ mode: 'TwoPlayer' }));

    modeButtons[0].click(); // already selected
    modeButtons[1].click();

    expect(changed).toHaveBeenCalledTimes(1);
    expect(changed).toHaveBeenCalledWith('VsComputer');
  });
});
