using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Text;

namespace IpkChat2024Client.Tcp;

record class ErrMessage(string Content, string DisplayName);

record class ReplyMessage(bool Type, string Content);

record class MsgMessage(string Sender, string Content);

record class ByeMessage;

class MessageParser
{
    private ArrayBufferWriter<byte> readed = new();

    public object? Parse(NetworkStream s)
    {
        if (!ReadMsg(s)) {
            return null;
        }

        var msg = Encoding.ASCII.GetString(readed.WrittenSpan).AsSpan();
        if (msg.StartsWith("ERROR "))
        {
            return ParseErr(msg);
        }
        if (msg.StartsWith("REPLY "))
        {
            return ParseReply(msg);
        }
        if (msg.StartsWith("MSG "))
        {
            return ParseMsg(msg);
        }
        if (msg.StartsWith("BYE"))
        {
            return ParseBye(msg);
        }

        throw new InvalidDataException("Received unknown message type");
    }

    private bool ReadMsg(NetworkStream s)
    {
        while (s.DataAvailable && !IsWholeMessage)
        {
            var b = (byte)s.ReadByte();
            readed.Write(new(ref b));
        }

        return IsWholeMessage;
    }

    private bool IsWholeMessage
        => readed.WrittenSpan[^2..].SequenceEqual("\r\n"u8);

    private ErrMessage ParseErr(ReadOnlySpan<char> msg)
    {
        msg = msg["ERROR ".Length..];

        var name = ParseFromName(ref msg);
        var content = ParseIsContent(msg);

        return new(content.ToString(), name.ToString());
    }

    private ReplyMessage ParseReply(ReadOnlySpan<char> msg)
    {
        [DoesNotReturn]
        void Throw()
        {
            throw new InvalidDataException(
                "The received reply message has wrong signature"
            );
        }

        msg = msg["REPLY ".Length..];
        bool ok;
        const string okMsg = "OK ";
        const string nokMsg = "NOK ";

        if (msg.StartsWith(okMsg))
        {
            msg = msg[okMsg.Length..];
            ok = true;
        } else if (msg.StartsWith(nokMsg))
        {
            msg = msg[nokMsg.Length..];
            ok = false;
        } else
        {
            throw new InvalidDataException("Invalid reply type.");
        }

        return new(ok, ParseIsContent(msg).ToString());
    }

    private MsgMessage ParseMsg(ReadOnlySpan<char> msg)
    {
        msg = msg["MSG ".Length..];
        var name = ParseFromName(ref msg);
        var content = ParseIsContent(msg);

        return new MsgMessage(name.ToString(), content.ToString());
    }

    private ByeMessage ParseBye(ReadOnlySpan<char> msg)
    {
        if (!msg.SequenceEqual("BYE\r\n"))
        {
            throw new InvalidDataException("Invalid bye message.");
        }

        return new();
    }

    private ReadOnlySpan<char> ParseFromName(ref ReadOnlySpan<char> msg)
    {
        [DoesNotReturn]
        void Throw()
        {
            throw new InvalidDataException(
                "The received message has invalid sender."
            );
        }

        const string msgFrom = "FROM ";
        if (!msg.StartsWith(msgFrom))
        {
            Throw();
        }

        msg = msg[msgFrom.Length..];
        var nameEnd = msg.IndexOf(' ');
        if (nameEnd == -1)
        {
            Throw();
        }

        var name = msg[..nameEnd];
        msg = msg[(nameEnd + 1)..];
        return name;
    }

    private ReadOnlySpan<char> ParseIsContent(ReadOnlySpan<char> msg)
    {
        const string isMsg = "IS ";
        if (!msg.StartsWith(isMsg))
        {
            throw new InvalidDataException(
                "The received message has invalid content."
            );
        }

        return msg[isMsg.Length..^2];
    }
}
