using Chess960Game.Domain.Board;
using Chess960Game.Domain.Moves;
using Chess960Game.Domain.Pieces;
using System.Collections.Concurrent;
using System.Text;

namespace Chess960Game.Domain.Bot;

public sealed class SimpleChessBot
{
    private readonly BoardEvaluator _evaluator;
    private readonly AlphaBetaSearch _search;

    private const int SearchDepth = 3;

    private readonly ConcurrentDictionary<string, Move> _bestMoveCache = new();

    public SimpleChessBot(MoveGenerator moveGenerator)
    {
        _evaluator = new BoardEvaluator(moveGenerator);
        _search = new AlphaBetaSearch(moveGenerator, _evaluator);
    }

    public Move? ChooseMove(Board.Board board, PieceColor botColor, int fullMoveNumber)
    {
        string key = BuildPositionKey(board, botColor, SearchDepth);

        if (_bestMoveCache.TryGetValue(key, out var cachedMove))
        {
            return cachedMove;
        }

        var bestMove = _search.FindBestMoveParallel(board, botColor, SearchDepth);

        if (bestMove is not null)
        {
            _bestMoveCache[key] = bestMove.Value;
        }

        return bestMove;
    }

    private string BuildPositionKey(Board.Board board, PieceColor sideToMove, int depth)
    {
        var builder = new StringBuilder();

        builder.Append(sideToMove);
        builder.Append('|');
        builder.Append(depth);
        builder.Append('|');

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var piece = board.GetPiece(new Position(row, col));

                if (piece is null)
                {
                    builder.Append('.');
                    continue;
                }

                builder.Append(GetPieceChar(piece));
            }
        }

        return builder.ToString();
    }

    private char GetPieceChar(Piece piece)
    {
        char symbol = piece.Type switch
        {
            PieceType.King => 'K',
            PieceType.Queen => 'Q',
            PieceType.Rook => 'R',
            PieceType.Bishop => 'B',
            PieceType.Knight => 'N',
            PieceType.Pawn => 'P',
            _ => '?'
        };

        return piece.Color == PieceColor.White
            ? symbol
            : char.ToLower(symbol);
    }
}