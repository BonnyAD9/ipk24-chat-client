using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;
using IpkChat2024Client.Cli;

namespace IpkChat2024Client.Udp;

public class UdpChatClient : ChatClient
{
    private readonly UdpClient client = new();

    private IPEndPoint server = new IPEndPoint(IPAddress.Any, 4567);
    private TimeSpan timeout = TimeSpan.FromMilliseconds(250);
    private int maxResend = 4;

    private ushort myId = 0;
    private ushort serverId;
    private bool firstMsg = true;

    private List<(ushort id, object msg)> received = [];
    private List<SentMessage> sent = [];
    public int MaxParalel { get; set; }

    private Queue<SentMessage> sendQueue = [];
    private Queue<object> recvQueue = [];

    protected override void Init(Args args)
    {
        server.Address = Dns
            .GetHostEntry(args.Address!)
            .AddressList
            .First(p => p.AddressFamily == AddressFamily.InterNetwork);
        server.Port = args.Port;
    }

    protected override void SendAuthorize(
        ReadOnlySpan<char> username,
        ReadOnlySpan<char> secret
    ) => QueueToSend(
        SentMessage.Authorize(myId, username, secret, DisplayName)
    );

    protected override void SendBye() => QueueToSend(SentMessage.Bye(myId));

    protected override void SendJoin(ReadOnlySpan<char> channel) =>
        QueueToSend(SentMessage.Join(myId, channel, DisplayName));

    protected override void SendMsg(ReadOnlySpan<char> content) =>
        QueueToSend(SentMessage.MakeMsg(myId, DisplayName, content));

    protected override void SendErr(ReadOnlySpan<char> content) =>
        QueueToSend(SentMessage.Err(myId, DisplayName, content));

    protected override object? TryReceive()
    {
        Update();

        if (recvQueue.TryDequeue(out object? res)) {
            return res;
        }
        return null;
    }

    protected override void Flush() => Update();

    protected override void Close() => client.Close();

    public void Update()
    {
        Recv();
        Send();
    }

    private void Recv()
    {
        while (client.Available != 0)
        {
            if (firstMsg) {
                server.Port = 0;
            }

            var data = client.Receive(ref server);
            var (id, msg) = MessageParser.Parse(data);
            SendConfirm(id);

            // swap the endianness of the ID because there is a bug in the
            // reference server that sends the id in LE instead of BE
            id = (ushort)((id << 8) | (id >> 8));

            MsgAction(id, msg);
        }

        PushReceived();
    }

    private void Send()
    {
        ResendTimeouted();

        while (sent.Count <= MaxParalel && sendQueue.Count > 0)
        {
            var msg = sendQueue.Dequeue();
            client.Send(msg.Msg.AsSpan(..msg.Length), server);
            msg.Time = DateTime.Now;
            sent.Add(msg);
        }
    }

    private void MsgAction(ushort id, object m)
    {
        switch (m)
        {
            case ConfirmMessage msg:
                ConfirmSent(msg.Id);
                break;
            case ReplyMessage msg:
                ConfirmSent(msg.RefId);
                received.Add((id, msg));
                break;
            default:
                received.Add((id, m));
                break;
        }
    }

    private void ResendTimeouted()
    {
        for (int i = 0; i < sent.Count; ++i)
        {
            if (DateTime.Now - sent[i].Time > timeout)
            {
                ResendMsgAt(i);
            }
        }
    }

    private void ResendMsgAt(int idx)
    {
        var msg = sent[idx];
        msg.Resend++;

        if (msg.Resend > maxResend)
        {
            sent.RemoveAt(idx);
            throw new TimeoutException(
                "Failed to send udp message: didn't receive response"
            );
        }
        else
        {
            sent[idx] = msg;
            client.Send(msg.Msg.AsSpan(..msg.Length), server);
        }
    }

    private void ConfirmSent(ushort id)
    {
        int i = sent.FindIndex(p => p.Id == id);
        if (i != -1)
        {
            sent.RemoveAt(i);
        }
    }

    private void PushReceived()
    {
        var cont = true;
        while (cont)
        {
            cont = false;
            for (int i = 0; i < received.Count; ++i)
            {
                if (received[i].id == serverId || firstMsg)
                {
                    if (firstMsg) {
                        serverId = received[i].id;
                    }
                    ++serverId;
                    recvQueue.Enqueue(received[i].msg);
                    received.RemoveAt(i);
                    cont = true;
                    firstMsg = false;
                    break;
                }
            }
        }
    }

    private void QueueToSend(SentMessage msg)
    {
        sendQueue.Enqueue(msg);
        ++myId;
    }

    private void SendConfirm(ushort id)
    {
        Span<byte> buf = [
            (byte)MessageType.Confirm,
            (byte)(id >> 8),
            (byte)id
        ];
        client.Send(buf, server);
    }
}
