using Chess960Game.Domain.Board;
using Chess960Game.Domain.Moves;
using Chess960Game.Domain.Pieces;

namespace Chess960Game.Domain.Bot;

public sealed class AlphaBetaSearch
{
    private readonly MoveGenerator _moveGenerator;
    private readonly BoardEvaluator _evaluator;
    private readonly Random _random = new();

    public AlphaBetaSearch(MoveGenerator moveGenerator, BoardEvaluator evaluator)
    {
        _moveGenerator = moveGenerator;
        _evaluator = evaluator;
    }

    public Move? FindBestMove(Board.Board board, PieceColor sideToMove, int depth)
    {
        var moves = _moveGenerator.GenerateAllLegalMoves(board, sideToMove);

        if (moves.Count == 0)
            return null;

        moves = OrderMoves(board, moves, sideToMove);

        Move bestMove = moves[0];

        int bestScore = sideToMove == PieceColor.White
            ? int.MinValue
            : int.MaxValue;

        foreach (var move in moves)
        {
            var boardCopy = board.Clone();

            ApplyMove(boardCopy, move, sideToMove);

            var nextSide = Opposite(sideToMove);

            int score = AlphaBeta(
                boardCopy,
                nextSide,
                depth - 1,
                int.MinValue + 1,
                int.MaxValue - 1);

            if (sideToMove == PieceColor.White)
            {
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
                else if (score == bestScore && _random.Next(2) == 0)
                {
                    bestMove = move;
                }
            }
            else
            {
                if (score < bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
                else if (score == bestScore && _random.Next(2) == 0)
                {
                    bestMove = move;
                }
            }
        }

        return bestMove;
    }

    private int AlphaBeta(
        Board.Board board,
        PieceColor sideToMove,
        int depth,
        int alpha,
        int beta)
    {
        var moves = _moveGenerator.GenerateAllLegalMoves(board, sideToMove);

        if (moves.Count == 0)
        {
            bool inCheck = _moveGenerator.IsKingInCheck(board, sideToMove);

            if (inCheck)
            {
                return sideToMove == PieceColor.White
                    ? -1_000_000 - depth
                    : 1_000_000 + depth;
            }

            return 0;
        }

        if (depth == 0)
            return _evaluator.Evaluate(board);

        moves = OrderMoves(board, moves, sideToMove);

        if (sideToMove == PieceColor.White)
        {
            int bestScore = int.MinValue + 1;

            foreach (var move in moves)
            {
                var boardCopy = board.Clone();
                ApplyMove(boardCopy, move, sideToMove);

                int score = AlphaBeta(
                    boardCopy,
                    PieceColor.Black,
                    depth - 1,
                    alpha,
                    beta);

                bestScore = Math.Max(bestScore, score);
                alpha = Math.Max(alpha, bestScore);

                if (alpha >= beta)
                    break;
            }

            return bestScore;
        }
        else
        {
            int bestScore = int.MaxValue - 1;

            foreach (var move in moves)
            {
                var boardCopy = board.Clone();
                ApplyMove(boardCopy, move, sideToMove);

                int score = AlphaBeta(
                    boardCopy,
                    PieceColor.White,
                    depth - 1,
                    alpha,
                    beta);

                bestScore = Math.Min(bestScore, score);
                beta = Math.Min(beta, bestScore);

                if (alpha >= beta)
                    break;
            }

            return bestScore;
        }
    }

    private List<Move> OrderMoves(Board.Board board, List<Move> moves, PieceColor sideToMove)
    {
        return moves
            .OrderByDescending(move => ScoreMove(board, move, sideToMove))
            .ToList();
    }

    private int ScoreMove(Board.Board board, Move move, PieceColor sideToMove)
    {
        int score = 0;

        var movingPiece = board.GetPiece(move.From);
        var capturedPiece = board.GetPiece(move.To);

        if (capturedPiece is not null && movingPiece is not null)
        {
            score += 10_000;
            score += _evaluator.GetPieceValue(capturedPiece.Type);
            score -= _evaluator.GetPieceValue(movingPiece.Type) / 10;
        }

        if (move.Promotion is not null)
        {
            score += 9_000;
            score += _evaluator.GetPieceValue(move.Promotion.Value);
        }

        var boardCopy = board.Clone();
        ApplyMove(boardCopy, move, sideToMove);

        var enemy = Opposite(sideToMove);

        if (_moveGenerator.IsKingInCheck(boardCopy, enemy))
            score += 2_000;

        return score;
    }

    private void ApplyMove(Board.Board board, Move move, PieceColor color)
    {
        if (move.Promotion is not null)
        {
            board.SetPiece(move.From, null);
            board.SetPiece(
                move.To,
                new Piece(move.Promotion.Value, color)
            );
        }
        else
        {
            board.MovePiece(move.From, move.To);
        }
    }

    private PieceColor Opposite(PieceColor color)
    {
        return color == PieceColor.White
            ? PieceColor.Black
            : PieceColor.White;
    }
}