import { ComponentFixture, TestBed } from '@angular/core/testing';
import { board } from '../../testing/fixtures';
import { Board } from './board';

describe('Board', () => {
  let fixture: ComponentFixture<Board>;

  function render(cells: string, inputs: { winningCells?: number[]; disabled?: boolean } = {}) {
    fixture.componentRef.setInput('board', board(cells));
    fixture.componentRef.setInput('winningCells', inputs.winningCells ?? []);
    fixture.componentRef.setInput('disabled', inputs.disabled ?? false);
    fixture.detectChanges();
    return [...(fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('button.cell')];
  }

  beforeEach(() => {
    fixture = TestBed.createComponent(Board);
  });

  it('renders nine cells showing X and O marks', () => {
    const cells = render('X...O....');

    expect(cells).toHaveLength(9);
    expect(cells[0].textContent?.trim()).toBe('X');
    expect(cells[4].textContent?.trim()).toBe('O');
    expect(cells[1].textContent?.trim()).toBe('');
  });

  it('locks occupied cells and keeps empty cells clickable', () => {
    const cells = render('X...O....');

    expect(cells[0].disabled).toBe(true);
    expect(cells[4].disabled).toBe(true);
    expect(cells[1].disabled).toBe(false);
  });

  it('locks every cell when disabled (game completed or request in flight)', () => {
    const cells = render('X........', { disabled: true });

    expect(cells.every((c) => c.disabled)).toBe(true);
  });

  it('highlights only the winning cells', () => {
    const cells = render('XXXOO....', { winningCells: [0, 1, 2] });

    const highlighted = cells.flatMap((c, i) => (c.classList.contains('win') ? [i] : []));
    expect(highlighted).toEqual([0, 1, 2]);
  });

  it('emits the cell index when an empty cell is clicked', () => {
    const clicked = vi.fn();
    fixture.componentInstance.cellClick.subscribe(clicked);
    const cells = render('.........');

    cells[5].click();

    expect(clicked).toHaveBeenCalledWith(5);
  });

  it('labels cells with 1-based row and column for screen readers', () => {
    const cells = render('.....X...');

    expect(cells[5].getAttribute('aria-label')).toBe('Row 2, Column 3, X');
  });
});
