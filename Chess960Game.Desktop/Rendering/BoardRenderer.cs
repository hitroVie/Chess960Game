using Chess960Game.Domain.Board;
using Chess960Game.Domain.Moves;
using Chess960Game.Domain.Pieces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Chess960Game.Domain.Moves;
using System.Collections.Generic;

namespace Chess960Game.Desktop.Rendering;

public class BoardRenderer
{
    private const int TileSize = 120;
    private const int Padding = 60;
    private const int PlatformTopHeight = 88;
    private const int PlatformDepth = 28;
    private const int PlatformGap = 6;
    private const int RowStep = 105;

    public void DrawBoard(
    SpriteBatch spriteBatch,
    Texture2D pixel,
    Func<Position, int> getShockwaveOffset)
    {
        DrawBoardArea(
            spriteBatch,
            pixel,
            8,
            8,
            Padding,
            Padding,
            getShockwaveOffset
        );
    }
    public void DrawBoardArea(
    SpriteBatch spriteBatch,
    Texture2D pixel,
    int rows,
    int cols,
    int startX,
    int startY,
    Func<Position, int>? getShockwaveOffset = null)
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                bool isLight = (row + col) % 2 == 0;

                Color topColor = isLight
                    ? new Color(235, 230, 245)
                    : new Color(120, 95, 170);

                Color sideColor = isLight
                    ? new Color(190, 185, 205)
                    : new Color(75, 45, 130);

                int x = startX + col * TileSize;
                int y = startY + row * RowStep;

                if (getShockwaveOffset is not null)
                    y += getShockwaveOffset(new Position(row, col));

                DrawPlatformTile(spriteBatch, pixel, x, y, topColor, sideColor);
            }
        }
    }
    public void DrawCoordinates(
        SpriteBatch spriteBatch,
        SpriteFont font,
        PieceColor playerColor)
    {
        for (int col = 0; col < 8; col++)
        {
            string letter = playerColor == PieceColor.White
                ? ((char)('a' + col)).ToString()
                : ((char)('h' - col)).ToString();

            Vector2 size = font.MeasureString(letter);

            Vector2 position = new Vector2(
                Padding + col * TileSize + TileSize / 2f - size.X / 2f,
                Padding + 7 * RowStep + PlatformTopHeight + PlatformDepth + 4
            );

            spriteBatch.DrawString(
                font,
                letter,
                position,
                Color.White,
                0f,
                Vector2.Zero,
                0.5f,
                SpriteEffects.None,
                0f
            );
        }

        for (int row = 0; row < 8; row++)
        {
            string number = playerColor == PieceColor.White
                ? (8 - row).ToString()
                : (row + 1).ToString();

            Vector2 size = font.MeasureString(number);

            Vector2 position = new Vector2(
                Padding - 28,
                Padding + row * RowStep + PlatformTopHeight / 2f - size.Y / 2f
            );

            spriteBatch.DrawString(
                font,
                number,
                position,
                Color.White,
                0f,
                Vector2.Zero,
                0.5f,
                SpriteEffects.None,
                0f
            );
        }
    }

    private void DrawPlatformTile(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        int x,
        int y,
        Color topColor,
        Color sideColor)
    {
        int width = TileSize - PlatformGap;

        var topRect = new Rectangle(
            x + PlatformGap / 2,
            y + PlatformGap / 2,
            width,
            PlatformTopHeight
        );

        var sideRect = new Rectangle(
            x + PlatformGap / 2,
            y + PlatformGap / 2 + PlatformTopHeight,
            width,
            PlatformDepth
        );

        spriteBatch.Draw(pixel, topRect, topColor);
        spriteBatch.Draw(pixel, sideRect, sideColor);

        DrawRectangleBorder(spriteBatch, pixel, topRect, new Color(25, 25, 35), 2);
        DrawRectangleBorder(spriteBatch, pixel, sideRect, new Color(25, 25, 35), 2);
    }

    private void DrawRectangleBorder(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        Rectangle rect,
        Color color,
        int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    public void DrawSelectedCell(
    SpriteBatch spriteBatch,
    Texture2D pixel,
    Position? selectedPosition,
    Func<int, int> toScreenRow,
    Func<int, int> toScreenCol)
    {
        if (selectedPosition is null)
            return;

        var pos = selectedPosition.Value;

        int centerX = Padding + toScreenCol(pos.Col) * TileSize + TileSize / 2;
        int centerY = Padding + toScreenRow(pos.Row) * RowStep + PlatformTopHeight / 2;

        DrawCircle(spriteBatch, pixel, centerX, centerY, 44, new Color(70, 0, 120, 45));
        DrawCircle(spriteBatch, pixel, centerX, centerY, 34, new Color(110, 0, 180, 65));
        DrawCircle(spriteBatch, pixel, centerX, centerY, 24, new Color(160, 40, 255, 90));
    }

    public void DrawAvailableMoves(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        List<Move> selectedMoves,
        Func<int, int> toScreenRow,
        Func<int, int> toScreenCol)
    {
        foreach (var move in selectedMoves)
        {
            var pos = move.To;

            int centerX = Padding + toScreenCol(pos.Col) * TileSize + TileSize / 2;
            int centerY = Padding + toScreenRow(pos.Row) * RowStep + PlatformTopHeight / 2;

            DrawCircle(spriteBatch, pixel, centerX, centerY, 10, new Color(60, 60, 60));
        }
    }

    private void DrawCircle(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        int centerX,
        int centerY,
        int radius,
        Color color)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x * x + y * y <= radius * radius)
                {
                    spriteBatch.Draw(
                        pixel,
                        new Rectangle(centerX + x, centerY + y, 1, 1),
                        color
                    );
                }
            }
        }
    }
}