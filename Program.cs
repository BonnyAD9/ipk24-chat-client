using System.Diagnostics;
using Bny.Console;
using IpkChat2024Client.Cli;
using IpkChat2024Client.Tcp;
using IpkChat2024Client.Udp;

namespace IpkChat2024Client;

static class Program
{

    static readonly TimeSpan sleepTime = TimeSpan.FromMilliseconds(10);
    static ConsoleReader reader = new();
    static ChatClient client = null!;
    static bool nonStandard =
        Environment.GetEnvironmentVariable("IS_BONNYAD9") == "YES";

    public static void Main(string[] args) =>
        Environment.Exit(Start(args.AsSpan()));

    public static int Start(ReadOnlySpan<string> argv)
    {
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

        InitANSI();

        Validators.ExtendChannel = nonStandard;

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

        try
        {
            Prepare(args);
        }
        catch (Exception ex)
        {
            reader.EWriteLine($"{r}ERR:{reset} {ex.Message}");
            return 1;
        }

        return RunClient();
    }

    private static void Prepare(Args args)
    {
        if (nonStandard)
        {
            Console.Write("Conneting... ");
        }

        client.Connect(args);

        if (!Console.IsInputRedirected)
        {
            Console.TreatControlCAsInput = true;
        }

        if (nonStandard)
        {
            Console.WriteLine($"{g}Done!{reset}");
        }

        reader.Init();

        if (nonStandard) {
            reader.PromptLength = "?: ".Length;
            reader.Prompt = $"{y}?{dgr}: {reset}";
        }
    }

    private static int RunClient()
    {
        while (true)
        {
            string? line;
            try
            {
                line = reader.TryReadLine();
            }
            catch (CtrlCException)
            {
                break;
            }

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

            try
            {
                while (Receive())
                    ;
            }
            catch (Exception ex)
            {
                reader.EWriteLine($"{r}ERR:{reset} {ex.Message}");
            }

            Thread.Sleep(sleepTime);
        }

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

    static void RunCommand(string c)
    {
        if (!c.StartsWith('/'))
        {
            client.Send(c);
            return;
        }

        switch (c)
        {
            case "/clear" or "/claer":
                reader.Clear();
                return;
        }

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
              {w}/auth <username> <secret> <display name>{reset}
                Authorises to the the server and sets the display name.

              {w}/join <channel id>{reset}
                Joins channel on server.

              {w}/rename <display name>{reset}
                Changes the display name.

              {w}/help{reset}
                Shows this help.

            To exit press {w}Ctrl+C{reset}.
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
              {w}ipk24chat-cleint -h{reset}
                Shows this help.

              {w}ipk24chat-client -t <protocol> -s <server address>
                                  {dgr}[flags]{reset}
                Connects to the server address with the protocol.

            {g}Flags:
              {y}-t  --protocol {w}(tcp | udp){reset}
                Selects the protocol.

              {y}-s  --address  --server {w}<server address>{reset}
                Selects the chat server to connect to.

              {y}-p  --port {w}<port>{reset}
                Selects the port to use when connecting to server. Default is
                4567

              {y}-d  --udp-timeout {w}<timeout>{reset}
                Sets the udp confirmation timeout in milliseconds. Has no
                effect when using tcp. Default is 250.

              {y}-r  --udp-retransmitions {w}<count>{reset}
                Sets the max number of retransimitions when using udp. Has no
                effect when using tcp.

              {y}-h  -?  --help{reset}
                Prints this help.

              {y}-e  --extend  --non-standard{reset}
                Enable non-standard features that would otherwise interfere
                with the specification.
            """
        );

        return 0;
    }

    // ANSI stuff

    static string sign = "";
    static string r = "";
    static string g = "";
    static string y = "";
    static string m = "";
    static string w = "";
    static string dgr = "";
    static string reset = "";
    static string i = "";

    static void InitANSI()
    {
        if (!nonStandard || Console.IsOutputRedirected)
        {
            return;
        }
        sign = string.Concat("xstigl00".Select((p, i) =>
            Term.Prepare(Term.fg, 250 - 10 * i, 50, 170 + 10 * i, p)
        ));
        r = Term.brightRed;
        g = Term.brightGreen;
        y = Term.brightYellow;
        m = Term.brightMagenta;
        w = Term.brightWhite;
        dgr = Term.brightBlack;
        reset = Term.reset;
        i = Term.italic;
    }
}
