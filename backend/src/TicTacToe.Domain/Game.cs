namespace TicTacToe.Domain;

/// <summary>
/// One Tic Tac Toe game session. The move list is the single source of truth:
/// the board, current player and status are all derived from it, so undo
/// can never leave them out of sync.
/// </summary>
public sealed class Game
{
    public const int Size = 3;

    private readonly Player?[] _board = new Player?[Size * Size];
    private readonly List<Move> _moves = [];

    public Game(GameMode mode)
        : this(Guid.NewGuid(), mode)
    {
    }

    public Game(Guid id, GameMode mode)
    {
        Id = id;
        Mode = mode;
    }

    public Guid Id { get; }

    public GameMode Mode { get; }

    // Read-only wrappers so callers can't cast back to the array/list and mutate state.
    public IReadOnlyList<Player?> Board => Array.AsReadOnly(_board);

    public IReadOnlyList<Move> Moves => _moves.AsReadOnly();

    public Player CurrentPlayer { get; private set; } = Player.X;

    public GameStatus Status { get; private set; } = GameStatus.InProgress;

    public Player? Winner { get; private set; }

    public IReadOnlyList<int> WinningCells { get; private set; } = [];

    public bool IsCompleted => Status != GameStatus.InProgress;

    // Option A: undo is disabled once the game is won or drawn.
    public bool CanUndo => !IsCompleted && _moves.Count > 0;

    /// <summary>
    /// Plays a human move. In computer mode the computer (O) replies immediately,
    /// unless the human's move completed the game.
    /// </summary>
    public void MakeMove(Player player, int row, int col)
    {
        Place(player, row, col);

        if (Mode == GameMode.VsComputer && !IsCompleted)
        {
            var cell = ComputerPlayer.ChooseMove(_board);
            Place(ComputerPlayer.Mark, cell / Size, cell % Size);
        }
    }

    /// <summary>
    /// Two-player: removes the last move. Computer mode: removes moves until it
    /// is the human's (X's) turn again, i.e. the computer's reply and the human
    /// move before it.
    /// </summary>
    public void Undo()
    {
        if (IsCompleted)
        {
            throw new GameRuleException(GameRuleError.GameAlreadyCompleted, "Undo is disabled after the game is completed.");
        }

        if (_moves.Count == 0)
        {
            throw new GameRuleException(GameRuleError.NothingToUndo, "There are no moves to undo.");
        }

        _moves.RemoveAt(_moves.Count - 1);

        // An odd move count means it would be O's turn, which the human never plays in computer mode.
        while (Mode == GameMode.VsComputer && _moves.Count % 2 == 1)
        {
            _moves.RemoveAt(_moves.Count - 1);
        }

        RebuildFromMoves();
    }

    /// <summary>Starts a fresh game in the same session (same id and mode).</summary>
    public void Reset()
    {
        _moves.Clear();
        RebuildFromMoves();
    }

    private void Place(Player player, int row, int col)
    {
        if (IsCompleted)
        {
            throw new GameRuleException(GameRuleError.GameAlreadyCompleted, "The game is already completed.");
        }

        if (player != CurrentPlayer)
        {
            throw new GameRuleException(GameRuleError.WrongPlayer, $"It is {CurrentPlayer}'s turn, not {player}'s.");
        }

        if (row is < 0 or >= Size || col is < 0 or >= Size)
        {
            throw new GameRuleException(GameRuleError.OutOfBounds, $"Cell ({row}, {col}) is outside the {Size}x{Size} board.");
        }

        var index = row * Size + col;
        if (_board[index] is not null)
        {
            throw new GameRuleException(GameRuleError.CellOccupied, $"Cell ({row}, {col}) is already taken by {_board[index]}.");
        }

        _board[index] = player;
        _moves.Add(new Move(_moves.Count + 1, player, row, col));
        Evaluate();
    }

    private void RebuildFromMoves()
    {
        Array.Clear(_board);
        foreach (var move in _moves)
        {
            _board[move.Row * Size + move.Col] = move.Player;
        }

        Evaluate();
    }

    private void Evaluate()
    {
        // X always starts, so whose turn it is follows from the number of moves.
        CurrentPlayer = _moves.Count % 2 == 0 ? Player.X : Player.O;

        var win = WinDetector.FindWinner(_board);
        if (win is not null)
        {
            Status = GameStatus.Won;
            Winner = win.Winner;
            WinningCells = win.Cells;
        }
        else
        {
            Status = WinDetector.IsFull(_board) ? GameStatus.Draw : GameStatus.InProgress;
            Winner = null;
            WinningCells = [];
        }
    }
}
