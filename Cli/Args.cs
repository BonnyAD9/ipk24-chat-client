namespace IpkChat2024Client.Cli;

class Args
{
    public Action Action { get; private set; } = Action.None;
    public string? Address { get; private set; } = null;
    public ushort Port { get; private set; } = 4567;
    public TimeSpan UdpConfirmationTimeout { get; private set; } =
        TimeSpan.FromMilliseconds(250);
    public byte MaxUdpRetransmitions { get; private set; } = 3;
    public bool ShowHelp { get; private set; } = false;
    public bool EnableNonStandardFeatures { get; private set; } = false;

    public static Args Parse(ReadOnlySpan<string> args) {
        var res = new Args();
        res.ParseMethod(args);

        if (res.Action == Action.None) {
            throw new ArgumentException(
                "Missing action. Use argument '-t' or '-h'."
            );
        }

        if (res.Action != Action.Help && res.Address is null) {
            throw new ArgumentException(
                "Missing server address with argument '-s'."
            );
        }

        return res;
    }

    private Args() {}

    private void ParseMethod(ReadOnlySpan<string> args) {
        while (args.Length != 0)
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
                case "-d" or "--udp-timeout":
                    UdpConfirmationTimeout =
                        TimeSpan.FromMilliseconds(ParseArg<ushort>(ref args));
                    break;
                case "-r" or "--udp-retransmitions":
                    MaxUdpRetransmitions = ParseArg<byte>(ref args);
                    break;
                case "-h" or "-?" or "--help":
                    Action = Action.Help;
                    args = args[1..];
                    break;
                case "-e" or "--extend" or "--non-standard":
                    EnableNonStandardFeatures = true;
                    args = args[1..];
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

    private string TakeSecond(ref ReadOnlySpan<string> args) {
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
