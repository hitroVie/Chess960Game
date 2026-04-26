using System.Linq;
using Chess960Game.Domain.Board;
using Chess960Game.Domain.Game;
using Chess960Game.Domain.Moves;
using Chess960Game.Domain.Pieces;
using Chess960Game.Domain.Setup;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO.Pipelines;

namespace Chess960Game.Desktop;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _pixel;
    private SpriteFont _font;

    private GameState _game;
    private MoveGenerator _moveGenerator;

    private MouseState _previousMouseState;
    private Position? _selectedPosition;

    private const int TileSize = 80;
    private const int Padding = 40;
    private const int BoardSize = TileSize * 8;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        int windowSize = BoardSize + Padding * 2;

        _graphics.PreferredBackBufferWidth = windowSize;
        _graphics.PreferredBackBufferHeight = windowSize;
        _graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        var setupGenerator = new Chess960SetupGenerator();

        _game = setupGenerator.CreateNewGame();
        _moveGenerator = new MoveGenerator();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _font = Content.Load<SpriteFont>("DefaultFont");
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        var mouseState = Mouse.GetState();

        bool leftClicked =
            mouseState.LeftButton == ButtonState.Pressed &&
            _previousMouseState.LeftButton == ButtonState.Released;

        if (leftClicked)
        {
            HandleMouseClick(mouseState.X, mouseState.Y);
        }

        _previousMouseState = mouseState;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Gray);

        _spriteBatch.Begin();

        DrawBoard();
        DrawSelectedCell();
        DrawPieces();
        DrawCoordinates();

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void HandleMouseClick(int mouseX, int mouseY)
    {
        if (!TryGetBoardPosition(mouseX, mouseY, out var clickedPosition))
            return;

        var clickedPiece = _game.Board.GetPiece(clickedPosition);

        if (_selectedPosition is null)
        {
            if (clickedPiece is null)
                return;

            if (clickedPiece.Color != _game.SideToMove)
                return;

            _selectedPosition = clickedPosition;
            return;
        }

        var from = _selectedPosition.Value;
        var piece = _game.Board.GetPiece(from);

        if (piece is null)
        {
            _selectedPosition = null;
            return;
        }

        var moves = _moveGenerator.GenerateMovesForPiece(_game.Board, from);
        bool canMove = moves.Any(m => m.To == clickedPosition);

        if (canMove)
        {
            _game.Board.MovePiece(from, clickedPosition);
            _game.SwitchTurn();
        }

        _selectedPosition = null;
    }

    private bool TryGetBoardPosition(int mouseX, int mouseY, out Position position)
    {
        position = default;

        int localX = mouseX - Padding;
        int localY = mouseY - Padding;

        if (localX < 0 || localY < 0)
            return false;

        if (localX >= BoardSize || localY >= BoardSize)
            return false;

        int col = localX / TileSize;
        int row = localY / TileSize;

        position = new Position(row, col);
        return true;
    }

    private void DrawBoard()
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                bool isLight = (row + col) % 2 == 0;

                Color color = isLight
                    ? Color.White
                    : new Color(120, 80, 200);

                var rect = new Rectangle(
                    Padding + col * TileSize,
                    Padding + row * TileSize,
                    TileSize,
                    TileSize
                );

                _spriteBatch.Draw(_pixel, rect, color);
            }
        }
    }

    private void DrawSelectedCell()
    {
        if (_selectedPosition is null)
            return;

        var pos = _selectedPosition.Value;

        var rect = new Rectangle(
            Padding + pos.Col * TileSize,
            Padding + pos.Row * TileSize,
            TileSize,
            TileSize
        );

        _spriteBatch.Draw(_pixel, rect, Color.Yellow * 0.45f);
    }

    private void DrawPieces()
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var position = new Position(row, col);
                var piece = _game.Board.GetPiece(position);

                if (piece is null)
                    continue;

                string symbol = GetPieceSymbol(piece);
                Vector2 textSize = _font.MeasureString(symbol);

                Vector2 drawPosition = new Vector2(
                    Padding + col * TileSize + TileSize / 2f - textSize.X / 2f,
                    Padding + row * TileSize + TileSize / 2f - textSize.Y / 2f
                );

                Color color = piece.Color == PieceColor.White
                    ? Color.White
                    : Color.Black;

                DrawOutlinedText(symbol, drawPosition, color);
            }
        }
    }

    private string GetPieceSymbol(Piece piece)
    {
        return piece.Type switch
        {
            PieceType.King => "K",
            PieceType.Queen => "Q",
            PieceType.Rook => "R",
            PieceType.Bishop => "B",
            PieceType.Knight => "N",
            PieceType.Pawn => "P",
            _ => "?"
        };
    }

    private void DrawCoordinates()
    {
        for (int col = 0; col < 8; col++)
        {
            string letter = ((char)('a' + col)).ToString();
            Vector2 size = _font.MeasureString(letter);

            Vector2 position = new Vector2(
                Padding + col * TileSize + TileSize / 2f - size.X / 2f,
                Padding + BoardSize + 6
            );

            _spriteBatch.DrawString(_font, letter, position, Color.Black);
        }

        for (int row = 0; row < 8; row++)
        {
            string number = (8 - row).ToString();
            Vector2 size = _font.MeasureString(number);

            Vector2 position = new Vector2(
                Padding - 24,
                Padding + row * TileSize + TileSize / 2f - size.Y / 2f
            );

            _spriteBatch.DrawString(_font, number, position, Color.Black);
        }
    }
    private void DrawOutlinedText(string text, Vector2 position, Color color)
    {
        if (color == Color.White)
        {
            // Чёрная обводка
            _spriteBatch.DrawString(_font, text, position + new Vector2(-1, -1), Color.Black);
            _spriteBatch.DrawString(_font, text, position + new Vector2(1, -1), Color.Black);
            _spriteBatch.DrawString(_font, text, position + new Vector2(-1, 1), Color.Black);
            _spriteBatch.DrawString(_font, text, position + new Vector2(1, 1), Color.Black);
        }

        // Основной текст
        _spriteBatch.DrawString(_font, text, position, color);
    }
}