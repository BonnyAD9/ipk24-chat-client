using System.Net;
using System.Text;

namespace IpkChat2024Client.Udp;

static class MessageParser
{
    public static (ushort id, object msg) Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 3)
        {
            throw new ProtocolViolationException(
                "Received invalid ipk message over udp: Message is too short"
            );
        }

        var type = (MessageType)data[0];
        data = data[1..];

        ushort id = ParseUshort(ref data);

        return (
            id,
            type switch
            {
                MessageType.Confirm => ParseConfirm(data, id),
                MessageType.Reply => ParseReply(data),
                MessageType.Msg => ParseMsg(data),
                MessageType.Err => ParseErr(data),
                MessageType.Bye => ParseBye(data),
                _ => throw new ProtocolViolationException(
                    "Ipk over udp received unsupported message type"
                ),
            }
        );
    }

    private static ConfirmMessage ParseConfirm(ReadOnlySpan<byte> data, ushort id)
    {
        ZeroLen(data);
        return new ConfirmMessage(id);
    }

    private static ReplyMessage ParseReply(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4) {
            throw new ProtocolViolationException(
                "Invalid reply message length: message is too short"
            );
        }

        var success = data[0] switch {
            0 => false,
            1 => true,
            _ => throw new ProtocolViolationException(
                $"Invalid result code '{data[0]}' in reply message"
            ),
        };
        data = data[1..];

        var refId = ParseUshort(ref data);
        var content = ParseString(ref data);
        ZeroLen(data);

        return new ReplyMessage(success, content, refId);
    }

    private static MsgMessage ParseMsg(ReadOnlySpan<byte> data)
    {
        var (name, content) = Parse2StrMsg(data);
        return new MsgMessage(name, content);
    }

    private static ErrMessage ParseErr(ReadOnlySpan<byte> data)
    {
        var (name, content) = Parse2StrMsg(data);
        return new ErrMessage(content, name);
    }

    private static ByeMessage ParseBye(ReadOnlySpan<byte> data)
    {
        ZeroLen(data);
        return new ByeMessage();
    }

    private static (string, string) Parse2StrMsg(ReadOnlySpan<byte> data)
    {
        var s1 = ParseString(ref data);
        var s2 = ParseString(ref data);
        ZeroLen(data);
        return (s1, s2);
    }

    private static string ParseString(ref ReadOnlySpan<byte> data)
    {
        var i = data.IndexOf((byte)0);
        if (i == -1) {
            throw new ProtocolViolationException("Missing string terminator");
        }
        var res = Encoding.ASCII.GetString(data[..i]);
        data = data[(i + 1)..];
        return res;
    }

    private static ushort ParseUshort(ref ReadOnlySpan<byte> data)
    {
        var res = (ushort)(data[0] << 8);
        res |= data[1];
        data = data[2..];
        return res;
    }

    private static void ZeroLen(ReadOnlySpan<byte> data)
    {
        if (data.Length != 0)
        {
            throw new ProtocolViolationException(
                "Received message with more data than expected"
            );
        }
    }
}
