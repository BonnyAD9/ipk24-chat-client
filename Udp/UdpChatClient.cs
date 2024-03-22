using System.Net;
using System.Net.Sockets;
using IpkChat2024Client.Cli;

namespace IpkChat2024Client.Udp;

public class UdpChatClient : ChatClient
{
    private readonly UdpClient client = new();

    private IPEndPoint server = new(IPAddress.Any, 4567);
    private TimeSpan timeout = TimeSpan.FromMilliseconds(250);
    private int maxResend = 3;

    /// <summary>
    /// Id of client messages that is incremented with each message.
    /// </summary>
    private ushort myId = 0;
    /// <summary>
    /// Id of server messages that is expected to increment with each message.
    /// </summary>
    private ushort serverId;
    /// <summary>
    /// True until the first message with different port is received.
    /// </summary>
    private bool firstMsg = true;

    /// <summary>
    /// Messages received out of order.
    /// </summary>
    private List<(ushort id, object msg)> received = [];
    /// <summary>
    /// Sent messages waiting to be confirmed.
    /// </summary>
    private List<SentMessage> sent = [];
    /// <summary>
    /// Maximum number of unconfirmed messages to sent before waiting for
    /// confirmation.
    /// </summary>
    public int MaxParalel { get; set; } = 1;

    /// <summary>
    /// Messages waiting to be sent in order.
    /// </summary>
    private Queue<SentMessage> sendQueue = [];
    /// <summary>
    /// Received messages in the correct order.
    /// </summary>
    private Queue<object> recvQueue = [];

    protected override void Init(Args args)
    {
        timeout = args.UdpConfirmationTimeout;
        maxResend = args.MaxUdpRetransmitions;
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

    /// <summary>
    /// Process all pending received messages
    /// </summary>
    private void Recv()
    {
        // Read all messages
        while (client.Available != 0)
        {
            // listen on all ports when we expect the server port to change
            if (firstMsg) {
                server.Port = 0;
            }

            var data = client.Receive(ref server);
            var (id, msg) = MessageParser.Parse(data);
            // immidietely confirm the message
            SendConfirm(id);

            // swap the endianness of the ID, if it seems that it is incorrect
            // because there is bug in the reference server that it sends the
            // id in LE instead of BE.
            var id2 = (ushort)((id << 8) | (id >> 8));
            if (Math.Abs(id2 - serverId) < Math.Abs(id - serverId)) {
                id = id2;
            }

            MsgAction(id, msg);
        }

        PushReceived();
    }

    /// <summary>
    /// Resend remeouted messages and send new pending messages.
    /// </summary>
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

    /// <summary>
    /// Do action based on the received message.
    /// </summary>
    /// <param name="id">Id of the received message.</param>
    /// <param name="m">The received message.</param>
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

        sent[idx] = msg;
        client.Send(msg.Msg.AsSpan(..msg.Length), server);
    }

    /// <summary>
    /// Remove message withe the given id from the unconfirmed sent messages
    /// because it has been confirmed by the server.
    /// </summary>
    /// <param name="id">id of the message that has been confirmed</param>
    private void ConfirmSent(ushort id)
    {
        int i = sent.FindIndex(p => p.Id == id);
        if (i != -1)
        {
            sent.RemoveAt(i);
        }
    }

    /// <summary>
    /// Move messages that are in order from received to recvQueue.
    /// </summary>
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

    /// <summary>
    /// Queue message to be sent.
    /// </summary>
    /// <param name="msg">Message to add to the queue.</param>
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
