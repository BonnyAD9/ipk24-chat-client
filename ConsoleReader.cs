using System.Text;
using Bny.Console;

class ConsoleReader
{
    private StringBuilder typed = new();
    private int position = 0;
    private bool isOutConsole = !Console.IsOutputRedirected;
    private bool isErrConsole = !Console.IsErrorRedirected;
    private bool isInConsole = !Console.IsInputRedirected;

    public void Init() {
        if (isOutConsole) {
            Term.Save();
        }
    }

    public string? TryReadLine()
    {
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey();
            if (key.KeyChar == '\n' || key.Key == ConsoleKey.Enter)
            {
                position = typed.Length;

                if (isOutConsole) {
                    ToPosition();
                }

                Ln();
                var res = typed.ToString();
                typed.Clear();

                if (isOutConsole) {
                    Term.Save();
                }

                position = 0;
                return res;
            }

            if (key.Key == ConsoleKey.C && key.Modifiers == ConsoleModifiers.Control) {
                position = typed.Length;
                ToPosition();
                Ln();
                throw new CtrlCException();
            }

            if (!isInConsole) {
                typed.Append(key.KeyChar);
                continue;
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
        if (!isOutConsole) {
            Console.WriteLine(str);
            return;
        }

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
        if (!isErrConsole) {
            Console.Error.WriteLine(str);
            return;
        }

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
        if (!isOutConsole) {
            return;
        }

        if (Console.CursorLeft == 0) {
            Console.Write(Term.upEnd, 1);
        } else {
            Term.Left();
        }
    }

    private void MoveRight() {
        if (!isOutConsole) {
            return;
        }

        if (Console.CursorLeft == Console.BufferWidth - 1) {
            Term.Form(Term.downStart, 1);
        } else {
            Term.Right();
        }
    }

    private void PrintFromPosition() {
        if (!isOutConsole) {
            return;
        }

        Term.Form(Term.eraseFromCursor, typed.ToString()[position..]);
        ToPosition();
    }

    private void ToPosition() {
        if (!isOutConsole) {
            return;
        }

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

    private void Ln() {
        if (isOutConsole) {
            Console.Write("\r\n");
        } else {
            Console.WriteLine();
        }
    }
}
