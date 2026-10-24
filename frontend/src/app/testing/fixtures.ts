import { GameState, Player } from '../core/models';

/** A fresh two-player game; override only what a test cares about. */
export function gameState(overrides: Partial<GameState> = {}): GameState {
  return {
    id: 'g1',
    mode: 'TwoPlayer',
    status: 'InProgress',
    currentPlayer: 'X',
    winner: null,
    board: Array(9).fill(null),
    winningCells: [],
    moves: [],
    canUndo: false,
    scoreboard: { xWins: 0, oWins: 0, draws: 0 },
    ...overrides,
  };
}

/** "XO.X....." -> board, row by row; '.' is empty. */
export function board(cells: string): (Player | null)[] {
  return [...cells].map((c) => (c === 'X' || c === 'O' ? c : null));
}
