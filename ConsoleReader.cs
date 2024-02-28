using System.Text;

class ConsoleReader
{
    private StringBuilder typed = new();
    private int position = 0;

    public string? TryReadLine()
    {
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey();
            if (key.KeyChar == '\n')
            {
                var res = typed.ToString();
                typed.Clear();
                return res;
            }
            if (key.KeyChar != 0)
            {
                typed.Append(key.KeyChar);
            }
        }

        return null;
    }
}
