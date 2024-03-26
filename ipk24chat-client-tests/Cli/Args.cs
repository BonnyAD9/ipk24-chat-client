namespace Ipk24ChatClientTester.Cli;

public class Args
{
    public string Address { get; private set; } = "127.0.0.1";
    public ushort Port { get; private set; } = 4567;
    public Action Action { get; private set; } = Action.Help;
    public string ReplayFile { get; private set; } = null!;

    private Args() {}

    public static Args Parse(ReadOnlySpan<string> args)
    {
        Args res = new();
        res.ParseArgs(args);
        if (res.Action != Action.Help && res.ReplayFile is null)
        {
            throw new ArgumentException("Missing replay file");
        }

        return res;
    }

    private void ParseArgs(ReadOnlySpan<string> args)
    {
        while (args.Length > 0)
        {
            switch (args[0])
            {
                case "-t" or "--protocol":
                    Action = TakeSecond(ref args).ToLower() switch
                    {
                        "tcp" => Action.Tcp,
                        "udp" => Action.Udp,
                        _ => throw new ArgumentException(
                            "Protocol must be either 'tcp' or 'udp'"
                        ),
                    };
                    break;
                case "-s" or "--address" or "--server":
                    Address = TakeSecond(ref args);
                    break;
                case "-p" or "--port":
                    Port = ParseArg<ushort>(ref args);
                    break;
                case "-f" or "--file" or "--replay-file":
                    ReplayFile = TakeSecond(ref args);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument {args[0]}");
            }
        }
    }

    private T ParseArg<T>(ref ReadOnlySpan<string> args) where T: IParsable<T> {
        var name = args[0];
        var value = TakeSecond(ref args);
        if (!T.TryParse(value, null, out T? res)) {
            throw new ArgumentException(
                $"Failed to parse value of argument '{name}': Value must be "
                    + $"{typeof(T).Name} but it was '{value}'"
            );
        }
        return res;
    }

    private static string TakeSecond(ref ReadOnlySpan<string> args) {
        if (args.Length < 2) {
            throw new ArgumentException(
                $"Expected parameter to argument '{args[0]}'"
            );
        }
        var res = args[1];
        args = args[2..];
        return res;
    }

}
