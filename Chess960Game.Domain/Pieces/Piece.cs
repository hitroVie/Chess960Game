namespace Chess960Game.Domain.Pieces;

public sealed class Piece
{
    public PieceType Type { get; }
    public PieceColor Color { get; }

    public Piece(PieceType type, PieceColor color)
    {
        Type = type;
        Color = color;
    }

    public override string ToString()
    {
        string symbol = Type switch
        {
            PieceType.King => "K",
            PieceType.Queen => "Q",
            PieceType.Rook => "R",
            PieceType.Bishop => "B",
            PieceType.Knight => "N",
            PieceType.Pawn => "P",
            _ => "?"
        };

        return Color == PieceColor.White
            ? symbol
            : symbol.ToLower();
    }
}