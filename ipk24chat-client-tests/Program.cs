using Bny.Console;
using Ipk24ChatClientTester.Cli;

namespace Ipk24ChatClientTester;

class Program
{
    public static void Main(string[] argv)
    {
        Args args;
        try
        {
            args = Args.Parse(argv);
        }
        catch (Exception ex)
        {
            
        }
    }

    static void PrintErr(string msg) =>
        Console.Error.WriteLine($"{Term.red}Error: {msg}");

    static void SendInput(string input) =>
        Console.WriteLine(input);

    static void WriteOut(string output) =>
        Console.Error.WriteLine(output);
}
