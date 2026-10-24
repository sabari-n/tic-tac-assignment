// Mirrors the backend contract (TicTacToe.Api/Contracts). Enums travel as strings.

export type Player = 'X' | 'O';
export type GameMode = 'TwoPlayer' | 'VsComputer';
export type GameStatus = 'InProgress' | 'Won' | 'Draw';

/** Row and col are 0-based. */
export interface Move {
  number: number;
  player: Player;
  row: number;
  col: number;
}

export interface Scoreboard {
  xWins: number;
  oWins: number;
  draws: number;
}

export interface GameState {
  id: string;
  mode: GameMode;
  status: GameStatus;
  currentPlayer: Player;
  winner: Player | null;
  /** 9 cells, row by row: index = row * 3 + col. */
  board: (Player | null)[];
  winningCells: number[];
  moves: Move[];
  canUndo: boolean;
  scoreboard: Scoreboard;
}

/** RFC 7807 error body returned by the API for 4xx responses. */
export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  /** Domain error code, e.g. "CellOccupied". */
  code?: string;
  /** Model-validation errors (missing fields etc.). */
  errors?: Record<string, string[]>;
}
