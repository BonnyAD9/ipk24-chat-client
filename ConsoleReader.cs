using System.Text;
using Bny.Console;

class ConsoleReader
{
    private StringBuilder typed = new();
    private int position = 0;
    private bool isOutConsole = !Console.IsOutputRedirected;
    private bool isErrConsole = !Console.IsErrorRedirected;
    private bool isInConsole = !Console.IsInputRedirected;
    private int lastWidth = 0;
    private string prompt = "";
    public string Prompt
    {
        get => prompt;
        set
        {
            prompt = value;
            Reprint();
        }
    }
    private int TermPos => position + Prompt.Length;

    public void Init() {
        if (isOutConsole) {
            Term.Save();
            lastWidth = Console.BufferWidth;
        }
    }

    public string? TryReadLine()
    {
        if (isOutConsole) {
            var newWidth = Console.BufferWidth;
            if (newWidth != lastWidth) {
                lastWidth = newWidth;
                Reprint();
            }
        }

        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true);
            if (key.KeyChar == '\n' || key.Key == ConsoleKey.Enter)
            {
                position = typed.Length;

                if (isOutConsole) {
                    ToPosition();
                    Ln();
                }

                var res = typed.ToString();
                typed.Clear();

                if (isOutConsole) {
                    Term.Save();
                }

                position = 0;
                Reprint();
                return res;
            }

            if (key.Key == ConsoleKey.C && key.Modifiers == ConsoleModifiers.Control) {
                position = typed.Length;
                if (isOutConsole) {
                    ToPosition();
                    Ln();
                }
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
                    if (!isOutConsole) {
                        break;
                    }

                    var newPos = position + lastWidth;
                    if (newPos > typed.Length) {
                        goto case ConsoleKey.End;
                    }
                    position = newPos;
                    Term.Down();
                    break;
                case ConsoleKey.UpArrow:
                    if (!isOutConsole) {
                        break;
                    }

                    var newPos2 = position - lastWidth;
                    if (newPos2 < 0) {
                        goto case ConsoleKey.Home;
                    }

                    position = newPos2;
                    Term.Up();
                    break;
                case ConsoleKey.End:
                    position = typed.Length;
                    ToPosition();
                    break;
                case ConsoleKey.Home:
                    position = 0;
                    if (isOutConsole) {
                        Term.Restore();
                    }
                    break;
                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        typed.Insert(position, key.KeyChar);
                        ++position;
                        if (isOutConsole) {
                            bool isEnd = Console.CursorLeft == lastWidth - 1;
                            Console.Write(key.KeyChar);
                            if (isEnd) {
                                Ln();
                            }
                            PrintFromPosition();
                        }
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
            Prompt,
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
            Prompt,
            typed
        );
        ToPosition();
    }

    private void MoveLeft() {
        if (!isOutConsole) {
            return;
        }

        if (Console.CursorLeft == 0) {
            Term.Form(Term.upStart, 1, Term.right, lastWidth - 1);
        } else {
            Term.Left();
        }
    }

    private void MoveRight() {
        if (!isOutConsole) {
            return;
        }

        if (Console.CursorLeft == lastWidth - 1) {
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
        var width = lastWidth;
        var right = TermPos % width;
        var down = TermPos / width;
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

    internal void Clear()
    {
        if (isOutConsole) {
            Term.Form(Term.erase, "\x1b[3J", Term.home, Term.save, Prompt, typed);
            ToPosition();
        }
    }

    private void Reprint() {
        if (!isOutConsole) {
            return;
        }

        Term.Form(Term.restore, Term.eraseFromCursor, Prompt, typed);
        ToPosition();
    }
}
