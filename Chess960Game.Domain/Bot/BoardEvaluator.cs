using Chess960Game.Domain.Board;
using Chess960Game.Domain.Moves;
using Chess960Game.Domain.Pieces;

namespace Chess960Game.Domain.Bot;

public sealed class BoardEvaluator
{
    private readonly MoveGenerator _moveGenerator;

    public BoardEvaluator(MoveGenerator moveGenerator)
    {
        _moveGenerator = moveGenerator;
    }

    public int Evaluate(Board.Board board)
    {
        int score = 0;

        score += EvaluateMaterial(board);
        score += EvaluatePiecePositions(board);
        score += EvaluateBishopPair(board);
        score += EvaluatePawnStructure(board);

        const int KingSafetyThreshold = 200;

        if (Math.Abs(score) <= KingSafetyThreshold)
        {
            score += EvaluateKingSafety(board);
        }

        return score;
    }

    private int EvaluateMaterial(Board.Board board)
    {
        int score = 0;

        foreach (var (_, piece) in board.GetAllPieces())
        {
            int value = GetPieceValue(piece.Type);
            score += piece.Color == PieceColor.White ? value : -value;
        }

        return score;
    }

    private int EvaluatePiecePositions(Board.Board board)
    {
        int score = 0;

        foreach (var (pos, piece) in board.GetAllPieces())
        {
            int bonus = GetPositionBonus(piece, pos);
            score += piece.Color == PieceColor.White ? bonus : -bonus;
        }

        return score;
    }

    private int EvaluateMobility(Board.Board board)
    {
        int whiteMoves = _moveGenerator.GenerateAllLegalMoves(board, PieceColor.White).Count;
        int blackMoves = _moveGenerator.GenerateAllLegalMoves(board, PieceColor.Black).Count;

        return (whiteMoves - blackMoves) * 5;
    }

    private int EvaluateBishopPair(Board.Board board)
    {
        int whiteBishops = 0;
        int blackBishops = 0;

        foreach (var (_, piece) in board.GetAllPieces())
        {
            if (piece.Type != PieceType.Bishop)
                continue;

            if (piece.Color == PieceColor.White)
                whiteBishops++;
            else
                blackBishops++;
        }

        int score = 0;

        if (whiteBishops >= 2)
            score += 30;

        if (blackBishops >= 2)
            score -= 30;

        return score;
    }

    private int EvaluatePawnStructure(Board.Board board)
    {
        int score = 0;

        score += EvaluateDoubledPawns(board, PieceColor.White);
        score -= EvaluateDoubledPawns(board, PieceColor.Black);

        score += EvaluateIsolatedPawns(board, PieceColor.White);
        score -= EvaluateIsolatedPawns(board, PieceColor.Black);

        score += EvaluatePassedPawns(board, PieceColor.White);
        score -= EvaluatePassedPawns(board, PieceColor.Black);

        return score;
    }

    private int EvaluateDoubledPawns(Board.Board board, PieceColor color)
    {
        int penalty = 0;

        for (int col = 0; col < 8; col++)
        {
            int pawnsOnFile = 0;

            for (int row = 0; row < 8; row++)
            {
                var piece = board.GetPiece(new Position(row, col));

                if (piece is not null &&
                    piece.Color == color &&
                    piece.Type == PieceType.Pawn)
                {
                    pawnsOnFile++;
                }
            }

            if (pawnsOnFile > 1)
                penalty -= (pawnsOnFile - 1) * 10;
        }

        return penalty;
    }

    private int EvaluateIsolatedPawns(Board.Board board, PieceColor color)
    {
        int penalty = 0;

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var piece = board.GetPiece(new Position(row, col));

                if (piece is null ||
                    piece.Color != color ||
                    piece.Type != PieceType.Pawn)
                    continue;

                bool hasFriendlyPawnNearby = HasPawnOnFile(board, color, col - 1) ||
                                             HasPawnOnFile(board, color, col + 1);

                if (!hasFriendlyPawnNearby)
                    penalty -= 15;
            }
        }

        return penalty;
    }

    private int EvaluatePassedPawns(Board.Board board, PieceColor color)
    {
        int bonus = 0;

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var piece = board.GetPiece(new Position(row, col));

                if (piece is null ||
                    piece.Color != color ||
                    piece.Type != PieceType.Pawn)
                    continue;

                if (IsPassedPawn(board, new Position(row, col), color))
                {
                    int advancement = color == PieceColor.White
                        ? 6 - row
                        : row - 1;

                    bonus += 20 + Math.Max(0, advancement) * 10;
                }
            }
        }

        return bonus;
    }

    private bool HasPawnOnFile(Board.Board board, PieceColor color, int col)
    {
        if (col < 0 || col > 7)
            return false;

        for (int row = 0; row < 8; row++)
        {
            var piece = board.GetPiece(new Position(row, col));

            if (piece is not null &&
                piece.Color == color &&
                piece.Type == PieceType.Pawn)
                return true;
        }

        return false;
    }

    private bool IsPassedPawn(Board.Board board, Position pawnPos, PieceColor color)
    {
        PieceColor enemyColor = color == PieceColor.White
            ? PieceColor.Black
            : PieceColor.White;

        int direction = color == PieceColor.White ? -1 : 1;

        for (int col = pawnPos.Col - 1; col <= pawnPos.Col + 1; col++)
        {
            if (col < 0 || col > 7)
                continue;

            int row = pawnPos.Row + direction;

            while (row >= 0 && row < 8)
            {
                var piece = board.GetPiece(new Position(row, col));

                if (piece is not null &&
                    piece.Color == enemyColor &&
                    piece.Type == PieceType.Pawn)
                    return false;

                row += direction;
            }
        }

        return true;
    }

    private int EvaluateKingSafety(Board.Board board)
    {
        int score = 0;

        score += EvaluateKingPawnShield(board, PieceColor.White);
        score -= EvaluateKingPawnShield(board, PieceColor.Black);

        return score;
    }

    private int EvaluateKingPawnShield(Board.Board board, PieceColor color)
    {
        Position kingPos;

        try
        {
            kingPos = board.FindKing(color);
        }
        catch
        {
            return 0;
        }

        int shieldRow = color == PieceColor.White
            ? kingPos.Row - 1
            : kingPos.Row + 1;

        if (shieldRow < 0 || shieldRow > 7)
            return -30;

        int pawns = 0;

        for (int col = kingPos.Col - 1; col <= kingPos.Col + 1; col++)
        {
            if (col < 0 || col > 7)
                continue;

            var piece = board.GetPiece(new Position(shieldRow, col));

            if (piece is not null &&
                piece.Color == color &&
                piece.Type == PieceType.Pawn)
            {
                pawns++;
            }
        }

        return pawns switch
        {
            0 => -40,
            1 => -20,
            2 => 5,
            _ => 15
        };
    }

    private int GetPositionBonus(Piece piece, Position pos)
    {
        int centerDistance =
            Math.Abs(pos.Row - 3) +
            Math.Abs(pos.Col - 3);

        return piece.Type switch
        {
            PieceType.Pawn => GetPawnBonus(piece.Color, pos),
            PieceType.Knight => 40 - centerDistance * 10,
            PieceType.Bishop => 30 - centerDistance * 6,
            PieceType.Rook => GetRookBonus(pos),
            PieceType.Queen => 20 - centerDistance * 4,
            PieceType.King => GetKingBonus(piece.Color, pos),
            _ => 0
        };
    }

    private int GetPawnBonus(PieceColor color, Position pos)
    {
        int advancement = color == PieceColor.White
            ? 6 - pos.Row
            : pos.Row - 1;

        int centerBonus = pos.Col is 3 or 4 ? 10 : 0;

        return advancement * 8 + centerBonus;
    }

    private int GetRookBonus(Position pos)
    {
        // Ладья чуть лучше на открытых центральных линиях
        return pos.Col is 3 or 4 ? 10 : 0;
    }

    private int GetKingBonus(PieceColor color, Position pos)
    {
        // В дебюте и мидгейме король лучше ближе к краю
        bool nearBackRank = color == PieceColor.White
            ? pos.Row >= 6
            : pos.Row <= 1;

        return nearBackRank ? 10 : -20;
    }

    public int GetPieceValue(PieceType type)
    {
        return type switch
        {
            PieceType.Pawn => 100,
            PieceType.Knight => 320,
            PieceType.Bishop => 330,
            PieceType.Rook => 500,
            PieceType.Queen => 900,
            PieceType.King => 20_000,
            _ => 0
        };
    }
}