namespace Ipk24ChatClient.Cli;

/// <summary>
/// Contains the parsed command line arguments
/// </summary>
public class Args
{
    /// <summary>
    /// Action that should be done
    /// </summary>
    public Action Action { get; private set; } = Action.None;
    /// <summary>
    /// Server address
    /// </summary>
    public string? Address { get; private set; } = null;
    /// <summary>
    /// Server port
    /// </summary>
    public ushort Port { get; private set; } = 4567;
    /// <summary>
    /// UDP confirmation timeout, 250ms by default
    /// </summary>
    public TimeSpan UdpConfirmationTimeout { get; private set; } =
        TimeSpan.FromMilliseconds(250);
    /// <summary>
    /// Max number of retransmitions of UDP message, 3 by default
    /// </summary>
    public byte MaxUdpRetransmitions { get; private set; } = 3;
    /// <summary>
    /// Max number of messages to send before getting confirmation
    /// </summary>
    public byte MaxUdpMessagesAtOnce { get; private set; } = 1;
    /// <summary>
    /// When true, enables features that interfere with the specification
    /// </summary>
    public bool EnableNonStandardFeatures { get; set; } = false;
    /// <summary>
    /// Determines whenther to use color.
    /// </summary>
    public ColorMode ColorMode { get; set; } = ColorMode.Auto;

    /// <summary>
    /// Parses the command line arguments
    /// </summary>
    /// <param name="args">Command line arguments to parse</param>
    /// <returns>Parsed command line arguments</returns>
    /// <exception cref="ArgumentException">For invalid arguments</exception>
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

    private void ParseMethod(ReadOnlySpan<string> args)
    {
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
                case "-w" or "--udp-window" or "--max-udp-messages":
                    MaxUdpMessagesAtOnce = ParseArg<byte>(ref args);
                    break;
                case "--color" or "--colour":
                    ColorMode = ParseColorMode(TakeSecond(ref args));
                    break;
                case string s when s.StartsWith("--color=")
                    || s.StartsWith("--colour="):
                    ColorMode = ParseColorMode(
                        s.AsSpan((s.IndexOf('=') + 1)..)
                    );
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

    private static ColorMode ParseColorMode(ReadOnlySpan<char> mode) =>
        mode switch
        {
            "auto" => ColorMode.Auto,
            "always" => ColorMode.Always,
            "never" => ColorMode.Never,
            _ => throw new ArgumentException(
                $"Invalid color mode '{mode}' expected one of: 'auto', "
                    + "'always', 'never'"
            ),
        };
}
