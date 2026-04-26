namespace Chess960Game.Domain.Board;

public readonly record struct Position(int Row, int Col)
{
    public bool IsValid =>
        Row >= 0 && Row < 8 &&
        Col >= 0 && Col < 8;

    public override string ToString()
    {
        char file = (char)('a' + Col);
        int rank = 8 - Row;
        return $"{file}{rank}";
    }
}