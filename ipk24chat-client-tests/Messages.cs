using System.Text;

namespace Ipk24ChatClientTester;

record class AuthMsg(
    ushort Id,
    string Username,
    string DisplayName,
    string Secret
) {
    public override string ToString() =>
        $"{Id}: AUTH {Username} AS {DisplayName} USING {Secret}";

    public byte[] ToTcp() => Encoding.ASCII.GetBytes(
        $"AUTH {Username} AS {DisplayName} USING {Secret}\r\n"
    );

    public byte[] ToUdp() => Serializer.MakeMsg(
        MessageType.Auth,
        Id,
        Username,
        DisplayName,
        Secret
    );
}

record class JoinMsg(ushort Id, string ChannelID, string DisplayName)
{
    public override string ToString() =>
        $"{Id}: JOIN {ChannelID} AS {DisplayName}";

    public byte[] ToTcp() => Encoding.ASCII.GetBytes(
        $"JOIN {ChannelID} AS {DisplayName}\r\n"
    );

    public byte[] ToUdp() =>
        Serializer.MakeMsg(MessageType.Auth, Id, ChannelID, DisplayName);
}

record class ErrMsg(ushort Id, string DisplayName, string MessageContent)
{
    public override string ToString() =>
        $"{Id}: ERR FROM {DisplayName} IS {MessageContent}";

    public byte[] ToTcp() => Encoding.ASCII.GetBytes(
        $"ERR FROM {DisplayName} IS {MessageContent}\r\n"
    );

    public byte[] ToUdp() =>
        Serializer.MakeMsg(MessageType.Err, Id, DisplayName, MessageContent);
}

record class ByeMsg(ushort Id)
{
    public override string ToString() => $"{Id}: BYE";

    public byte[] ToTcp() => Encoding.ASCII.GetBytes($"BYE\r\n");

    public byte[] ToUdp() => Serializer.MakeMsg(MessageType.Bye, Id);
}

record class MsgMsg(ushort Id, string DisplayName, string MessageContent)
{
    public override string ToString() =>
        $"{Id}: MSG FROM {DisplayName} IS {MessageContent}";

    public byte[] ToTcp() => Encoding.ASCII.GetBytes(
        $"MSG FROM {DisplayName} IS {MessageContent}\r\n"
    );

    public byte[] ToUdp() =>
        Serializer.MakeMsg(MessageType.Msg, Id, DisplayName, MessageContent);
}

record class ReplyMsg(ushort Id, bool Ok, string MessageContent, ushort RefId)
{
    public override string ToString() =>
        $"{Id}->{RefId}: REPLY {(Ok ? "OK" : "NOK")} IS {MessageContent}";

    public byte[] ToTcp() => Encoding.ASCII.GetBytes(
        $"REPLY {(Ok ? "OK" : "NOK")} OS {MessageContent}\r\n"
    );

    public byte[] ToUdp()
    {
        List<byte> res = new();
        Serializer.MakeHeader(res, MessageType.Reply, Id);
        res.AddRange([
            Ok ? (byte)1 : (byte)0,
            (byte)(RefId >> 8),
            (byte)RefId
        ]);
        Serializer.AddString(res, MessageContent);
        return res.ToArray();
    }
}

record class ConfirmMsg(ushort RefId)
{
    public override string ToString() =>
        $"->{RefId}: CONFIRM";

    public byte[] ToTcp() => [];

    public byte[] ToUdp() => Serializer.MakeMsg(MessageType.Confirm, RefId);
}

static class Serializer
{
    public static byte[] MakeMsg(MessageType type, ushort id, params string[] strings)
    {
        List<byte> res = new();

        MakeHeader(res, type, id);
        foreach (var s in strings)
        {
            AddString(res, s);
        }

        return res.ToArray();
    }

    public static void MakeHeader(List<byte> res, MessageType type, ushort id)
    {
        res.AddRange([
            (byte)type,
            (byte)(id >> 8),
            (byte)id,
        ]);
    }

    public static void AddString(List<byte> res, string str)
    {
        res.AddRange(Encoding.ASCII.GetBytes(str));
        res.Add(0);
    }
}
