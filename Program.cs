using System.Diagnostics;
using Bny.Console;
using Ipk24ChatClient.Cli;
using Ipk24ChatClient.Tcp;
using Ipk24ChatClient.Udp;

namespace Ipk24ChatClient;

static class Program
{
    /// <summary>
    /// Sleep time between each iteration.
    /// </summary>
    static readonly TimeSpan sleepTime = TimeSpan.FromMilliseconds(10);
    /// <summary>
    /// Used for console interaction.
    /// </summary>
    static ConsoleReader reader = new();
    /// <summary>
    /// The client for sending and receiving messages.
    /// </summary>
    static ChatClient client = null!;
    /// <summary>
    /// True if non standard features are enabled.
    /// </summary>
    static bool nonStandard =
        Environment.GetEnvironmentVariable("IPK_EXTEND") == "YES";

    public static void Main(string[] args) =>
        Environment.Exit(Start(args.AsSpan()));

    /// <summary>
    /// The entry point for the application.
    /// </summary>
    /// <param name="argv">CLI arguments.</param>
    /// <returns>Error code.</returns>
    public static int Start(ReadOnlySpan<string> argv)
    {
        // parse args
        Args args;
        try
        {
            args = Args.Parse(argv);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        if (args.EnableNonStandardFeatures)
        {
            nonStandard = true;
        }

        args.EnableNonStandardFeatures = nonStandard;
        Validators.ExtendChannel = nonStandard;

        // init colors.
        InitANSI(args.ColorMode);

        // Choose what to do.
        switch (args.Action)
        {
            case Cli.Action.Help:
                return Help(args);
            case Cli.Action.Tcp:
                client = new TcpChatClient();
                break;
            case Cli.Action.Udp:
                client = new UdpChatClient();
                break;
            default:
                throw new UnreachableException("Invalid action");
        }

        // Initialize
        try
        {
            Prepare(args);
        }
        catch (Exception ex)
        {
            reader.EWriteLine($"{r}ERR:{reset} {ex.Message}");
            return 1;
        }

        // Run the mainloop
        return RunClient();
    }

    private static void Prepare(Args args)
    {
        if (nonStandard)
        {
            Console.Write("Conneting... ");
        }

        // Initialize the connection.
        client.Connect(args);

        if (!Console.IsInputRedirected)
        {
            Console.TreatControlCAsInput = true;
        }

        if (nonStandard)
        {
            Console.WriteLine($"{g}Done!{reset}");
        }

        // Initialize the reader.
        reader.Init();

        if (nonStandard) {
            reader.PromptLength = "?: ".Length;
            reader.Prompt = $"{y}?{dgr}: {reset}";
        }
    }

    private static int RunClient()
    {
        // The mainloop
        while (true)
        {
            // Read from console.
            string? line;
            try
            {
                line = reader.TryReadLine();
            }
            catch (CtrlCException)
            {
                // Exit when Ctrl+C is pressed.
                break;
            }

            // Process the line readed from console.
            if (line is not null && line.Length != 0)
            {
                try
                {
                    RunCommand(line);
                }
                catch (Exception ex)
                {
                    reader.EWriteLine($"{r}ERR:{reset} {ex.Message}");
                }
            }

            // Check for new received messages.
            try
            {
                while (Receive())
                    ;
            }
            catch (Exception ex)
            {
                reader.EWriteLine($"{r}ERR:{reset} {ex.Message}");
            }

            // Sleep for little while so that the CPU doesn't burn from doing
            // nothing
            Thread.Sleep(sleepTime);
        }

        // Close the connection.
        try
        {
            client.Bye();
        }
        catch (Exception ex)
        {
            reader.EWriteLine($"{r}ERR:{reset} {ex.Message}");
        }

        return 0;
    }

    /// <summary>
    /// Parses the command and does appropriate action.
    /// </summary>
    /// <param name="c">The command the user typed.</param>
    /// <exception cref="InvalidOperationException">Invalid command</exception>
    static void RunCommand(string c)
    {
        // Differentiate between commands and regural messages.
        if (!c.StartsWith('/'))
        {
            client.Send(c);
            return;
        }

        // extensions
        switch (c)
        {
            case "/clear" or "/claer":
                reader.Clear();
                return;
        }

        // Parse the standard commands
        var cmd = c.AsSpan()[1..];

        const string authCmd = "auth ";
        const string joinCmd = "join ";
        const string renameCmd = "rename ";
        const string helpCmd = "help";

        if (cmd.StartsWith(authCmd))
        {
            RunAuth(cmd[authCmd.Length..]);
        }
        else if (cmd.StartsWith(joinCmd))
        {
            RunJoin(cmd[joinCmd.Length..]);
        }
        else if (cmd.StartsWith(renameCmd))
        {
            RunRename(cmd[renameCmd.Length..]);
        }
        else if (cmd.SequenceEqual(helpCmd))
        {
            RunHelp();
        }
        else
        {
            throw new InvalidOperationException("Invalid command");
        }
    }

    static void RunHelp()
    {
        reader.WriteLine(
            $"""
            Welcome to help for {i}{g}ip24chat-client{reset} commands by {sign}.

            {g}Commands:
              {c}/auth {w}<username> <secret> <display name>{reset}
                Authorises to the the server and sets the display name.

              {c}/join {w}<channel id>{reset}
                Joins channel on server.

              {c}/rename {w}<display name>{reset}
                Changes the display name.

              {c}/help{reset}
                Shows this help.

            {g}Extension commands:
              {c}/clear
              /claer{reset}
                {clears} the screen.

            To exit press {i}{w}Ctrl+C{reset}.
            """
        );
    }

    static void RunRename(ReadOnlySpan<char> cmd)
    {
        client.DisplayName = cmd.ToString();
        if (nonStandard) {
            reader.PromptLength = client.DisplayName.Length + 2;
            reader.Prompt = $"{y}{client.DisplayName}{dgr}: {reset}";
        }
    }

    static void RunJoin(ReadOnlySpan<char> cmd)
    {
        client.Join(cmd);
    }

    static void RunAuth(ReadOnlySpan<char> cmd)
    {
        var idx = cmd.IndexOf(' ');
        if (idx == -1)
        {
            throw new InvalidOperationException(
                "Missing secret for command auth"
            );
        }

        var username = cmd[..idx];
        cmd = cmd[(idx + 1)..];
        idx = cmd.IndexOf(' ');

        if (idx == -1)
        {
            throw new InvalidOperationException(
                "Missing display name for the auth command"
            );
        }

        var secret = cmd[..idx];
        var name = cmd[(idx + 1)..];

        client.Authorize(username, secret, name.ToString());

        if (nonStandard) {
            reader.PromptLength = client.DisplayName.Length + 2;
            reader.Prompt = $"{y}{client.DisplayName}{dgr}: {reset}";
        }
    }

    /// <summary>
    /// Check for new messages from the server.
    /// </summary>
    /// <returns>
    /// True when new message has been processed. False when there was no new
    /// message
    /// </returns>
    static bool Receive()
    {
        switch (client.Receive()) {
            case ErrMessage msg:
                reader.EWriteLine(
                    $"{r}ERR {reset}FROM {m}{msg.DisplayName}{dgr}: {reset}"
                        + msg.Content
                );
                return true;
            case ReplyMessage msg:
                if (msg.Ok)
                {
                    reader.EWriteLine($"{g}Success: {reset}{msg.Content}");
                }
                else
                {
                    reader.EWriteLine($"{r}Failure: {reset}{msg.Content}");
                }
                return true;
            case MsgMessage msg:
                reader.WriteLine(
                    $"{m}{msg.Sender}{dgr}: {w}{msg.Content}{reset}"
                );
                return true;
            case ByeMessage:
                if (nonStandard) {
                    reader.WriteLine($"{dgr}Server said bye{reset}");
                }
                return true;
        }
        return false;
    }

    private static int Help(Args args)
    {
        Console.WriteLine(
            $"""
            Welcome to help for {i}{g}ip24chat-client{reset} by {sign}.

            {g}Usage:
              {c}ipk24chat-cleint {y}-h{reset}
                Shows this help.

              {c}ipk24chat-client {y}-t {w}<protocol> {y}-s {w}<server address>
                               {dg}[flags] [extension flags]{reset}
                Connects to the server address with the protocol.

            {g}Flags:
              {y}-t  --protocol {w}(tcp | udp){reset}
                Selects the protocol.

              {y}-s  --address  --server {w}<server address>{reset}
                Selects the chat server to connect to.

              {y}-p  --port {w}<port>{reset}
                Selects the port to use when connecting to server. Default is
                {w}4567{reset}.

              {y}-d  --udp-timeout {w}<timeout>{reset}
                Sets the udp confirmation timeout in milliseconds. Has no
                effect when using tcp. Default is {w}250{reset}.

              {y}-r  --udp-retransmitions {w}<count>{reset}
                Sets the max number of retransimitions when using udp. Has no
                effect when using tcp. Default is {w}3{reset}.

              {y}-h  -?  --help{reset}
                Prints this help.

            {g}Extension flags:
              {y}-e  --extend  --non-standard{reset}
                Enable non-standard quality of life improvements that would
                otherwise interfere with the specification. This can be also
                enabled with the {m}IPK_EXTEND{reset} environment variable.

              {y}-w  --udp-window  --max-udp-messages {w}<count>{reset}
                Max number of messages to send at once before receiving
                confirmation. Has no effect when using tcp. Default is {w}
                1{reset}.

              {y}--color  --colour {w}(auto | always | never)
              {y}--color  --colour{w}=(auto | always | never){reset}
                Set the mode for printing colors.
                {w}always{reset}
                  Use colors when printing.

                {w}never{reset}
                  Don't use colors when printing.

                {w}auto{reset}
                  Use colors only when printing to terminal and when extended
                  features are enabled either with the {y}-e{reset} flag or the
                  {m}IPK_EXTEND{reset} environment variable.

            {g}Envirnoment variables:
              {m}IPK_EXTEND{dgr}[=(YES | <other>)]{reset}
                When set to {w}YES{reset}, enables non-standard quality of life
                improvements that would otherwise interfere with the
                specification. Otherwise doesn't affect the setting. This can
                be also enabled with the {y}-e {reset}flag.
            """
        );

        return 0;
    }

    // ANSI codes that are set conditionally.

    static string sign = "xstigl00";
    static string r = "";
    static string g = "";
    static string c = "";
    static string m = "";
    static string y = "";
    static string w = "";
    static string dg = "";
    static string dgr = "";
    static string reset = "";
    static string i = "";
    static string clears = "Clears";

    static void InitANSI(ColorMode colorMode)
    {
        var useColor = colorMode switch
        {
            ColorMode.Auto => nonStandard && !Console.IsOutputRedirected,
            ColorMode.Always => true,
            ColorMode.Never => false,
            _ => throw new UnreachableException(),
        };
        if (!useColor)
        {
            return;
        }

        sign = string.Concat("xstigl00".Select((p, i) =>
            Term.Prepare(Term.fg, 250 - 10 * i, 50, 170 + 10 * i, p)
        ));
        r = Term.brightRed;
        g = Term.brightGreen;
        c = Term.brightCyan;
        m = Term.brightMagenta;
        y = Term.brightYellow;
        w = Term.brightWhite;
        dg = Term.green;
        dgr = Term.brightBlack;
        reset = Term.reset;
        i = Term.italic;
        clears = $"\x1b[9mClaers\x1b[29mClears";
    }
}
