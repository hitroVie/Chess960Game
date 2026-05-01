using Chess960Game.Domain.Board;
using Chess960Game.Domain.Game;
using Chess960Game.Domain.Moves;
using Chess960Game.Domain.Pieces;
using Chess960Game.Domain.Setup;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

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
    private List<Move> _selectedMoves = new();

    private bool _waitingForPromotion = false;
    private Position? _promotionFrom;
    private Position? _promotionTo;
    private PieceColor? _promotionColor;

    private readonly List<(Rectangle Rect, PieceType Type, string Label)> _promotionButtons = new();

    private string _statusText = "";
    private bool _gameOver = false;

    private const int TileSize = 80;
    private const int Padding = 40;
    private const int BoardSize = TileSize * 8;

    private bool _whiteKingMoved = false;
    private bool _blackKingMoved = false;

    private bool _whiteKingsideRookMoved = false;
    private bool _whiteQueensideRookMoved = false;

    private bool _blackKingsideRookMoved = false;
    private bool _blackQueensideRookMoved = false;

    private PieceColor _botColor;
    private PieceColor _playerColor;
    private readonly Random _random = new();
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

        _playerColor = _random.Next(2) == 0
            ? PieceColor.White
            : PieceColor.Black;
        _botColor = _playerColor == PieceColor.White
            ? PieceColor.Black
            : PieceColor.White;

        UpdateGameStatus();
        CreatePromotionButtons();
        TryMakeBotMove();
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
        DrawAvailableMoves();
        DrawPieces();
        DrawCoordinates();
        DrawStatus();
        DrawPromotionButtons();

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void HandleMouseClick(int mouseX, int mouseY)
    {
        if (_waitingForPromotion)
        {
            HandlePromotionButtonClick(mouseX, mouseY);
            return;
        }

        if (_gameOver)
            return;

        if (!TryGetBoardPosition(mouseX, mouseY, out var clickedPosition))
            return;

        var clickedPiece = _game.Board.GetPiece(clickedPosition);

        if (_selectedPosition is null)
        {
            if (clickedPiece is null)
                return;

            if (clickedPiece.Color != _game.SideToMove)
                return;
            if (clickedPiece.Color != _playerColor)
                return;

            _selectedPosition = clickedPosition;
            _selectedMoves = _moveGenerator.GenerateLegalMovesForPiece(_game.Board, clickedPosition);
            return;
        }

        var from = _selectedPosition.Value;
        var piece = _game.Board.GetPiece(from);

        if (piece is null)
        {
            _selectedPosition = null;
            _selectedMoves.Clear();
            return;
        }

        // Рокировка: выбрали ладью, потом кликнули по своему королю
        if (TryCastleByRookClick(from, clickedPosition))
        {
            _selectedPosition = null;
            _selectedMoves.Clear();
            return;
        }

        var moves = _moveGenerator.GenerateLegalMovesForPiece(_game.Board, from);
        bool canMove = moves.Any(m => m.To == clickedPosition);

        if (canMove)
        {
            var selectedMove = moves.First(m => m.To == clickedPosition);

            if (selectedMove.Promotion is not null)
            {
                var movingPiece = _game.Board.GetPiece(from);

                _waitingForPromotion = true;
                _promotionFrom = from;
                _promotionTo = clickedPosition;
                _promotionColor = movingPiece!.Color;

                _selectedPosition = null;
                _selectedMoves.Clear();

                return;
            }

            MarkPieceMoved(from, piece);

            _game.Board.MovePiece(from, clickedPosition);
            _game.SwitchTurn();
            UpdateGameStatus();

            TryMakeBotMove();
        }

        _selectedPosition = null;
        _selectedMoves.Clear();
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

        int screenCol = localX / TileSize;
        int screenRow = localY / TileSize;

        position = ToBoardPosition(screenRow, screenCol);
        return true;
    }

    private void UpdateGameStatus()
    {
        var side = _game.SideToMove;

        bool isInCheck = _moveGenerator.IsKingInCheck(_game.Board, side);
        var legalMoves = _moveGenerator.GenerateAllLegalMoves(_game.Board, side);

        if (isInCheck && legalMoves.Count == 0)
        {
            var winner = side == PieceColor.White
                ? "Black"
                : "White";

            _statusText = $"Checkmate! {winner} wins.";
            _gameOver = true;
            return;
        }

        if (!isInCheck && legalMoves.Count == 0)
        {
            _statusText = "Stalemate! Draw.";
            _gameOver = true;
            return;
        }

        _statusText = "";
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
            Padding + ToScreenCol(pos.Col) * TileSize,
            Padding + ToScreenRow(pos.Row) * TileSize,
            TileSize,
            TileSize
        );

        _spriteBatch.Draw(_pixel, rect, Color.Yellow);
    }

    private void DrawAvailableMoves()
    {
        foreach (var move in _selectedMoves)
        {
            var pos = move.To;

            int centerX = Padding + ToScreenCol(pos.Col) * TileSize + TileSize / 2;
            int centerY = Padding + ToScreenRow(pos.Row) * TileSize + TileSize / 2;

            DrawCircle(
                centerX,
                centerY,
                10,
                new Color(60, 60, 60)
            );
        }
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

                int screenRow = ToScreenRow(row);
                int screenCol = ToScreenCol(col);

                Vector2 drawPosition = new Vector2(
                    Padding + screenCol * TileSize + TileSize / 2f - textSize.X / 2f,
                    Padding + screenRow * TileSize + TileSize / 2f - textSize.Y / 2f
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
            string letter = _playerColor == PieceColor.White
                ? ((char)('a' + col)).ToString()
                : ((char)('h' - col)).ToString();
            Vector2 size = _font.MeasureString(letter);

            Vector2 position = new Vector2(
                Padding + col * TileSize + TileSize / 2f - size.X / 2f,
                Padding + BoardSize + 6
            );

            _spriteBatch.DrawString(_font, letter, position, Color.Black);
        }

        for (int row = 0; row < 8; row++)
        {
            string number = _playerColor == PieceColor.White
                ? (8 - row).ToString()
                : (row + 1).ToString();
            Vector2 size = _font.MeasureString(number);

            Vector2 position = new Vector2(
                Padding - 24,
                Padding + row * TileSize + TileSize / 2f - size.Y / 2f
            );

            _spriteBatch.DrawString(_font, number, position, Color.Black);
        }
    }

    private void DrawStatus()
    {
        if (string.IsNullOrWhiteSpace(_statusText))
            return;

        Vector2 position = new Vector2(Padding, 8);

        _spriteBatch.DrawString(
            _font,
            _statusText,
            position,
            Color.Black
        );
    }

    private void DrawOutlinedText(string text, Vector2 position, Color color)
    {
        if (color == Color.White)
        {
            _spriteBatch.DrawString(_font, text, position + new Vector2(-1, -1), Color.Black);
            _spriteBatch.DrawString(_font, text, position + new Vector2(1, -1), Color.Black);
            _spriteBatch.DrawString(_font, text, position + new Vector2(-1, 1), Color.Black);
            _spriteBatch.DrawString(_font, text, position + new Vector2(1, 1), Color.Black);
        }

        _spriteBatch.DrawString(_font, text, position, color);
    }

    private void DrawCircle(int centerX, int centerY, int radius, Color color)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x * x + y * y <= radius * radius)
                {
                    _spriteBatch.Draw(
                        _pixel,
                        new Rectangle(centerX + x, centerY + y, 1, 1),
                        color
                    );
                }
            }
        }
    }
    private void CreatePromotionButtons()
    {
        _promotionButtons.Clear();

        int x = Padding;
        int y = 8;
        int width = 90;
        int height = 28;
        int gap = 10;

        _promotionButtons.Add((new Rectangle(x, y, width, height), PieceType.Queen, "Queen"));
        _promotionButtons.Add((new Rectangle(x + (width + gap), y, width, height), PieceType.Rook, "Rook"));
        _promotionButtons.Add((new Rectangle(x + (width + gap) * 2, y, width, height), PieceType.Bishop, "Bishop"));
        _promotionButtons.Add((new Rectangle(x + (width + gap) * 3, y, width, height), PieceType.Knight, "Knight"));
    }
    private void HandlePromotionButtonClick(int mouseX, int mouseY)
    {
        foreach (var button in _promotionButtons)
        {
            if (!button.Rect.Contains(mouseX, mouseY))
                continue;

            _game.Board.SetPiece(_promotionFrom!.Value, null);
            _game.Board.SetPiece(
                _promotionTo!.Value,
                new Piece(button.Type, _promotionColor!.Value)
            );

            _waitingForPromotion = false;
            _promotionFrom = null;
            _promotionTo = null;
            _promotionColor = null;
            _statusText = "";

            _game.SwitchTurn();
            UpdateGameStatus();

            TryMakeBotMove();

            return;
        }
    }
    private void DrawPromotionButtons()
    {
        if (!_waitingForPromotion)
            return;

        foreach (var button in _promotionButtons)
        {
            _spriteBatch.Draw(_pixel, button.Rect, Color.White);

            DrawRectangleBorder(button.Rect, Color.Black, 2);

            Vector2 textSize = _font.MeasureString(button.Label);

            Vector2 textPosition = new Vector2(
                button.Rect.X + button.Rect.Width / 2f - textSize.X / 2f,
                button.Rect.Y + button.Rect.Height / 2f - textSize.Y / 2f
            );

            _spriteBatch.DrawString(_font, button.Label, textPosition, Color.Black);
        }
    }
    private void DrawRectangleBorder(Rectangle rect, Color color, int thickness)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
    private bool TryCastleByRookClick(Position rookPosition, Position kingPosition)
    {
        var rook = _game.Board.GetPiece(rookPosition);
        var king = _game.Board.GetPiece(kingPosition);

        if (rook is null || king is null)
            return false;

        if (rook.Type != PieceType.Rook)
            return false;

        if (king.Type != PieceType.King)
            return false;

        if (rook.Color != king.Color)
            return false;

        if (rook.Color != _game.SideToMove)
            return false;

        var color = rook.Color;
        bool isKingside = rookPosition.Col > kingPosition.Col;

        if (!HasCastlingRight(color, isKingside))
            return false;

        var expectedKingStart = color == PieceColor.White
            ? _game.WhiteKingStart
            : _game.BlackKingStart;

        var expectedRookStart = GetExpectedRookStart(color, isKingside);

        if (kingPosition != expectedKingStart)
            return false;

        if (rookPosition != expectedRookStart)
            return false;

        if (_moveGenerator.IsKingInCheck(_game.Board, color))
            return false;

        int row = color == PieceColor.White ? 7 : 0;

        var finalKingPosition = new Position(row, isKingside ? 6 : 2);
        var finalRookPosition = new Position(row, isKingside ? 5 : 3);

        if (!IsCastlingPathClear(kingPosition, finalKingPosition, kingPosition, rookPosition))
            return false;

        if (!IsCastlingPathClear(rookPosition, finalRookPosition, kingPosition, rookPosition))
            return false;

        foreach (var pos in GetPositionsBetweenInclusive(kingPosition, finalKingPosition))
        {
            if (!IsKingSafeOnCastlingSquare(kingPosition, rookPosition, pos, color))
                return false;
        }

        _game.Board.SetPiece(kingPosition, null);
        _game.Board.SetPiece(rookPosition, null);

        _game.Board.SetPiece(finalKingPosition, new Piece(PieceType.King, color));
        _game.Board.SetPiece(finalRookPosition, new Piece(PieceType.Rook, color));

        MarkKingMoved(color);
        MarkRookMoved(color, isKingside);

        _game.SwitchTurn();
        UpdateGameStatus();

        return true;
    }

    private bool HasCastlingRight(PieceColor color, bool isKingside)
    {
        if (color == PieceColor.White)
        {
            if (_whiteKingMoved)
                return false;

            return isKingside
                ? !_whiteKingsideRookMoved
                : !_whiteQueensideRookMoved;
        }

        if (_blackKingMoved)
            return false;

        return isKingside
            ? !_blackKingsideRookMoved
            : !_blackQueensideRookMoved;
    }

    private Position GetExpectedRookStart(PieceColor color, bool isKingside)
    {
        if (color == PieceColor.White)
        {
            return isKingside
                ? _game.WhiteKingsideRookStart
                : _game.WhiteQueensideRookStart;
        }

        return isKingside
            ? _game.BlackKingsideRookStart
            : _game.BlackQueensideRookStart;
    }

    private void MarkPieceMoved(Position from, Piece piece)
    {
        if (piece.Type == PieceType.King)
        {
            MarkKingMoved(piece.Color);
            return;
        }

        if (piece.Type != PieceType.Rook)
            return;

        if (piece.Color == PieceColor.White)
        {
            if (from == _game.WhiteKingsideRookStart)
                _whiteKingsideRookMoved = true;

            if (from == _game.WhiteQueensideRookStart)
                _whiteQueensideRookMoved = true;
        }
        else
        {
            if (from == _game.BlackKingsideRookStart)
                _blackKingsideRookMoved = true;

            if (from == _game.BlackQueensideRookStart)
                _blackQueensideRookMoved = true;
        }
    }

    private void MarkKingMoved(PieceColor color)
    {
        if (color == PieceColor.White)
            _whiteKingMoved = true;
        else
            _blackKingMoved = true;
    }

    private void MarkRookMoved(PieceColor color, bool isKingside)
    {
        if (color == PieceColor.White)
        {
            if (isKingside)
                _whiteKingsideRookMoved = true;
            else
                _whiteQueensideRookMoved = true;
        }
        else
        {
            if (isKingside)
                _blackKingsideRookMoved = true;
            else
                _blackQueensideRookMoved = true;
        }
    }

    private bool IsCastlingPathClear(
        Position from,
        Position to,
        Position kingStart,
        Position rookStart)
    {
        foreach (var pos in GetPositionsBetweenInclusive(from, to))
        {
            if (pos == kingStart || pos == rookStart)
                continue;

            if (_game.Board.GetPiece(pos) is not null)
                return false;
        }

        return true;
    }

    private IEnumerable<Position> GetPositionsBetweenInclusive(Position from, Position to)
    {
        int rowStep = Math.Sign(to.Row - from.Row);
        int colStep = Math.Sign(to.Col - from.Col);

        int row = from.Row;
        int col = from.Col;

        while (true)
        {
            yield return new Position(row, col);

            if (row == to.Row && col == to.Col)
                break;

            row += rowStep;
            col += colStep;
        }
    }

    private bool IsKingSafeOnCastlingSquare(
        Position kingStart,
        Position rookStart,
        Position testPosition,
        PieceColor color)
    {
        var boardCopy = _game.Board.Clone();

        boardCopy.SetPiece(kingStart, null);
        boardCopy.SetPiece(rookStart, null);
        boardCopy.SetPiece(testPosition, new Piece(PieceType.King, color));

        return !_moveGenerator.IsKingInCheck(boardCopy, color);
    }

    private int ToScreenRow(int boardRow)
    {
        return _playerColor == PieceColor.White
            ? boardRow
            : 7 - boardRow;
    }

    private int ToScreenCol(int boardCol)
    {
        return _playerColor == PieceColor.White
            ? boardCol
            : 7 - boardCol;
    }

    private Position ToBoardPosition(int screenRow, int screenCol)
    {
        if (_playerColor == PieceColor.White)
            return new Position(screenRow, screenCol);

        return new Position(7 - screenRow, 7 - screenCol);
    }

    private void TryMakeBotMove()
    {
        if (_gameOver)
            return;

        if (_waitingForPromotion)
            return;

        if (_game.SideToMove != _botColor)
            return;

        var legalMoves = _moveGenerator.GenerateAllLegalMoves(_game.Board, _botColor);

        if (legalMoves.Count == 0)
        {
            UpdateGameStatus();
            return;
        }

        var move = legalMoves[_random.Next(legalMoves.Count)];

        var piece = _game.Board.GetPiece(move.From);

        if (piece is null)
            return;

        MarkPieceMoved(move.From, piece);

        if (move.Promotion is not null)
        {
            _game.Board.SetPiece(move.From, null);
            _game.Board.SetPiece(
                move.To,
                new Piece(PieceType.Queen, piece.Color)
            );
        }
        else
        {
            _game.Board.MovePiece(move.From, move.To);
        }

        _game.SwitchTurn();
        UpdateGameStatus();
    }
}