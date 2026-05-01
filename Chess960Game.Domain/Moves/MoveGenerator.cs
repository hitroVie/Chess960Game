using Chess960Game.Domain.Board;
using Chess960Game.Domain.Pieces;

namespace Chess960Game.Domain.Moves;

public sealed class MoveGenerator
{
    public List<Move> GenerateMovesForPiece(Board.Board board, Position from)
    {
        var piece = board.GetPiece(from);

        if (piece is null)
            return new List<Move>();

        return piece.Type switch
        {
            PieceType.Pawn => GeneratePawnMoves(board, from, piece),
            PieceType.Knight => GenerateKnightMoves(board, from, piece),
            PieceType.Bishop => GenerateSlidingMoves(board, from, piece, BishopDirections),
            PieceType.Rook => GenerateSlidingMoves(board, from, piece, RookDirections),
            PieceType.Queen => GenerateSlidingMoves(board, from, piece, QueenDirections),
            PieceType.King => GenerateKingMoves(board, from, piece),
            _ => new List<Move>()
        };
    }

    public List<Move> GenerateLegalMovesForPiece(Board.Board board, Position from)
    {
        var piece = board.GetPiece(from);

        if (piece is null)
            return new List<Move>();

        var pseudoMoves = GenerateMovesForPiece(board, from);
        var legalMoves = new List<Move>();

        foreach (var move in pseudoMoves)
        {
            var boardCopy = board.Clone();
            boardCopy.MovePiece(move.From, move.To);

            bool kingInCheck = IsKingInCheck(boardCopy, piece.Color);

            if (!kingInCheck)
                legalMoves.Add(move);
        }

        return legalMoves;
    }

    public bool IsKingInCheck(Board.Board board, PieceColor color)
    {
        var kingPosition = board.FindKing(color);

        var enemyColor = color == PieceColor.White
            ? PieceColor.Black
            : PieceColor.White;

        return IsSquareAttacked(board, kingPosition, enemyColor);
    }

    private static readonly (int dr, int dc)[] KnightOffsets =
    {
        (-2, -1), (-2, 1),
        (-1, -2), (-1, 2),
        (1, -2), (1, 2),
        (2, -1), (2, 1)
    };

    private static readonly (int dr, int dc)[] KingOffsets =
    {
        (-1, -1), (-1, 0), (-1, 1),
        (0, -1),           (0, 1),
        (1, -1),  (1, 0),  (1, 1)
    };

    private static readonly (int dr, int dc)[] BishopDirections =
    {
        (-1, -1), (-1, 1),
        (1, -1),  (1, 1)
    };

    private static readonly (int dr, int dc)[] RookDirections =
    {
        (-1, 0),
        (1, 0),
        (0, -1),
        (0, 1)
    };

    private static readonly (int dr, int dc)[] QueenDirections =
    {
        (-1, -1), (-1, 1),
        (1, -1),  (1, 1),
        (-1, 0),
        (1, 0),
        (0, -1),
        (0, 1)
    };

    private List<Move> GenerateKnightMoves(Board.Board board, Position from, Piece piece)
    {
        var moves = new List<Move>();

        foreach (var (dr, dc) in KnightOffsets)
        {
            var to = new Position(from.Row + dr, from.Col + dc);
            TryAddMove(board, moves, from, to, piece);
        }

        return moves;
    }

    private List<Move> GenerateKingMoves(Board.Board board, Position from, Piece piece)
    {
        var moves = new List<Move>();

        foreach (var (dr, dc) in KingOffsets)
        {
            var to = new Position(from.Row + dr, from.Col + dc);
            TryAddMove(board, moves, from, to, piece);
        }

        return moves;
    }

    private List<Move> GenerateSlidingMoves(
        Board.Board board,
        Position from,
        Piece piece,
        (int dr, int dc)[] directions)
    {
        var moves = new List<Move>();

        foreach (var (dr, dc) in directions)
        {
            int row = from.Row + dr;
            int col = from.Col + dc;

            while (true)
            {
                var to = new Position(row, col);

                if (!to.IsValid)
                    break;

                var targetPiece = board.GetPiece(to);

                if (targetPiece is null)
                {
                    moves.Add(new Move(from, to, piece.Type));
                }
                else
                {
                    if (targetPiece.Color != piece.Color &&
                        targetPiece.Type != PieceType.King)
                    {
                        moves.Add(new Move(
                            from,
                            to,
                            piece.Type,
                            IsCapture: true));
                    }

                    break;
                }

                row += dr;
                col += dc;
            }
        }

        return moves;
    }

    private List<Move> GeneratePawnMoves(Board.Board board, Position from, Piece piece)
    {
        var moves = new List<Move>();

        int direction = piece.Color == PieceColor.White ? -1 : 1;
        int startRow = piece.Color == PieceColor.White ? 6 : 1;
        int promotionRow = piece.Color == PieceColor.White ? 0 : 7;

        var oneStep = new Position(from.Row + direction, from.Col);

        if (oneStep.IsValid && board.GetPiece(oneStep) is null)
        {
            AddPawnMove(moves, from, oneStep, piece, promotionRow);

            var twoStep = new Position(from.Row + direction * 2, from.Col);

            if (from.Row == startRow &&
                twoStep.IsValid &&
                board.GetPiece(twoStep) is null)
            {
                moves.Add(new Move(from, twoStep, piece.Type));
            }
        }

        var captureLeft = new Position(from.Row + direction, from.Col - 1);
        var captureRight = new Position(from.Row + direction, from.Col + 1);

        TryAddPawnCapture(board, moves, from, captureLeft, piece, promotionRow);
        TryAddPawnCapture(board, moves, from, captureRight, piece, promotionRow);

        return moves;
    }
    public List<Move> GenerateAllLegalMoves(Board.Board board, PieceColor color)
    {
        var allMoves = new List<Move>();

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var pos = new Position(row, col);
                var piece = board.GetPiece(pos);

                if (piece is null)
                    continue;

                if (piece.Color != color)
                    continue;

                var moves = GenerateLegalMovesForPiece(board, pos);
                allMoves.AddRange(moves);
            }
        }

        return allMoves;
    }

    private void AddPawnMove(
        List<Move> moves,
        Position from,
        Position to,
        Piece piece,
        int promotionRow)
    {
        if (to.Row == promotionRow)
        {
            moves.Add(new Move(
                from,
                to,
                piece.Type,
                Promotion: PieceType.Queen));
        }
        else
        {
            moves.Add(new Move(from, to, piece.Type));
        }
    }

    private void TryAddPawnCapture(
        Board.Board board,
        List<Move> moves,
        Position from,
        Position to,
        Piece piece,
        int promotionRow)
    {
        if (!to.IsValid)
            return;

        var targetPiece = board.GetPiece(to);

        if (targetPiece is null)
            return;

        if (targetPiece.Color == piece.Color)
            return;

        if (targetPiece.Type == PieceType.King)
            return;

        if (to.Row == promotionRow)
        {
            moves.Add(new Move(
                from,
                to,
                piece.Type,
                IsCapture: true,
                Promotion: PieceType.Queen));
        }
        else
        {
            moves.Add(new Move(
                from,
                to,
                piece.Type,
                IsCapture: true));
        }
    }

    private void TryAddMove(
        Board.Board board,
        List<Move> moves,
        Position from,
        Position to,
        Piece piece)
    {
        if (!to.IsValid)
            return;

        var targetPiece = board.GetPiece(to);

        if (targetPiece is null)
        {
            moves.Add(new Move(from, to, piece.Type));
            return;
        }

        if (targetPiece.Color != piece.Color)
        {
            if (targetPiece.Type == PieceType.King)
                return;

            moves.Add(new Move(
                from,
                to,
                piece.Type,
                IsCapture: true));
        }
    }
    private bool IsSquareAttacked(Board.Board board, Position target, PieceColor byColor)
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var from = new Position(row, col);
                var piece = board.GetPiece(from);

                if (piece is null)
                    continue;

                if (piece.Color != byColor)
                    continue;

                if (AttacksSquare(board, from, piece, target))
                    return true;
            }
        }

        return false;
    }
    private bool AttacksSquare(Board.Board board, Position from, Piece piece, Position target)
    {
        int dr = target.Row - from.Row;
        int dc = target.Col - from.Col;

        return piece.Type switch
        {
            PieceType.Knight =>
                (Math.Abs(dr) == 2 && Math.Abs(dc) == 1) ||
                (Math.Abs(dr) == 1 && Math.Abs(dc) == 2),

            PieceType.King =>
                Math.Abs(dr) <= 1 &&
                Math.Abs(dc) <= 1 &&
                (dr != 0 || dc != 0),

            PieceType.Pawn =>
                piece.Color == PieceColor.White
                    ? dr == -1 && Math.Abs(dc) == 1
                    : dr == 1 && Math.Abs(dc) == 1,

            PieceType.Bishop =>
                Math.Abs(dr) == Math.Abs(dc) &&
                IsPathClear(board, from, target),

            PieceType.Rook =>
                (dr == 0 || dc == 0) &&
                IsPathClear(board, from, target),

            PieceType.Queen =>
                (
                    Math.Abs(dr) == Math.Abs(dc) ||
                    dr == 0 ||
                    dc == 0
                ) &&
                IsPathClear(board, from, target),

            _ => false
        };
    }
    private bool IsPathClear(Board.Board board, Position from, Position to)
    {
        int rowStep = Math.Sign(to.Row - from.Row);
        int colStep = Math.Sign(to.Col - from.Col);

        int row = from.Row + rowStep;
        int col = from.Col + colStep;

        while (row != to.Row || col != to.Col)
        {
            var pos = new Position(row, col);

            if (board.GetPiece(pos) is not null)
                return false;

            row += rowStep;
            col += colStep;
        }

        return true;
    }
}