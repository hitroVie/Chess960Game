using Chess960Game.Domain.Board;
using Chess960Game.Domain.Pieces;

namespace Chess960Game.Domain.Moves;

public readonly record struct Move(
    Position From,
    Position To,
    PieceType PieceType,
    bool IsCapture = false,
    bool IsCastling = false,
    bool IsEnPassant = false,
    PieceType? Promotion = null
);