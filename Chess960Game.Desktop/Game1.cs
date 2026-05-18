#nullable enable
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
using Chess960Game.Domain.Bot;

namespace Chess960Game.Desktop;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _pixel;
    private Texture2D _backgroundTexture;
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

    private const int TileSize = 120;
    private const int Padding = 60;
    private const int BoardSize = TileSize * 8;
    private const int PlatformTopHeight = 88;
    private const int PlatformDepth = 28;
    private const int PlatformGap = 6;
    private const int RowStep = 105;

    private bool _whiteKingMoved = false;
    private bool _blackKingMoved = false;

    private bool _whiteKingsideRookMoved = false;
    private bool _whiteQueensideRookMoved = false;

    private bool _blackKingsideRookMoved = false;
    private bool _blackQueensideRookMoved = false;

    private SimpleChessBot _bot;
    private int _fullMoveNumber = 1;

    private PieceColor _botColor;
    private PieceColor _playerColor;
    private readonly Random _random = new();

    private readonly Dictionary<string, Texture2D> _pieceTextures = new();
    private enum MoveAnimationType
    {
        Normal,
        Attack
    }

    private MoveAnimationType _animationType = MoveAnimationType.Normal;

    private bool _isAnimatingMove = false;
    private Piece? _animatedPiece;
    private Position _animationFrom;
    private Position _animationTo;
    private float _animationProgress = 0f;

    private float _animationArcHeight;
    private float _animationScaleBoost;
    private const float NormalMoveAnimationDuration = 0.28f;
    private const float AttackMoveAnimationDuration = 0.65f;
    private bool _isShockwaveActive = false;
    private Position _shockwaveCenter;
    private float _shockwaveTimer = 0f;
    private const float ShockwaveDuration = 0.7f;
    private bool _botMovePending = false;
    private float _botMoveDelayTimer = 0f;
    private const float BotMoveDelay = 0.18f;
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
        _bot = new SimpleChessBot(_moveGenerator);

        _playerColor = _random.Next(2) == 0
            ? PieceColor.White
            : PieceColor.Black;
        _botColor = _playerColor == PieceColor.White
            ? PieceColor.Black
            : PieceColor.White;

        UpdateGameStatus();
        CreatePromotionButtons();
        if (_botColor == PieceColor.White)
        {
            ScheduleBotMove();
        }
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _backgroundTexture = Content.Load<Texture2D>("Backgrounds/background");
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _font = Content.Load<SpriteFont>("DefaultFont");
        LoadPieceTextures();
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        UpdateMoveAnimation(gameTime);
        UpdateShockwave(gameTime);
        UpdateBotMoveDelay(gameTime);

        if (_isAnimatingMove)
        {
            base.Update(gameTime);
            return;
        }

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
    private void LoadPieceTextures()
    {
        _pieceTextures["White_King"] = Content.Load<Texture2D>("Pieces/white_king");
        _pieceTextures["White_Queen"] = Content.Load<Texture2D>("Pieces/white_queen");
        _pieceTextures["White_Rook"] = Content.Load<Texture2D>("Pieces/white_rook");
        _pieceTextures["White_Bishop"] = Content.Load<Texture2D>("Pieces/white_bishop");
        _pieceTextures["White_Knight"] = Content.Load<Texture2D>("Pieces/white_knight");
        _pieceTextures["White_Pawn"] = Content.Load<Texture2D>("Pieces/white_pawn");

        _pieceTextures["Black_King"] = Content.Load<Texture2D>("Pieces/black_king");
        _pieceTextures["Black_Queen"] = Content.Load<Texture2D>("Pieces/black_queen");
        _pieceTextures["Black_Rook"] = Content.Load<Texture2D>("Pieces/black_rook");
        _pieceTextures["Black_Bishop"] = Content.Load<Texture2D>("Pieces/black_bishop");
        _pieceTextures["Black_Knight"] = Content.Load<Texture2D>("Pieces/black_knight");
        _pieceTextures["Black_Pawn"] = Content.Load<Texture2D>("Pieces/black_pawn");
    }
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin();

        DrawBackground();

        DrawBoard();
        DrawSelectedCell();
        DrawAvailableMoves();
        DrawPieces();
        DrawMoveAnimation();
        DrawStatus();
        DrawPromotionButtons();

        _spriteBatch.End();

        base.Draw(gameTime);
    }
    private void DrawBackground()
    {
        var rect = new Rectangle(
            0,
            0,
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight
        );

        _spriteBatch.Draw(_backgroundTexture, rect, Color.White);
    }
    private void UpdateShockwave(GameTime gameTime)
    {
        if (!_isShockwaveActive)
            return;

        _shockwaveTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_shockwaveTimer >= ShockwaveDuration)
        {
            _isShockwaveActive = false;
            _shockwaveTimer = 0f;
        }
    }
    private void StartShockwave(Position center)
    {
        _isShockwaveActive = true;
        _shockwaveCenter = center;
        _shockwaveTimer = 0f;
    }
    private void ScheduleBotMove()
    {
        _botMovePending = true;
        _botMoveDelayTimer = 0f;
    }

    private void UpdateBotMoveDelay(GameTime gameTime)
    {
        if (!_botMovePending)
            return;

        if (_isAnimatingMove)
            return;

        _botMoveDelayTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_botMoveDelayTimer >= BotMoveDelay)
        {
            _botMovePending = false;
            _botMoveDelayTimer = 0f;

            TryMakeBotMove();
        }
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

            ScheduleBotMove();

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

            var boardCopy = _game.Board.Clone();
            boardCopy.MovePiece(from, clickedPosition);

            var enemyColor = piece.Color == PieceColor.White
                ? PieceColor.Black
                : PieceColor.White;

            bool givesCheck = _moveGenerator.IsKingInCheck(boardCopy, enemyColor);

            var animationType = givesCheck
                ? MoveAnimationType.Attack
                : MoveAnimationType.Normal;

            MarkPieceMoved(from, piece);

            _game.Board.MovePiece(from, clickedPosition);

            StartMoveAnimation(
                piece,
                from,
                clickedPosition,
                animationType
            );

            _game.SwitchTurn();
            UpdateGameStatus();

            ScheduleBotMove();
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

        int screenCol = localX / TileSize;
        int screenRow = localY / RowStep;

        if (screenCol < 0 || screenCol >= 8)
            return false;

        if (screenRow < 0 || screenRow >= 8)
            return false;

        int cellX = localX - screenCol * TileSize;
        int cellY = localY - screenRow * RowStep;

        bool insidePlatform =
            cellX >= PlatformGap / 2 &&
            cellX <= TileSize - PlatformGap / 2 &&
            cellY >= PlatformGap / 2 &&
            cellY <= PlatformGap / 2 + PlatformTopHeight + PlatformDepth;

        if (!insidePlatform)
            return false;

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

                Color topColor = isLight
                    ? new Color(235, 230, 245)
                    : new Color(120, 95, 170);

                Color sideColor = isLight
                    ? new Color(190, 185, 205)
                    : new Color(75, 45, 130);

                int x = Padding + col * TileSize;
                int y = Padding + row * RowStep;

                int waveOffset = GetShockwaveOffset(new Position(row, col));
                y += waveOffset;

                DrawPlatformTile(x, y, topColor, sideColor);
            }
        }
    }
    private int GetShockwaveOffset(Position position)
    {
        if (!_isShockwaveActive)
            return 0;

        int screenRow = ToScreenRow(position.Row);
        int screenCol = ToScreenCol(position.Col);

        int centerRow = ToScreenRow(_shockwaveCenter.Row);
        int centerCol = ToScreenCol(_shockwaveCenter.Col);

        float distance = MathF.Sqrt(
            MathF.Pow(screenRow - centerRow, 2) +
            MathF.Pow(screenCol - centerCol, 2)
        );

        float time = _shockwaveTimer / ShockwaveDuration;

        float waveRadius = time * 6f;
        float waveWidth = 0.9f;

        float delta = MathF.Abs(distance - waveRadius);

        if (delta > waveWidth)
            return 0;

        float strength = 1f - delta / waveWidth;
        float fade = 1f - time;

        return (int)(-MathF.Sin(strength * MathF.PI) * 16f * fade);
    }
    private void DrawPlatformTile(int x, int y, Color topColor, Color sideColor)
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

        _spriteBatch.Draw(_pixel, topRect, topColor);
        _spriteBatch.Draw(_pixel, sideRect, sideColor);

        DrawRectangleBorder(topRect, new Color(25, 25, 35), 2);
        DrawRectangleBorder(sideRect, new Color(25, 25, 35), 2);
    }

    private void DrawSelectedCell()
    {
        if (_selectedPosition is null)
            return;

        var pos = _selectedPosition.Value;

        int x = Padding + ToScreenCol(pos.Col) * TileSize + PlatformGap / 2;
        int y = Padding + ToScreenRow(pos.Row) * RowStep + PlatformGap / 2;

        var rect = new Rectangle(
            x,
            y,
            TileSize - PlatformGap,
            PlatformTopHeight
        );

        _spriteBatch.Draw(_pixel, rect, Color.Yellow);
    }

    private void DrawAvailableMoves()
    {
        foreach (var move in _selectedMoves)
        {
            var pos = move.To;

            int centerX =
                Padding + ToScreenCol(pos.Col) * TileSize + TileSize / 2;

            int centerY =
                Padding + ToScreenRow(pos.Row) * RowStep + PlatformTopHeight / 2;

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

                if (_isAnimatingMove &&
                    _animatedPiece is not null &&
                    position == _animationTo)
                {
                    continue;
                }

                string key = $"{piece.Color}_{piece.Type}";

                if (!_pieceTextures.TryGetValue(key, out var texture))
                    continue;

                int pieceSize = 90;
                Vector2 drawPosition = GetPieceDrawPosition(position, pieceSize);

                var destination = new Rectangle(
                    (int)drawPosition.X,
                    (int)drawPosition.Y,
                    pieceSize,
                    pieceSize
                );

                _spriteBatch.Draw(texture, destination, Color.White);
            }
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

            ScheduleBotMove();

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
    private void StartMoveAnimation(Piece piece, Position from, Position to, MoveAnimationType type = MoveAnimationType.Normal)
    {
        _isAnimatingMove = true;
        _animatedPiece = piece;
        _animationFrom = from;
        _animationTo = to;
        _animationProgress = 0f;
        _animationType = type;

        if (type == MoveAnimationType.Attack)
        {
            _animationArcHeight = 180;
            _animationScaleBoost = 0.45f;
        }
        else
        {
            _animationArcHeight = 65;
            _animationScaleBoost = 0.05f;
        }
    }
    private Vector2 GetPieceDrawPosition(Position position, int pieceSize)
    {
        int screenRow = ToScreenRow(position.Row);
        int screenCol = ToScreenCol(position.Col);

        int pieceX = Padding + screenCol * TileSize + (TileSize - pieceSize) / 2;

        int waveOffset = GetShockwaveOffset(position);

        int pieceBottomY =
            Padding +
            screenRow * RowStep +
            PlatformGap / 2 +
            PlatformTopHeight / 2 +
            18 +
            waveOffset;

        int pieceY = pieceBottomY - pieceSize;

        return new Vector2(pieceX, pieceY);
    }

    private void UpdateMoveAnimation(GameTime gameTime)
    {
        if (!_isAnimatingMove)
            return;

        float duration = _animationType == MoveAnimationType.Attack
            ? AttackMoveAnimationDuration
            : NormalMoveAnimationDuration;

        _animationProgress += (float)gameTime.ElapsedGameTime.TotalSeconds / duration;

        if (_animationProgress >= 1f)
        {
            _animationProgress = 1f;

            if (_animationType == MoveAnimationType.Attack)
            {
                StartShockwave(_animationTo);
            }

            _isAnimatingMove = false;
            _animatedPiece = null;
        }
    }
    private void DrawMoveAnimation()
    {
        if (!_isAnimatingMove || _animatedPiece is null)
            return;

        string key = $"{_animatedPiece.Color}_{_animatedPiece.Type}";

        if (!_pieceTextures.TryGetValue(key, out var texture))
            return;

        int baseSize = 90;

        Vector2 from = GetPieceDrawPosition(_animationFrom, baseSize);
        Vector2 to = GetPieceDrawPosition(_animationTo, baseSize);

        float t = SmoothStep(_animationProgress);
        Vector2 position = Vector2.Lerp(from, to, t);

        float scale;

        if (_animationType == MoveAnimationType.Attack)
        {
            float arc = MathF.Sin(t * MathF.PI) * 180f;
            position.Y -= arc;

            scale = 1f + MathF.Sin(t * MathF.PI) * 0.45f;
        }
        else
        {
            float arc = MathF.Sin(t * MathF.PI) * _animationArcHeight;
            position.Y -= arc;

            scale = 1f + MathF.Sin(t * MathF.PI) * _animationScaleBoost;
        }


        int size = (int)(baseSize * scale);

        var destination = new Rectangle(
            (int)(position.X - (size - baseSize) / 2f),
            (int)(position.Y - (size - baseSize) / 2f),
            size,
            size
        );

        _spriteBatch.Draw(texture, destination, Color.White);
    }

    private float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }
    private void TryMakeBotMove()
    {
        if (_gameOver)
            return;

        if (_waitingForPromotion)
            return;

        if (_game.SideToMove != _botColor)
            return;

        var move = _bot.ChooseMove(_game.Board, _botColor, _fullMoveNumber);

        if (move is null)
        {
            UpdateGameStatus();
            return;
        }

        var selectedMove = move.Value;

        var piece = _game.Board.GetPiece(selectedMove.From);

        if (piece is null)
            return;

        var boardCopy = _game.Board.Clone();

        if (selectedMove.Promotion is not null)
        {
            boardCopy.SetPiece(selectedMove.From, null);
            boardCopy.SetPiece(
                selectedMove.To,
                new Piece(PieceType.Queen, piece.Color)
            );
        }
        else
        {
            boardCopy.MovePiece(selectedMove.From, selectedMove.To);
        }

        var enemyColor = piece.Color == PieceColor.White
            ? PieceColor.Black
            : PieceColor.White;

        bool givesCheck = _moveGenerator.IsKingInCheck(boardCopy, enemyColor);

        var animationType = givesCheck
            ? MoveAnimationType.Attack
            : MoveAnimationType.Normal;

        MarkPieceMoved(selectedMove.From, piece);

        if (selectedMove.Promotion is not null)
        {
            _game.Board.SetPiece(selectedMove.From, null);
            _game.Board.SetPiece(
                selectedMove.To,
                new Piece(PieceType.Queen, piece.Color)
            );

            StartMoveAnimation(
                new Piece(PieceType.Queen, piece.Color),
                selectedMove.From,
                selectedMove.To,
                animationType
            );
        }
        else
        {
            _game.Board.MovePiece(selectedMove.From, selectedMove.To);

            StartMoveAnimation(
                piece,
                selectedMove.From,
                selectedMove.To,
                animationType
            );
        }

        _game.SwitchTurn();
        UpdateGameStatus();

        _fullMoveNumber++;
    }

}