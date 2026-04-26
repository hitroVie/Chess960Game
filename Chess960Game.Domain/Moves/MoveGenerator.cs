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
            var row = from.Row + dr;
            var col = from.Col + dc;

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
                    if (targetPiece.Color != piece.Color)
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

    private void AddPawnMove(
        List<Move> moves,
        Position from,
        Position to,
        Piece piece,
        int promotionRow)
    {
        if (to.Row == promotionRow)
        {
            moves.Add(new Move(from, to, piece.Type, Promotion: PieceType.Queen));
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
            moves.Add(new Move(
                from,
                to,
                piece.Type,
                IsCapture: true));
        }
    }
}