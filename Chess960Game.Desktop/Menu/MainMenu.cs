#nullable enable
using Chess960Game.Desktop.Animation;
using Chess960Game.Desktop.Rendering;
using Chess960Game.Domain.Board;
using Chess960Game.Domain.Pieces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chess960Game.Desktop.Menu;

public enum MainMenuResult
{
    None,
    Difficulty1,
    Difficulty2,
    Difficulty3
}

public class MainMenu
{
    private const int TileSize = 120;
    private const int RowStep = 105;
    private const int PlatformTopHeight = 88;
    private const int PlatformGap = 6;
    private const int PieceSize = 90;

    private readonly Random _random = new();

    private readonly List<Position> _centralPositions = new()
    {
        new Position(1, 1),
        new Position(1, 2),
        new Position(2, 1),
        new Position(2, 2)
    };

    private readonly MoveAnimation _moveAnimation = new();

    private Position _knightPosition;
    private readonly Dictionary<Position, MainMenuResult> _difficultyTargets = new();

    private MainMenuResult _pendingResult = MainMenuResult.None;

    private readonly int _startX = 300;
    private readonly int _startY = 190;

    public MainMenu()
    {
        Reset();
    }

    public void Reset()
    {
        _difficultyTargets.Clear();

        _knightPosition = _centralPositions[_random.Next(_centralPositions.Count)];

        var moves = GetKnightMoves(_knightPosition)
            .Take(3)
            .ToList();

        if (moves.Count > 0)
            _difficultyTargets[moves[0]] = MainMenuResult.Difficulty1;

        if (moves.Count > 1)
            _difficultyTargets[moves[1]] = MainMenuResult.Difficulty2;

        if (moves.Count > 2)
            _difficultyTargets[moves[2]] = MainMenuResult.Difficulty3;

        _pendingResult = MainMenuResult.None;
    }

    public MainMenuResult Update(
        GameTime gameTime,
        MouseState mouseState,
        MouseState previousMouseState)
    {
        bool finishedMove = _moveAnimation.Update(
            (float)gameTime.ElapsedGameTime.TotalSeconds
        );

        if (finishedMove || (!_moveAnimation.IsActive && _pendingResult != MainMenuResult.None))
        {
            var result = _pendingResult;
            _pendingResult = MainMenuResult.None;
            return result;
        }

        if (_moveAnimation.IsActive)
            return MainMenuResult.None;

        bool leftClicked =
            mouseState.LeftButton == ButtonState.Pressed &&
            previousMouseState.LeftButton == ButtonState.Released;

        if (!leftClicked)
            return MainMenuResult.None;

        if (!TryGetMenuPosition(mouseState.X, mouseState.Y, out var clickedPosition))
            return MainMenuResult.None;

        if (!_difficultyTargets.TryGetValue(clickedPosition, out var selectedDifficulty))
            return MainMenuResult.None;

        _pendingResult = selectedDifficulty;

        _moveAnimation.Start(
            new Piece(PieceType.Knight, PieceColor.White),
            _knightPosition,
            clickedPosition,
            MoveAnimationType.Normal
        );

        _knightPosition = clickedPosition;

        return MainMenuResult.None;
    }

    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        SpriteFont font,
        BoardRenderer boardRenderer,
        PieceRenderer pieceRenderer)
    {
        DrawTitle(spriteBatch, font);

        boardRenderer.DrawBoardArea(
            spriteBatch,
            pixel,
            4,
            4,
            _startX,
            _startY
        );

        DrawDifficultyLabels(spriteBatch, font);
        DrawKnight(spriteBatch, pieceRenderer);
    }

    private void DrawTitle(SpriteBatch spriteBatch, SpriteFont font)
    {
        string title = "NEON GAMBIT";
        float scale = 0.8f;

        Vector2 size = font.MeasureString(title) * scale;

        Vector2 position = new Vector2(
            _startX + TileSize * 2f - size.X / 2f,
            90
        );

        spriteBatch.DrawString(
            font,
            title,
            position,
            Color.White,
            0f,
            Vector2.Zero,
            scale,
            SpriteEffects.None,
            0f
        );
    }

    private void DrawDifficultyLabels(SpriteBatch spriteBatch, SpriteFont font)
    {
        foreach (var target in _difficultyTargets)
        {
            string text = target.Value switch
            {
                MainMenuResult.Difficulty1 => "1",
                MainMenuResult.Difficulty2 => "2",
                MainMenuResult.Difficulty3 => "3",
                _ => ""
            };

            var position = GetCellCenter(target.Key);

            float scale = 0.65f;
            Vector2 size = font.MeasureString(text) * scale;

            spriteBatch.DrawString(
                font,
                text,
                new Vector2(position.X - size.X / 2f, position.Y - size.Y / 2f),
                Color.White,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f
            );
        }
    }

    private void DrawKnight(SpriteBatch spriteBatch, PieceRenderer pieceRenderer)
    {
        var piece = new Piece(PieceType.Knight, PieceColor.White);

        if (!pieceRenderer.TryGetTexture(piece, out var texture))
            return;

        Vector2 drawPosition;

        if (_moveAnimation.IsActive && _moveAnimation.Piece is not null)
        {
            float t = SmoothStep(_moveAnimation.Progress);

            Vector2 from = GetPieceDrawPosition(_moveAnimation.From);
            Vector2 to = GetPieceDrawPosition(_moveAnimation.To);

            drawPosition = Vector2.Lerp(from, to, t);

            float arc = MathF.Sin(t * MathF.PI) * _moveAnimation.ArcHeight;
            drawPosition.Y -= arc;
        }
        else
        {
            drawPosition = GetPieceDrawPosition(_knightPosition);
        }

        var destination = new Rectangle(
            (int)drawPosition.X,
            (int)drawPosition.Y,
            PieceSize,
            PieceSize
        );

        spriteBatch.Draw(texture, destination, Color.White);
    }

    private List<Position> GetKnightMoves(Position from)
    {
        int[] rowOffsets = { -2, -2, -1, -1, 1, 1, 2, 2 };
        int[] colOffsets = { -1, 1, -2, 2, -2, 2, -1, 1 };

        var moves = new List<Position>();

        for (int i = 0; i < rowOffsets.Length; i++)
        {
            int row = from.Row + rowOffsets[i];
            int col = from.Col + colOffsets[i];

            if (row < 0 || row >= 4)
                continue;

            if (col < 0 || col >= 4)
                continue;

            moves.Add(new Position(row, col));
        }

        return moves;
    }

    private bool TryGetMenuPosition(int mouseX, int mouseY, out Position position)
    {
        position = default;

        int localX = mouseX - _startX;
        int localY = mouseY - _startY;

        if (localX < 0 || localY < 0)
            return false;

        int col = localX / TileSize;
        int row = localY / RowStep;

        if (row < 0 || row >= 4 || col < 0 || col >= 4)
            return false;

        position = new Position(row, col);
        return true;
    }

    private Vector2 GetCellCenter(Position position)
    {
        return new Vector2(
            _startX + position.Col * TileSize + TileSize / 2f,
            _startY + position.Row * RowStep + PlatformTopHeight / 2f
        );
    }

    private Vector2 GetPieceDrawPosition(Position position)
    {
        int pieceX = _startX + position.Col * TileSize + (TileSize - PieceSize) / 2;

        int pieceBottomY =
            _startY +
            position.Row * RowStep +
            PlatformGap / 2 +
            PlatformTopHeight / 2 +
            18;

        int pieceY = pieceBottomY - PieceSize;

        return new Vector2(pieceX, pieceY);
    }

    private float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }
}