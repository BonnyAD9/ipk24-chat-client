using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IpkChat2024Client.Udp;

class IpkUdpClient
{
    private readonly UdpClient client = new();
    private ushort idCounter = 0;
    private List<SentMessage> sent = [];
    public int MaxParalel { get; set; }
    private Queue<SentMessage> sendQueue = [];
    private Queue<object> recvQueue = [];
    private bool firstMsg = true;
    private ushort recvId;
    private List<(ushort id, object msg)> received = [];
    private IPAddress address = IPAddress.Any;

    public void Connect(string address, ushort port)
    {
        var adr = Dns.GetHostAddresses(address);
        if (adr.Length != 0)
        {
            this.address = adr[0];
        }
        client.Connect(address, port);
    }

    public void Flush() => Update();

    public object? Read()
    {
        Update();
        if (recvQueue.TryDequeue(out object? res)) {
            return res;
        }
        return null;
    }

    public void Close() => client.Close();

    public void SendErr(
        ReadOnlySpan<char> displayName, ReadOnlySpan<char> content
    ) => Send2StringMsg(MessageType.Err, displayName, content);

    public void SendMsg(
        ReadOnlySpan<char> displayName,
        ReadOnlySpan<char> content
    ) => Send2StringMsg(MessageType.Msg, displayName, content);

    public void SendBye()
    {
        var arr = new byte[3];
        var buf = arr.AsSpan();
        SetHeader(MessageType.Bye, ref buf);
        Send(arr);
    }

    private void SendConfirm(ushort id)
    {
        Span<byte> buf = stackalloc byte[3];

        buf[0] = (byte)MessageType.Confirm;
        buf[1] = (byte)(id >> 8);
        buf[1] = (byte)id;

        client.Send(buf);
    }

    public void SendAuth(
        ReadOnlySpan<char> username,
        ReadOnlySpan<char> displayName,
        ReadOnlySpan<char> secret
    ) {
        var maxLen = 6
            + username.Length
            + displayName.Length
            + secret.Length;

        byte[] arr = new byte[maxLen];
        var buf = arr.AsSpan();

        SetHeader(MessageType.Auth, ref buf);

        AddString(username, ref buf);
        AddString(displayName, ref buf);
        AddString(secret, ref buf);

        SendEx(arr, buf.Length);
    }

    public void SendJoin(
        ReadOnlySpan<char> channel,
        ReadOnlySpan<char> displayName
    ) => Send2StringMsg(MessageType.Join, channel, displayName);

    private void Send2StringMsg(
        MessageType type,
        ReadOnlySpan<char> s1,
        ReadOnlySpan<char> s2
    ) {
        var maxLen = 5 + s1.Length + s2.Length;
        byte[] arr = new byte[maxLen];
        var buf = arr.AsSpan();

        SetHeader(type, ref buf);

        AddString(s1, ref buf);
        AddString(s2, ref buf);

        SendEx(arr, buf.Length);
    }

    private void AddString(ReadOnlySpan<char> str, ref Span<byte> res)
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
        sendQueue.Enqueue(new(idCounter, msg, len));
        ++idCounter;
    }

    public void Update()
    {
        Recv();
        Send();
    }

    private void Recv()
    {
        while (client.Available != 0)
        {
            var endpoint = new IPEndPoint(IPAddress.Any, 0);
            var data = client.Receive(ref endpoint);
            int i;
            var (id, m) = ParseMsg(data);
            SendConfirm(id);

            switch (m)
            {
                case ConfirmMessage msg:
                    i = sent.FindIndex(p => p.Id == msg.Id);
                    if (i != -1)
                    {
                        sent.RemoveAt(i);
                    }
                    break;
                case ReplyMessage msg:
                    i = sent.FindIndex(p => p.Id == msg.RefId);
                    if (i != -1)
                    {
                        sent.RemoveAt(i);
                    }
                    received.Add((id, msg));
                    break;
                default:
                    received.Add((id, m));
                    break;
            }
        }

        var cont = true;
        while (cont)
        {
            cont = false;
            for (int i = 0; i < received.Count; ++i)
            {
                if (received[i].id == recvId || firstMsg)
                {
                    recvQueue.Enqueue(received[i].msg);
                    received.RemoveAt(i);
                    cont = true;
                    firstMsg = false;
                    break;
                }
            }
        }
    }

    private void Send()
    {
        while (sent.Count <= MaxParalel)
        {
            var msg = sendQueue.Dequeue();
            client.Send(msg.Msg.AsSpan(..msg.Length));
            msg.Time = DateTime.Now;
            sent.Add(msg);
        }
    }

    private (ushort, object) ParseMsg(ReadOnlySpan<byte> data)
    {
        if (data.Length < 3) {
            throw new ProtocolViolationException(
                "Received invalid ipk message over udp: Message is too short"
            );
        }

        var type = (MessageType)data[0];
        data = data[1..];

        ushort id = ParseUshort(ref data);

        return (id, type switch
        {
            MessageType.Confirm => ParseConfirm(data, id),
            MessageType.Reply => ParseReply(data, id),
            MessageType.Msg => ParseMsgMsg(data, id),
            MessageType.Err => ParseErrMsg(data, id),
            MessageType.Bye => ParseByeMsg(data, id),
            _ => throw new DataException(
                "Ipk over udp received unsupported message type"
            ),
        });
    }

    private ConfirmMessage ParseConfirm(ReadOnlySpan<byte> data, ushort id)
    {
        ZeroLen(data);
        return new ConfirmMessage(id);
    }

    private ReplyMessage ParseReply(ReadOnlySpan<byte> data, ushort id)
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

    private MsgMessage ParseMsgMsg(ReadOnlySpan<byte> data, ushort id)
    {
        var (name, content) = Parse2StrMsg(data);
        return new MsgMessage(name, content);
    }

    private ErrMessage ParseErrMsg(ReadOnlySpan<byte> data, ushort id)
    {
        var (name, content) = Parse2StrMsg(data);
        return new ErrMessage(content, name);
    }

    private ByeMessage ParseByeMsg(ReadOnlySpan<byte> data, ushort id)
    {
        ZeroLen(data);
        return new ByeMessage();
    }

    private (string, string) Parse2StrMsg(ReadOnlySpan<byte> data)
    {
        var s1 = ParseString(ref data);
        var s2 = ParseString(ref data);
        ZeroLen(data);
        return (s1, s2);
    }

    private ushort ParseUshort(ref ReadOnlySpan<byte> data)
    {
        var res = (ushort)(data[0] >> 8);
        res |= data[1];
        data = data[2..];
        return res;
    }

    private string ParseString(ref ReadOnlySpan<byte> data)
    {
        var i = data.IndexOf((byte)0);
        if (i == -1) {
            throw new ProtocolViolationException("Missing string terminator");
        }
        var res = Encoding.ASCII.GetString(data[..i]);
        data = data[(i + 1)..];
        return res;
    }

    private void ZeroLen(ReadOnlySpan<byte> data)
    {
        if (data.Length != 0)
        {
            throw new ProtocolViolationException(
                "Received message with more data than expected"
            );
        }
    }
}
