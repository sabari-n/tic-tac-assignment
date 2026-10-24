import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MoveHistory } from './move-history';

describe('MoveHistory', () => {
  let fixture: ComponentFixture<MoveHistory>;

  beforeEach(() => {
    fixture = TestBed.createComponent(MoveHistory);
  });

  it('shows an empty message before any move', () => {
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No moves yet.');
  });

  it('lists each move with number, player and 1-based position', () => {
    fixture.componentRef.setInput('moves', [
      { number: 1, player: 'X', row: 0, col: 0 },
      { number: 2, player: 'O', row: 1, col: 1 },
    ]);
    fixture.detectChanges();

    const rows = [...(fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr')].map((tr) =>
      [...tr.querySelectorAll('td')].map((td) => td.textContent?.trim()),
    );
    expect(rows).toEqual([
      ['1', 'X', 'Row 1, Column 1'],
      ['2', 'O', 'Row 2, Column 2'],
    ]);
  });
});
