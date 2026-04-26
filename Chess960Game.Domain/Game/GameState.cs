using Chess960Game.Domain.Board;
using Chess960Game.Domain.Pieces;

namespace Chess960Game.Domain.Game;

public sealed class GameState
{
    public Board.Board Board { get; }
    public PieceColor SideToMove { get; private set; }

    public Position WhiteKingStart { get; }
    public Position BlackKingStart { get; }

    public Position WhiteKingsideRookStart { get; }
    public Position WhiteQueensideRookStart { get; }

    public Position BlackKingsideRookStart { get; }
    public Position BlackQueensideRookStart { get; }

    public GameState(
        Board.Board board,
        PieceColor sideToMove,
        Position whiteKingStart,
        Position blackKingStart,
        Position whiteKingsideRookStart,
        Position whiteQueensideRookStart,
        Position blackKingsideRookStart,
        Position blackQueensideRookStart)
    {
        Board = board;
        SideToMove = sideToMove;
        WhiteKingStart = whiteKingStart;
        BlackKingStart = blackKingStart;
        WhiteKingsideRookStart = whiteKingsideRookStart;
        WhiteQueensideRookStart = whiteQueensideRookStart;
        BlackKingsideRookStart = blackKingsideRookStart;
        BlackQueensideRookStart = blackQueensideRookStart;
    }

    public void SwitchTurn()
    {
        SideToMove = SideToMove == PieceColor.White
            ? PieceColor.Black
            : PieceColor.White;
    }
}