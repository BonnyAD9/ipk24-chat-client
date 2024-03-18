using System.Net.Sockets;
using System.Text;

namespace IpkChat2024Client.Udp;

class IpkUdpClient
{
    private readonly UdpClient client = new();
    private ushort idCounter = 0;
    private List<SentMessage> sent = [];

    public void Connect(string address, ushort port)
    {
        client.Connect(address, port);
    }

    public void Send(ErrMessage msg) =>
        Send2StringMsg(MessageType.Err, msg.DisplayName, msg.Content);

    public void Send(MsgMessage msg) =>
        Send2StringMsg(MessageType.Msg, msg.Sender, msg.Content);

    public void Send(ByeMessage msg)
    {
        var arr = new byte[3];
        var buf = arr.AsSpan();
        SetHeader(MessageType.Bye, ref buf);
        Send(arr);
    }

    private void SendConfirm(ushort id)
    {
        var arr = new byte[3];
        Span<byte> buf = arr.AsSpan();

        buf[0] = (byte)MessageType.Confirm;
        buf[1] = (byte)(id >> 8);
        buf[1] = (byte)id;

        Send(arr);
    }

    public void Send(AuthMessage msg)
    {
        var maxLen = 6
            + msg.Username.Length
            + msg.DisplayName.Length
            + msg.Secret.Length;

        byte[] arr = new byte[maxLen];
        var buf = arr.AsSpan();

        SetHeader(MessageType.Auth, ref buf);

        AddString(msg.Username, ref buf);
        AddString(msg.DisplayName, ref buf);
        AddString(msg.Secret, ref buf);

        SendEx(arr, buf.Length);
    }

    public void Send(JoinMessage msg) =>
        Send2StringMsg(MessageType.Join, msg.Channel, msg.DisplayName);

    private void Send2StringMsg(MessageType type, string s1, string s2)
    {
        var maxLen = 5 + s1.Length + s2.Length;
        byte[] arr = new byte[maxLen];
        var buf = arr.AsSpan();

        SetHeader(type, ref buf);

        AddString(s1, ref buf);
        AddString(s2, ref buf);

        SendEx(arr, buf.Length);
    }

    private void AddString(string str, ref Span<byte> res)
    {
        var i = Encoding.ASCII.GetBytes(str, res);
        res[i] = 0;
        res = res[(i + 1)..];
    }

    private void SetHeader(MessageType type, ref Span<byte> res)
    {
        res[0] = (byte)type;
        res[1] = (byte)(idCounter >> 8);
        res[2] = (byte)idCounter;
        res = res[3..];
    }

    private void SendEx(byte[] msg, int len) => Send(msg, msg.Length - len);

    private void Send(byte[] msg) => Send(msg, msg.Length);

    private void Send(byte[] msg, int len)
    {
        client.Send(msg.AsSpan(..len));
        sent.Add(new(idCounter, msg, len));
        ++idCounter;
    }
}
