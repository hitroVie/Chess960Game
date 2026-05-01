using Chess960Game.Domain.Board;
using Chess960Game.Domain.Moves;
using Chess960Game.Domain.Setup;

var setupGenerator = new Chess960SetupGenerator();
var game = setupGenerator.CreateNewGame();
var moveGenerator = new MoveGenerator();

while (true)
{
    Console.Clear();

    Console.WriteLine("Chess960 Console");
    Console.WriteLine();
    Console.WriteLine(game.Board.ToPrettyString());
    Console.WriteLine();
    Console.WriteLine($"Ходят: {game.SideToMove}");
    Console.WriteLine("Введите ход, например: e2 e4");
    Console.WriteLine("Команды: moves e2, exit");
    Console.Write("> ");

    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    input = input.Trim().ToLower();

    if (input == "exit")
        break;

    if (input.StartsWith("moves "))
    {
        var posText = input.Replace("moves ", "").Trim();

        if (!TryParsePosition(posText, out var pos))
        {
            ShowMessage("Неверная клетка.");
            continue;
        }

        ShowMovesFrom(pos);
        continue;
    }

    var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    if (parts.Length != 2)
    {
        ShowMessage("Нужно ввести ход в формате: e2 e4");
        continue;
    }

    if (!TryParsePosition(parts[0], out var from) ||
        !TryParsePosition(parts[1], out var to))
    {
        ShowMessage("Неверный формат клетки. Пример: e2 e4");
        continue;
    }

    var piece = game.Board.GetPiece(from);

    if (piece is null)
    {
        ShowMessage($"На клетке {from} нет фигуры.");
        continue;
    }

    if (piece.Color != game.SideToMove)
    {
        ShowMessage($"Сейчас ходят {game.SideToMove}.");
        continue;
    }

    var moves = moveGenerator.GenerateLegalMovesForPiece(game.Board, from);

    var selectedMove = moves.FirstOrDefault(m => m.To == to);

    if (selectedMove.Equals(default(Move)))
    {
        ShowMessage($"Фигура на {from} не может сходить на {to}.");
        continue;
    }

    game.Board.MovePiece(from, to);
    game.SwitchTurn();
}

bool TryParsePosition(string text, out Position position)
{
    position = default;

    if (text.Length != 2)
        return false;

    char file = text[0];
    char rank = text[1];

    if (file < 'a' || file > 'h')
        return false;

    if (rank < '1' || rank > '8')
        return false;

    int col = file - 'a';
    int row = 8 - (rank - '0');

    position = new Position(row, col);
    return position.IsValid;
}

void ShowMovesFrom(Position pos)
{
    var piece = game.Board.GetPiece(pos);

    if (piece is null)
    {
        ShowMessage($"На клетке {pos} нет фигуры.");
        return;
    }

    var moves = moveGenerator.GenerateLegalMovesForPiece(game.Board, pos);

    Console.Clear();
    Console.WriteLine(game.Board.ToPrettyString());
    Console.WriteLine();
    Console.WriteLine($"{piece} на {pos}:");

    if (moves.Count == 0)
    {
        Console.WriteLine("Нет доступных ходов.");
    }
    else
    {
        foreach (var move in moves)
        {
            var capture = move.IsCapture ? "x" : "-";
            Console.WriteLine($"{move.From} {capture} {move.To}");
        }
    }

    Console.WriteLine();
    Console.WriteLine("Нажмите Enter...");
    Console.ReadLine();
}

void ShowMessage(string message)
{
    Console.WriteLine();
    Console.WriteLine(message);
    Console.WriteLine("Нажмите Enter...");
    Console.ReadLine();
}