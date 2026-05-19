using Chess960Game.Domain.Board;
using Chess960Game.Domain.Pieces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Chess960Game.Desktop.Rendering;

public class PieceRenderer
{
    private readonly Dictionary<(PieceColor, PieceType), Texture2D> _textures = new();

    public void LoadContent(ContentManager content)
    {
        _textures[(PieceColor.White, PieceType.King)] = content.Load<Texture2D>("Pieces/white_king");
        _textures[(PieceColor.White, PieceType.Queen)] = content.Load<Texture2D>("Pieces/white_queen");
        _textures[(PieceColor.White, PieceType.Rook)] = content.Load<Texture2D>("Pieces/white_rook");
        _textures[(PieceColor.White, PieceType.Bishop)] = content.Load<Texture2D>("Pieces/white_bishop");
        _textures[(PieceColor.White, PieceType.Knight)] = content.Load<Texture2D>("Pieces/white_knight");
        _textures[(PieceColor.White, PieceType.Pawn)] = content.Load<Texture2D>("Pieces/white_pawn");

        _textures[(PieceColor.Black, PieceType.King)] = content.Load<Texture2D>("Pieces/black_king");
        _textures[(PieceColor.Black, PieceType.Queen)] = content.Load<Texture2D>("Pieces/black_queen");
        _textures[(PieceColor.Black, PieceType.Rook)] = content.Load<Texture2D>("Pieces/black_rook");
        _textures[(PieceColor.Black, PieceType.Bishop)] = content.Load<Texture2D>("Pieces/black_bishop");
        _textures[(PieceColor.Black, PieceType.Knight)] = content.Load<Texture2D>("Pieces/black_knight");
        _textures[(PieceColor.Black, PieceType.Pawn)] = content.Load<Texture2D>("Pieces/black_pawn");
    }

    public void DrawPieces(
        SpriteBatch spriteBatch,
        Board board,
        int pieceSize,
        Func<Position, int, Vector2> getPieceDrawPosition,
        bool isAnimating,
        Position animationTo,
        Piece? animatedPiece)
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var position = new Position(row, col);
                var piece = board.GetPiece(position);

                if (piece is null)
                    continue;

                if (isAnimating && animatedPiece is not null && position == animationTo)
                    continue;

                if (!_textures.TryGetValue((piece.Color, piece.Type), out var texture))
                    continue;

                Vector2 drawPosition = getPieceDrawPosition(position, pieceSize);

                var destination = new Rectangle(
                    (int)drawPosition.X,
                    (int)drawPosition.Y,
                    pieceSize,
                    pieceSize
                );

                spriteBatch.Draw(texture, destination, Color.White);
            }
        }
    }

    public bool TryGetTexture(Piece piece, out Texture2D texture)
    {
        return _textures.TryGetValue((piece.Color, piece.Type), out texture);
    }
}