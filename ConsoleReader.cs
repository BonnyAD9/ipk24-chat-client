namespace Ipk24ChatClient;

using System.Text;
using Bny.Console;

/// <summary>
/// This class is used for interaction with the console. It handles reading
/// from stdin without blocking and printing to console while the user is
/// typing. It also properly handles situations where some of the standard
/// streams is not connected to the console.
/// </summary>
class ConsoleReader
{
    private readonly StringBuilder typed = new();
    private int position = 0;
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
    /// <summary>
    /// Number of characters in Prompt that will be printed to screen.
    /// </summary>
    public int PromptLength { get; set; }
    /// <summary>
    /// X coordinate of the cursor if everything was on single line.
    /// </summary>
    private int TermPos => position + PromptLength;

    // cache theese values because getting them may be expensive.
    private readonly bool isOutConsole = !Console.IsOutputRedirected;
    private readonly bool isErrConsole = !Console.IsErrorRedirected;
    private readonly bool isInConsole = !Console.IsInputRedirected;

    /// <summary>
    /// Cached width of the console buffer in characters.
    /// </summary>
    private int lastWidth = 0;
    /// <summary>
    /// Calculate the x position of the cursor in console using chached values.
    /// </summary>
    private int TermLeft => TermPos % lastWidth;

    /// <summary>
    /// Initializes the reader. Since call to this, interaction with the
    /// should be done exclusively with this instance.
    /// </summary>
    public void Init() {
        if (isOutConsole) {
            Term.Save();
            lastWidth = Console.BufferWidth;
        }
    }

    /// <summary>
    /// React on user input in the console.
    /// </summary>
    /// <returns>
    /// One line that the user typed. Null if the user has not typed one full
    /// line yet.
    /// </returns>
    /// <exception cref="CtrlCException">
    /// Thrown when user pressed Ctrl+C
    /// </exception>
    public string? TryReadLine()
    {
        // Cache the terminal width.
        if (isOutConsole) {
            var newWidth = Console.BufferWidth;
            if (newWidth != lastWidth) {
                lastWidth = newWidth;
                Reprint();
            }
        }

        // React on user input while there is any.
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true);

            // Check if the user typed one whole line.
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

            // Check for Ctrl+C
            if (key.Key == ConsoleKey.C && key.Modifiers == ConsoleModifiers.Control) {
                position = typed.Length;
                if (isOutConsole) {
                    ToPosition();
                    Ln();
                }
                throw new CtrlCException();
            }

            // If the input is not console, read as if it was file.
            if (!isInConsole) {
                typed.Append(key.KeyChar);
                continue;
            }

            // React on special keys.
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
                        MoveLeft();
                        --position;
                    }
                    break;
                case ConsoleKey.RightArrow:
                    if (position != typed.Length) {
                        MoveRight();
                        ++position;
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
                        ToPosition();
                    }
                    break;
                default:
                    // Check if the character is printable
                    if (char.IsControl(key.KeyChar))
                    {
                        break;
                    }

                    // Insert the user typed printable character at the current
                    // position.
                    typed.Insert(position, key.KeyChar);
                    ++position;
                    if (isOutConsole) {
                        bool isEnd = TermLeft == 0 && TermPos != 0;
                        Console.Write(key.KeyChar);
                        if (isEnd) {
                            Ln();
                        }
                        PrintFromPosition();
                    }
                    break;
            }
        }

        return null;
    }

    /// <summary>
    /// Print to the standard output.
    /// </summary>
    /// <param name="str">What to print.</param>
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

    /// <summary>
    /// Print to the standard error output.
    /// </summary>
    /// <param name="str">What to print.</param>
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

        // properly handle when the cursor is all the way left and it shoud
        // jump to the end of the previous line.
        if (TermLeft == 0) {
            Term.Form(Term.upStart, 1, Term.right, lastWidth - 1);
        } else {
            Term.Left();
        }
    }

    private void MoveRight() {
        if (!isOutConsole) {
            return;
        }

        // properly handle when the cursor is all the way right and it should
        // jump to the start of the next line.
        if (TermLeft == lastWidth - 1) {
            Term.Form(Term.downStart, 1);
        } else {
            Term.Right();
        }
    }

    /// <summary>
    /// Prints to stdout all the typed data from the cursor position. Does
    /// nothing when stdout is redirected.
    /// </summary>
    private void PrintFromPosition() {
        if (!isOutConsole) {
            return;
        }

        Term.Form(Term.eraseFromCursor, typed.ToString()[position..]);
        ToPosition();
    }

    /// <summary>
    /// Moves cursor to the correct position where the user should see it.
    /// </summary>
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

    /// <summary>
    /// Prints one line.
    /// </summary>
    private void Ln() {
        if (isOutConsole) {
            Console.Write("\r\n");
        } else {
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Clear the console screen and buffer and move cursor to the top left.
    /// </summary>
    internal void Clear()
    {
        if (isOutConsole) {
            Term.Form(Term.erase, "\x1b[3J", Term.home, Term.save, Prompt, typed);
            ToPosition();
        }
    }

    /// <summary>
    /// Refreshes the typed data that the user sees.
    /// </summary>
    private void Reprint() {
        if (!isOutConsole) {
            return;
        }

        Term.Form(Term.restore, Term.eraseFromCursor, Prompt, typed);
        ToPosition();
    }
}
