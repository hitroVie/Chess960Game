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
using Chess960Game.Desktop.Animation;
using Chess960Game.Desktop.Rendering;

namespace Chess960Game.Desktop;

public class Game1 : Game
{
    // MonoGame resources
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _pixel;
    private Texture2D _backgroundTexture;
    private SpriteFont _font;

    // Core game state
    private GameState _game;
    private MoveGenerator _moveGenerator;

    private MouseState _previousMouseState;
    private Position? _selectedPosition;
    private List<Move> _selectedMoves = new();

    private string _statusText = "";
    private bool _gameOver = false;

    // Promotion state
    private bool _waitingForPromotion = false;
    private Position? _promotionFrom;
    private Position? _promotionTo;
    private PieceColor? _promotionColor;


    // Board layout
    private readonly BoardRenderer _boardRenderer = new();
    private const int TileSize = 120;
    private const int Padding = 60;
    private const int BoardSize = TileSize * 8;

    private const int PlatformTopHeight = 88;
    private const int PlatformDepth = 28;
    private const int PlatformGap = 6;
    private const int RowStep = 105;

    // Piece rendering
    private readonly PieceRenderer _pieceRenderer = new();
    private const int PieceSize = 90;

    // Castling state
    private bool _whiteKingMoved = false;
    private bool _blackKingMoved = false;

    private bool _whiteKingsideRookMoved = false;
    private bool _whiteQueensideRookMoved = false;

    private bool _blackKingsideRookMoved = false;
    private bool _blackQueensideRookMoved = false;

    // Bot
    private SimpleChessBot _bot;
    private int _fullMoveNumber = 1;
    private PieceColor _botColor;
    private PieceColor _playerColor;
    private readonly Random _random = new();

    private bool _botMovePending = false;
    private float _botMoveDelayTimer = 0f;
    private const float BotMoveDelay = 0.18f;

    // Animation
    private readonly MoveAnimation _moveAnimation = new();

    // Shockwave animation
    private readonly ShockwaveEffect _shockwave = new();

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
        _pieceRenderer.LoadContent(Content);
    }
 
    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        UpdateMoveAnimation(gameTime);
        UpdateShockwave(gameTime);
        UpdateBotMoveDelay(gameTime);

        if (_waitingForPromotion)
        {
            HandlePromotionKeyboardInput();
            base.Update(gameTime);
            return;
        }

        if (_moveAnimation.IsActive)
        {
            _previousMouseState = Mouse.GetState();

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
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin();

        DrawBackground();

        _boardRenderer.DrawBoard(_spriteBatch, _pixel, GetShockwaveOffset);
        _boardRenderer.DrawCoordinates(_spriteBatch, _font, _playerColor);
        _boardRenderer.DrawSelectedCell(
            _spriteBatch,
            _pixel,
            _selectedPosition,
            ToScreenRow,
            ToScreenCol
        );
        _boardRenderer.DrawAvailableMoves(
            _spriteBatch,
            _pixel,
            _selectedMoves,
            ToScreenRow,
            ToScreenCol
        );
        _pieceRenderer.DrawPieces(
            _spriteBatch,
            _game.Board,
            PieceSize,
            GetPieceDrawPosition,
            _moveAnimation.IsActive,
            _moveAnimation.To,
            _moveAnimation.Piece
        );
        DrawMoveAnimation();
        DrawStatus();


        _spriteBatch.End();

        base.Draw(gameTime);
    }

    //Input
    private void HandleMouseClick(int mouseX, int mouseY)
    {
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
    
    //Promotion
    private void HandlePromotionKeyboardInput()
    {
        var keyboard = Keyboard.GetState();

        if (keyboard.IsKeyDown(Keys.Q))
            CompletePromotion(PieceType.Queen);

        if (keyboard.IsKeyDown(Keys.R))
            CompletePromotion(PieceType.Rook);

        if (keyboard.IsKeyDown(Keys.B))
            CompletePromotion(PieceType.Bishop);

        if (keyboard.IsKeyDown(Keys.N))
            CompletePromotion(PieceType.Knight);
    }
    private void CompletePromotion(PieceType pieceType)
    {
        if (_promotionFrom is null || _promotionTo is null || _promotionColor is null)
            return;

        _game.Board.SetPiece(_promotionFrom.Value, null);

        _game.Board.SetPiece(
            _promotionTo.Value,
            new Piece(pieceType, _promotionColor.Value)
        );

        _waitingForPromotion = false;
        _promotionFrom = null;
        _promotionTo = null;
        _promotionColor = null;

        _game.SwitchTurn();
        UpdateGameStatus();

        ScheduleBotMove();
    }

    //Game status
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

    //Drawing
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

    private void DrawStatus()
    {
        string text = _waitingForPromotion
            ? "Promotion: Q Queen | R Rook | B Bishop | N Knight"
            : _statusText;

        if (string.IsNullOrWhiteSpace(text))
            return;

        float scale = 0.6f;

        Vector2 textSize = _font.MeasureString(text) * scale;

        Vector2 position = new Vector2(
            _graphics.PreferredBackBufferWidth / 2f - textSize.X / 2f,
            _graphics.PreferredBackBufferHeight - 90
        );

        _spriteBatch.DrawString(
            _font,
            text,
            position,
            Color.White,
            0f,
            Vector2.Zero,
            scale,
            SpriteEffects.None,
            0f
        );
    }


    //Castling
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

    //Bot
    private void ScheduleBotMove()
    {
        _botMovePending = true;
        _botMoveDelayTimer = 0f;
    }
    private void UpdateBotMoveDelay(GameTime gameTime)
    {
        if (!_botMovePending)
            return;

        if (_moveAnimation.IsActive)
            return;

        _botMoveDelayTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_botMoveDelayTimer >= BotMoveDelay)
        {
            _botMovePending = false;
            _botMoveDelayTimer = 0f;

            TryMakeBotMove();
        }
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

    //Animation
    private void StartMoveAnimation(Piece piece, Position from, Position to, MoveAnimationType type = MoveAnimationType.Normal)
    {
        _moveAnimation.Start(piece, from, to, type);
    }
    private void UpdateMoveAnimation(GameTime gameTime)
    {
        bool finishedAttack = _moveAnimation.Update(
            (float)gameTime.ElapsedGameTime.TotalSeconds
        );

        if (finishedAttack)
        {
            StartShockwave(_moveAnimation.To);
        }
    }
    private void DrawMoveAnimation()
    {
        if (!_moveAnimation.IsActive || _moveAnimation.Piece is null)
            return;

        if (!_pieceRenderer.TryGetTexture(_moveAnimation.Piece, out var texture))
            return;

        int baseSize = PieceSize;

        Vector2 from = GetPieceDrawPosition(_moveAnimation.From, baseSize);
        Vector2 to = GetPieceDrawPosition(_moveAnimation.To, baseSize);

        float t = SmoothStep(_moveAnimation.Progress);
        Vector2 position = Vector2.Lerp(from, to, t);

        float scale;

        if (_moveAnimation.Type == MoveAnimationType.Attack)
        {
            float arc = MathF.Sin(t * MathF.PI) * 180f;
            position.Y -= arc;

            scale = 1f + MathF.Sin(t * MathF.PI) * 0.45f;
        }
        else
        {
            float arc = MathF.Sin(t * MathF.PI) * _moveAnimation.ArcHeight;
            position.Y -= arc;

            scale = 1f + MathF.Sin(t * MathF.PI) * _moveAnimation.ScaleBoost;
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
    private void StartShockwave(Position center)
    {
        _shockwave.Start(center);
    }
    private void UpdateShockwave(GameTime gameTime)
    {
        _shockwave.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
    }
    private int GetShockwaveOffset(Position position)
    {
        return _shockwave.GetOffset(
            position,
            ToScreenRow,
            ToScreenCol
        );
    }
    private float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    //Coordinate helpers
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
}