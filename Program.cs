using System.ComponentModel.Design;
using System.Diagnostics;
using Bny.Console;
using IpkChat2024Client.Cli;
using IpkChat2024Client.Tcp;
using TcpChatClient = IpkChat2024Client.Tcp.ChatClient;

namespace IpkChat2024Client;

static class Program
{

    static readonly TimeSpan sleepTime = TimeSpan.FromMilliseconds(10);
    static ConsoleReader reader = new();
    static IChatClient client = null!;
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

        Validators.ExtendChannel = nonStandard;

        switch (args.Action)
        {
            case Cli.Action.Help:
                return Help(args);
            case Cli.Action.Tcp:
                try
                {
                    PrepareTcp(args);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                }
                return RunClient();
            case Cli.Action.Udp:
                try
                {
                    PrepareUdp(args);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                }
                return RunClient();
            default:
                throw new UnreachableException("Invalid action");
        }
    }

    private static void PrepareUdp(Args args)
    {
        throw new NotImplementedException("Udp is not implemented yet");
    }

    private static void PrepareTcp(Args args)
    {
        TcpChatClient chat = new();

        if (nonStandard) {
            Console.Write("Conneting... ");
        }

        chat.Connect(args.Address!, args.Port);
        client = chat;

        if (!Console.IsInputRedirected) {
            Console.TreatControlCAsInput = true;
        }

        if (nonStandard) {
            Term.FormLine(Term.brightGreen, "Done!", Term.reset);
        }

        reader.Init();

        if (nonStandard) {
            reader.PromptLength = "?: ".Length;
            reader.Prompt =
                $"{Term.brightYellow}?{Term.brightBlack}: {Term.reset}";
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
                    if (nonStandard && !Console.IsErrorRedirected) {
                        reader.EWriteLine(
                            $"{Term.brightRed}ERR:{Term.reset} {ex.Message}"
                        );
                    } else {
                        reader.EWriteLine($"ERR: {ex.Message}");
                    }
                }
            }

            try
            {
                Receive();
            }
            catch (Exception ex)
            {
                if (nonStandard && !Console.IsErrorRedirected) {
                    reader.EWriteLine(
                        $"{Term.brightRed}ERR:{Term.reset} {ex.Message}"
                    );
                } else {
                    reader.EWriteLine($"ERR: {ex.Message}");
                }
            }

            Thread.Sleep(sleepTime);
        }

        try
        {
            client.Bye();
        }
        catch (Exception ex)
        {
            if (nonStandard && !Console.IsErrorRedirected) {
                reader.EWriteLine(
                    $"{Term.brightRed}ERR:{Term.reset} {ex.Message}"
                );
            } else {
                reader.EWriteLine($"ERR: {ex.Message}");
            }
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
        var (sign, g, y, i, w, dgr, r) = !Console.IsInputRedirected
            ? (
                string.Concat("xstigl00".Select((p, i) => {
                    return Term.Prepare(
                        Term.fg, 250 - 10 * i, 50, 170 + 10 * i, p
                    );
                })),
                Term.brightGreen,
                Term.brightYellow,
                Term.italic,
                Term.brightWhite,
                Term.brightBlack,
                Term.reset
            ) : (
                "xstigl00",
                "",
                "",
                "",
                "",
                "",
                ""
            );

        reader.WriteLine(
            $"""
            Welcome to help for {i}{g}ip24chat-client{r} commands by {sign}.

            {g}Commands:
              {w}/auth <username> <secret> <display name>{r}
                Authorises to the the server and sets the display name.

              {w}/join <channel id>{r}
                Joins channel on server.

              {w}/rename <display name>{r}
                Changes the display name.

              {w}/help{r}
                Shows this help.

            To exit press {w}Ctrl+C{r}.
            """
        );
    }

    static void RunRename(ReadOnlySpan<char> cmd)
    {
        client.DisplayName = cmd.ToString();
        if (nonStandard) {
            reader.PromptLength = client.DisplayName.Length + 2;
            reader.Prompt =
                $"{Term.brightYellow}{client.DisplayName}{Term.brightBlack}: "
                    + $"{Term.reset}";
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
            reader.Prompt =
                $"{Term.brightYellow}{client.DisplayName}{Term.brightBlack}: "
                    + $"{Term.reset}";
        }
    }

    static void Receive()
    {
        switch (client.Receive()) {
            case ErrMessage msg:
                if (nonStandard && !Console.IsErrorRedirected) {
                    reader.EWriteLine(
                        $"{Term.brightRed}ERR {Term.reset}FROM "
                            + $"{Term.brightMagenta}{msg.DisplayName}"
                            + $"{Term.brightBlack}: {Term.reset}{msg.Content}"
                    );
                } else {
                    reader.EWriteLine(
                        $"ERR FROM {msg.DisplayName}: {msg.Content}"
                    );
                }
                break;
            case ReplyMessage msg:
                if (msg.Ok) {
                    if (nonStandard && !Console.IsErrorRedirected) {
                        reader.EWriteLine(
                            $"{Term.brightGreen}Success: {Term.reset}"
                                + $"{msg.Content}"
                        );
                    } else {
                        reader.EWriteLine($"Success: {msg.Content}");
                    }
                } else {
                    if (nonStandard && !Console.IsErrorRedirected) {
                        reader.EWriteLine(
                            $"{Term.brightRed}Failure: {Term.reset}"
                                + $"{msg.Content}"
                        );
                    } else {
                        reader.EWriteLine($"Failure: {msg.Content}");
                    }
                }
                break;
            case MsgMessage msg:
                if (nonStandard && !Console.IsErrorRedirected) {
                    reader.WriteLine(
                        $"{Term.brightMagenta}{msg.Sender}{Term.brightBlack}: "
                            + $"{Term.brightWhite}{msg.Content}{Term.reset}"
                    );
                } else {
                    reader.WriteLine($"{msg.Sender}: {msg.Content}");
                }
                break;
        }
    }

    private static int Help(Args args)
    {
        var (sign, g, y, i, w, dgr, r) = !Console.IsInputRedirected
            ? (
                string.Concat("xstigl00".Select((p, i) => {
                    return Term.Prepare(
                        Term.fg, 250 - 10 * i, 50, 170 + 10 * i, p
                    );
                })),
                Term.brightGreen,
                Term.brightYellow,
                Term.italic,
                Term.brightWhite,
                Term.brightBlack,
                Term.reset
            ) : (
                "xstigl00",
                "",
                "",
                "",
                "",
                "",
                ""
            );

        Console.WriteLine(
            $"""
            Welcome to help for {i}{g}ip24chat-client{r} by {sign}.

            {g}Usage:
              {w}ipk24chat-cleint -h{r}
                Shows this help.

              {w}ipk24chat-client -t <protocol> -s <server address>
                                  {dgr}[flags]{r}
                Connects to the server address with the protocol.

            {g}Flags:
              {y}-t  --protocol {w}(tcp | udp){r}
                Selects the protocol.

              {y}-s  --address  --server {w}<server address>{r}
                Selects the chat server to connect to.

              {y}-p  --port {w}<port>{r}
                Selects the port to use when connecting to server. Default is
                4567

              {y}-d  --udp-timeout {w}<timeout>{r}
                Sets the udp confirmation timeout in milliseconds. Has no
                effect when using tcp. Default is 250.

              {y}-r  --udp-retransmitions {w}<count>{r}
                Sets the max number of retransimitions when using udp. Has no
                effect when using tcp.

              {y}-h  -?  --help{r}
                Prints this help.

              {y}-e  --extend  --non-standard{r}
                Enable non-standard features that would otherwise interfere
                with the specification.
            """
        );

        return 0;
    }
}
