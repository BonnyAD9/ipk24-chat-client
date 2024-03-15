using IpkChat2024Client.Tcp;
using TcpChatClient = IpkChat2024Client.Tcp.ChatClient;

namespace IpkChat2024Client;

static class Program
{

    static readonly TimeSpan sleepTime = TimeSpan.FromMilliseconds(10);
    static ConsoleReader reader = new();
    static IChatClient client = null!;

    public static void Main(string[] args)
    {
        TcpChatClient chat = new();
        Console.Write("Conneting... ");
        chat.Connect("anton5.fit.vutbr.cz", 4567);
        client = chat;

        if (!Console.IsInputRedirected) {
            Console.TreatControlCAsInput = true;
        }

        Console.WriteLine("Done!");
        reader.Init();

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

            if (line is not null)
            {
                try
                {
                    RunCommand(line);
                }
                catch (Exception ex)
                {
                    reader.EWriteLine($"ERROR: {ex.Message}");
                }
            }

            try
            {
                Receive();
            }
            catch (Exception ex)
            {
                reader.EWriteLine($"ERROR: {ex.Message}");
            }

            Thread.Sleep(sleepTime);
        }

        try
        {
            client.Bye();
        }
        catch (Exception ex)
        {
            reader.EWriteLine($"ERROR: {ex.Message}");
        }
    }

    static void RunCommand(string c)
    {
        if (!c.StartsWith('/'))
        {
            client.Send(c);
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
        reader.WriteLine("Help:");
    }

    static void RunRename(ReadOnlySpan<char> cmd)
    {
        client.DisplayName = cmd.ToString();
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
    }

    static void Receive()
    {
        switch (client.Receive()) {
            case ErrMessage msg:
                reader.EWriteLine(
                    $"ERR FROM {msg.DisplayName}: {msg.Content}"
                );
                break;
            case ReplyMessage msg:
                if (msg.Ok) {
                    reader.EWriteLine($"Success: {msg.Content}");
                } else {
                    reader.EWriteLine($"Failure: {msg.Content}");
                }
                break;
            case MsgMessage msg:
                reader.WriteLine($"{msg.Sender}: {msg.Content}");
                break;
        }
    }
}
