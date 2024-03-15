using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.Data;

namespace IpkChat2024Client.Tcp;

public class ChatClient : IChatClient
{
    private TcpClient tcpClient = new();

    public ChatClientState State { get; private set; }
        = ChatClientState.Stopped;

    private string? displayName;

    private MessageParser parser = new();

    public string DisplayName
    {
        get => displayName ?? "name";
        set {
            ValidateDisplayName(value);
            displayName = value;
        }
    }

    public void Connect(string address, ushort port)
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
            DisplayName = displayName;
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

        writer.Write("AUTH "u8);
        Encoding.ASCII.GetBytes(username, writer);

        writer.Write(" AS "u8);
        Encoding.ASCII.GetBytes(DisplayName, writer);

        writer.Write(" USING "u8);
        Encoding.ASCII.GetBytes(secret, writer);

        writer.Write("\r\n"u8);

        stream.Write(writer.WrittenSpan);
        stream.Flush();

        State = ChatClientState.Authorized;
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

        writer.Write("JOIN "u8);
        Encoding.ASCII.GetBytes(channelId, writer);

        writer.Write(" AS "u8);
        Encoding.ASCII.GetBytes(DisplayName, writer);

        writer.Write("\r\n"u8);

        stream.Write(writer.WrittenSpan);
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

        writer.Write("MSG FROM "u8);
        Encoding.ASCII.GetBytes(DisplayName, writer);

        writer.Write(" IS "u8);
        Encoding.ASCII.GetBytes(message, writer);

        writer.Write("\r\n"u8);

        stream.Write(writer.WrittenSpan);
        stream.Flush();
    }

    public void Err(ReadOnlySpan<char> msg)
    {
        ValidateMessage(msg);

        var stream = tcpClient.GetStream();
        ArrayBufferWriter<byte> writer = new();

        writer.Write("ERROR FROM "u8);
        Encoding.ASCII.GetBytes(DisplayName, writer);

        writer.Write(" IS "u8);
        Encoding.ASCII.GetBytes(msg, writer);

        writer.Write("\r\n"u8);

        stream.Write(writer.WrittenSpan);
        stream.Flush();

        State = ChatClientState.Error;

        Bye();
    }

    public object? Receive()
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
            return null;
        }

        switch (State)
        {
            case ChatClientState.Started or ChatClientState.Error:
                var msg = "Recieved unexpected message: Didn't expect any "
                    + "message.";
                Err(msg);
                throw new ProtocolViolationException(msg);
            case ChatClientState.Authorized:
                return ReceiveAuthorize();
            case ChatClientState.Open:
                return ReceiveOpen();
            case ChatClientState.End:
                return ReceiveEnd();
        }

        return null;
    }

    private object? ReceiveEnd() => ParseMessage();

    private object? ReceiveOpen()
    {
        switch (ParseMessage())
        {
            case null:
                return null;
            case MsgMessage msg:
                return msg;
            case ErrMessage msg:
                Bye();
                return msg;
            case ByeMessage msg:
                State = ChatClientState.End;
                return msg;
            default:
                const string err =
                    "Recieved unexpected message, expected MSG, ERR or BYE.";
                Err(err);
                throw new ProtocolViolationException(err);
        }
    }

    private object? ReceiveAuthorize()
    {
        switch (ParseMessage())
        {
            case null:
                return null;
            case ReplyMessage msg:
                if (msg.Ok)
                {
                    State = ChatClientState.Open;
                }
                return msg;
            case ErrMessage msg:
                Bye();
                return msg;
            default:
                const string err =
                    "Recieved unexpected message, expected REPLY or ERR.";
                Err(err);
                throw new ProtocolViolationException(err);
        }
    }

    private object? ParseMessage()
    {
        try {
            return parser.Parse(tcpClient.GetStream());
        } catch (InvalidDataException ex) {
            var msg = "Failed to parse message: ";// + ex.Message;
            Err(msg);
            throw new InvalidDataException(msg, ex);
        }
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
