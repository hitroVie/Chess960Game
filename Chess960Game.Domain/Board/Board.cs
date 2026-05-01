using Chess960Game.Domain.Pieces;

namespace Chess960Game.Domain.Board;

public sealed class Board
{
    private readonly Piece?[,] _cells = new Piece?[8, 8];

    public Piece? GetPiece(Position pos)
    {
        if (!pos.IsValid)
            throw new ArgumentOutOfRangeException(nameof(pos));

        return _cells[pos.Row, pos.Col];
    }

    public void SetPiece(Position pos, Piece? piece)
    {
        if (!pos.IsValid)
            throw new ArgumentOutOfRangeException(nameof(pos));

        _cells[pos.Row, pos.Col] = piece;
    }

    public void MovePiece(Position from, Position to)
    {
        var piece = GetPiece(from);

        if (piece is null)
            throw new InvalidOperationException("No piece at source.");

        SetPiece(to, piece);
        SetPiece(from, null);
    }

    public string ToPrettyString()
    {
        var lines = new List<string>();

        for (int row = 0; row < 8; row++)
        {
            var line = $"{8 - row} ";

            for (int col = 0; col < 8; col++)
            {
                line += (_cells[row, col]?.ToString() ?? ".") + " ";
            }

            lines.Add(line);
        }

        lines.Add("  a b c d e f g h");

        return string.Join(Environment.NewLine, lines);
    }
    public Position FindKing(PieceColor color)
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var pos = new Position(row, col);
                var piece = GetPiece(pos);

                if (piece == null)
                    continue;

                if (piece.Type == PieceType.King &&
                    piece.Color == color)
                {
                    return pos;
                }
            }
        }

        throw new Exception("King not found");
    }
    public Board Clone()
    {
        var clone = new Board();

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var pos = new Position(row, col);
                var piece = GetPiece(pos);

                if (piece is not null)
                {
                    clone.SetPiece(
                        pos,
                        new Piece(piece.Type, piece.Color)
                    );
                }
            }
        }

        return clone;
    }
}