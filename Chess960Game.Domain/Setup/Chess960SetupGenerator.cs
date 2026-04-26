using Chess960Game.Domain.Board;
using Chess960Game.Domain.Game;
using Chess960Game.Domain.Pieces;

namespace Chess960Game.Domain.Setup;

public sealed class Chess960SetupGenerator
{
    private readonly Random _random;

    public Chess960SetupGenerator(Random? random = null)
    {
        _random = random ?? new Random();
    }

    public GameState CreateNewGame()
    {
        var backRank = GenerateBackRank();
        var board = new Board.Board();

        for (int col = 0; col < 8; col++)
        {
            board.SetPiece(new Position(7, col), new Piece(backRank[col], PieceColor.White));
            board.SetPiece(new Position(6, col), new Piece(PieceType.Pawn, PieceColor.White));

            board.SetPiece(new Position(0, col), new Piece(backRank[col], PieceColor.Black));
            board.SetPiece(new Position(1, col), new Piece(PieceType.Pawn, PieceColor.Black));
        }

        int kingCol = Array.IndexOf(backRank, PieceType.King);

        int[] rookCols = backRank
            .Select((piece, index) => new { piece, index })
            .Where(x => x.piece == PieceType.Rook)
            .Select(x => x.index)
            .OrderBy(x => x)
            .ToArray();

        int queensideRookCol = rookCols[0];
        int kingsideRookCol = rookCols[1];

        return new GameState(
            board,
            PieceColor.White,
            whiteKingStart: new Position(7, kingCol),
            blackKingStart: new Position(0, kingCol),
            whiteKingsideRookStart: new Position(7, kingsideRookCol),
            whiteQueensideRookStart: new Position(7, queensideRookCol),
            blackKingsideRookStart: new Position(0, kingsideRookCol),
            blackQueensideRookStart: new Position(0, queensideRookCol)
        );
    }

    private PieceType[] GenerateBackRank()
    {
        var result = new PieceType?[8];

        int[] darkSquares = { 0, 2, 4, 6 };
        int[] lightSquares = { 1, 3, 5, 7 };

        int bishop1 = darkSquares[_random.Next(darkSquares.Length)];
        int bishop2 = lightSquares[_random.Next(lightSquares.Length)];

        result[bishop1] = PieceType.Bishop;
        result[bishop2] = PieceType.Bishop;

        int queenPos = PickRandomEmpty(result);
        result[queenPos] = PieceType.Queen;

        int knight1 = PickRandomEmpty(result);
        result[knight1] = PieceType.Knight;

        int knight2 = PickRandomEmpty(result);
        result[knight2] = PieceType.Knight;

        var remaining = Enumerable.Range(0, 8)
            .Where(i => result[i] is null)
            .OrderBy(i => i)
            .ToArray();

        result[remaining[0]] = PieceType.Rook;
        result[remaining[1]] = PieceType.King;
        result[remaining[2]] = PieceType.Rook;

        return result.Select(x => x!.Value).ToArray();
    }

    private int PickRandomEmpty(PieceType?[] positions)
    {
        var empty = Enumerable.Range(0, positions.Length)
            .Where(i => positions[i] is null)
            .ToArray();

        return empty[_random.Next(empty.Length)];
    }
}