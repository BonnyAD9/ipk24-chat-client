using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;

namespace IpkChat2024Client.Tcp;

public class ChatClient : IChatClient
{
    private TcpClient tcpClient = new TcpClient();

    public ChatClientState State { get; private set; }
        = ChatClientState.Stopped;

    private byte[]? authorizeMsg;

    private string? displayName;

    public string DisplayName
    {
        get => displayName ?? "name";
        set {
            ValidateDisplayName(value);
            displayName = value;
        }
    }

    public void Connect(IPAddress address, ushort port)
    {
        if (State != ChatClientState.Stopped)
        {
            throw new InvalidOperationException(
                "Cannot connect: Client is already connected"
            );
        }
        tcpClient.Connect(address, port);
        State = ChatClientState.Started;
    }

    public void Authorize(
        ReadOnlySpan<char> username,
        ReadOnlySpan<char> secret,
        string? displayName = null
    )
    {
        ValidateUsername(username);
        ValidateSecret(secret);

        if (displayName is not null)
        {
            DisplayName = DisplayName;
        }

        if (State != ChatClientState.Started
            && State != ChatClientState.Authorized
        )
        {
            throw new InvalidOperationException(State switch
                {
                    ChatClientState.Stopped
                        => "Cannot authorize: not connected to server",
                    _ => "Client is already authorized",
                }
            );
        }

        var stream = tcpClient.GetStream();
        ArrayBufferWriter<byte> writer = new();

        stream.Write("AUTH "u8);

        Encoding.ASCII.GetBytes(username, writer);
        stream.Write(writer.WrittenSpan);

        stream.Write(" USING "u8);

        writer.Clear();
        Encoding.ASCII.GetBytes(secret, writer);
        stream.Write(writer.WrittenSpan);

        stream.Write("\r\n"u8);

        stream.Flush();
    }

    public void Bye()
    {
        switch (State)
        {
            case ChatClientState.Stopped:
                throw new InvalidOperationException(
                    "Cannot say bye when not connected"
                );
            case ChatClientState.End:
                throw new InvalidOperationException(
                    "Cannot say bye: connection is alreaady closed"
                );
        }

        var stream = tcpClient.GetStream();
        stream.Write("BYE\r\n"u8);
        stream.Flush();

        State = ChatClientState.End;
    }

    public void Join(ReadOnlySpan<char> channelId)
    {
        ValidateChannel(channelId);

        if (State != ChatClientState.Open)
        {
            throw new InvalidOperationException(
                "Cannot join channel: Not authorized"
            );
        }

        var stream = tcpClient.GetStream();
        ArrayBufferWriter<byte> writer = new();

        stream.Write("JOIN "u8);

        Encoding.ASCII.GetBytes(channelId, writer);
        stream.Write(writer.WrittenSpan);

        stream.Write(" AS "u8);

        writer.Clear();
        Encoding.ASCII.GetBytes(DisplayName, writer);
        stream.Write(writer.WrittenSpan);

        stream.Write("\n\r"u8);
        stream.Flush();
    }

    public void Send(ReadOnlySpan<char> message)
    {
        ValidateMessage(message);

        if (State != ChatClientState.Open)
        {
            throw new InvalidOperationException(
                "Cannot send messages: Not authorized."
            );
        }

        var stream = tcpClient.GetStream();
        ArrayBufferWriter<byte> writer = new();

        stream.Write("MSG FROM "u8);

        Encoding.ASCII.GetBytes(DisplayName, writer);
        stream.Write(writer.WrittenSpan);

        stream.Write(" IS "u8);

        writer.Clear();
        Encoding.ASCII.GetBytes(message, writer);
        stream.Write(writer.WrittenSpan);

        stream.Flush();
    }

    public void Err(ReadOnlySpan<char> msg)
    {
        ValidateMessage(msg);

        var stream = tcpClient.GetStream();
        ArrayBufferWriter<byte> writer = new();

        stream.Write("ERROR FROM "u8);

        Encoding.ASCII.GetBytes(DisplayName, writer);
        stream.Write(writer.WrittenSpan);

        stream.Write(" IS "u8);

        writer.Clear();
        Encoding.ASCII.GetBytes(msg, writer);
        stream.Write(writer.WrittenSpan);

        stream.Write("\r\n"u8);

        State = ChatClientState.Error;

        Bye();
    }

    public List<string> Receive()
    {
        if (State == ChatClientState.Stopped)
        {
            throw new InvalidOperationException(
                "Cannot recieve: Client not connected"
            );
        }

        var stream = tcpClient.GetStream();
        if (!stream.DataAvailable)
        {
            return new();
        }

        List<string> msgs = new();

        switch (State)
        {
            case ChatClientState.Started:
                var msg = "Recieved unexpected message: Didn't expect any "
                    + "message.";
                Err(msg);
                throw new ProtocolViolationException(msg);
        }

        // TODO
        throw new NotImplementedException();
    }

    private static void ValidateUsername(ReadOnlySpan<char> username)
        => Validate(
            username,
            20,
            c => c == '-' || char.IsAsciiLetterOrDigit(c),
            "username",
            "ascii leter, digit or '-'"
        );

    private static void ValidateChannel(ReadOnlySpan<char> channel)
        => Validate(
            channel,
            20,
            c => c == '-' || char.IsAsciiLetterOrDigit(c),
            "channel id",
            "ascii leter, digit or '-'"
        );

    private static void ValidateSecret(ReadOnlySpan<char> secret) => Validate(
        secret,
        128,
        c => c == '-' || char.IsAsciiLetterOrDigit(c),
        "channel id",
        "ascii leter, digit or '-'"
    );

    private static void ValidateDisplayName(ReadOnlySpan<char> displayName)
        => Validate(
            displayName,
            20,
            c => c >= 0x21 && c <= 0x7E,
            "display name",
            "printable ascii characters"
        );

    private static void ValidateMessage(ReadOnlySpan<char> message)
        => Validate(
            message,
            1400,
            c => c >= 0x20 && c <= 0x7E,
            "message",
            "printable ascii characters"
        );

    private static void Validate(
        ReadOnlySpan<char> field,
        int maxLen,
        Func<char, bool> allowedChars,
        string name,
        string allowedDesc
    )
    {
        if (field.Length == 0) {
            throw new ArgumentException(
                $"Too short {name} (must be at least 1 character)."
            );
        }
        if (field.Length > maxLen) {
            throw new ArgumentException(
                $"Too long {name} (cannot be more than 20 characters long)."
            );
        }
        foreach (var c in field)
        {
            if (!allowedChars(c)) {
                throw new ArgumentException(
                    $"Invalid  {name}: It may contain only {allowedDesc}."
                );
            }
        }
    }
}
