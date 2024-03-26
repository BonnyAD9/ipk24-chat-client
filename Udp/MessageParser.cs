using System.Net;
using System.Text;

namespace Ipk24ChatClient.Udp;

/// <summary>
/// Used to parse binary UDP messages for the IPK protocol
/// </summary>
public static class MessageParser
{
    /// <summary>
    /// Parse the given message.
    /// </summary>
    /// <param name="data">Message to parse.</param>
    /// <returns>Tuple of id of the message and the message itself</returns>
    /// <exception cref="ProtocolViolationException">
    /// For invalid data.
    /// </exception>
    public static (ushort id, object msg) Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 3)
        {
            throw new ProtocolViolationException(
                "Received invalid ipk message over udp: Message is too short"
            );
        }

        // Parse the common data
        var type = (MessageType)data[0];
        data = data[1..];

        ushort id = ParseUshort(ref data);

        try
        {
            // Parse based on the type of the message
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
        catch (Exception ex)
        {
            throw new UdpMessageParseException(ex, id);
        }
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

    /// <summary>
    /// Parses message that consists of two strings.
    /// </summary>
    /// <param name="data">Data to parse</param>
    /// <returns>The two strings of the message</returns>
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

    /// <summary>
    /// Reads ushort from data in BE byte order.
    /// </summary>
    /// <param name="data">Data to read from</param>
    /// <returns>Readed ushort</returns>
    private static ushort ParseUshort(ref ReadOnlySpan<byte> data)
    {
        var res = (ushort)(data[0] << 8);
        res |= data[1];
        data = data[2..];
        return res;
    }

    /// <summary>
    /// Throws when data length is not 0.
    /// </summary>
    /// <param name="data">Data to check.</param>
    /// <exception cref="ProtocolViolationException">
    /// For non 0 data length
    /// </exception>
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
