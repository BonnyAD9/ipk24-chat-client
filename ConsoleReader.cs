using System.Text;
using Bny.Console;

class ConsoleReader
{
    private StringBuilder typed = new();
    private int position = 0;

    public void Init() {
        Term.Save();
    }

    public string? TryReadLine()
    {
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey();
            if (key.KeyChar == '\n' || key.Key == ConsoleKey.Enter)
            {
                position = typed.Length;
                ToPosition();
                Console.Write("\n\r");
                var res = typed.ToString();
                typed.Clear();
                Term.Save();
                position = 0;
                return res;
            }
            if (key.Key == ConsoleKey.C && key.Modifiers == ConsoleModifiers.Control) {
                position = typed.Length;
                ToPosition();
                Console.Write("\n\r");
                throw new CtrlCException();
            }
            switch (key.Key)
            {
                case ConsoleKey.Backspace:
                    if (position != 0) {
                        --position;
                        typed.Remove(position, 1);
                        MoveLeft();
                        PrintFromPosition();
                    }
                    break;
                case ConsoleKey.Delete:
                    if (position != typed.Length) {
                        typed.Remove(position, 1);
                        PrintFromPosition();
                    }
                    break;
                case ConsoleKey.LeftArrow:
                    if (position != 0) {
                        --position;
                        MoveLeft();
                    }
                    break;
                case ConsoleKey.RightArrow:
                    if (position != typed.Length) {
                        ++position;
                        MoveRight();
                    }
                    break;
                case ConsoleKey.DownArrow:
                    var new_pos = position + Console.BufferWidth;
                    if (new_pos <= typed.Length) {
                        position = new_pos;
                        Term.Down();
                    }
                    break;
                case ConsoleKey.UpArrow:
                    var new_pos2 = position - Console.BufferWidth;
                    if (new_pos2 >= 0) {
                        position = new_pos2;
                        Term.Up();
                    }
                    break;
                case ConsoleKey.End:
                    position = typed.Length;
                    ToPosition();
                    break;
                case ConsoleKey.Home:
                    position = 0;
                    Term.Restore();
                    break;
                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        typed.Insert(position, key.KeyChar);
                        ++position;
                        PrintFromPosition();
                    }
                    break;
            }
        }

        return null;
    }

    public void WriteLine(string str) {
        Term.Form(
            Term.restore,
            Term.eraseFromCursor,
            str,
            "\n\r",
            Term.save,
            typed
        );
        ToPosition();
    }

    public void EWriteLine(string str) {
        Term.Form(
            Term.restore,
            Term.eraseFromCursor
        );
        Console.Error.Write(str);
        Term.Form(
            "\n\r",
            Term.save,
            typed
        );
        ToPosition();
    }

    private void MoveLeft() {
        if (Console.CursorLeft == 0) {
            Console.Write(Term.upEnd, 1);
        } else {
            Term.Left();
        }
    }

    private void MoveRight() {
        if (Console.CursorLeft == Console.BufferWidth - 1) {
            Term.Form(Term.downStart, 1);
        } else {
            Term.Right();
        }
    }

    private void PrintFromPosition() {
        Term.Form(Term.eraseFromCursor, typed.ToString()[position..]);
        ToPosition();
    }

    private void ToPosition() {
        Term.Restore();
        var width = Console.BufferWidth;
        var right = position % width;
        var down = position / width;
        if (right != 0) {
            Term.Right(right);
        }
        if (down != 0) {
            Term.Down(down);
        }
    }
}
