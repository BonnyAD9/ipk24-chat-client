using System.Net;
using System.Net.Sockets;
using IpkChat2024Client.Tcp;

namespace IpkChat2024Client.Udp;

class ChatClient : IChatClient
{
    private IpkUdpClient client = new();
    private ChatClientState state = ChatClientState.Stopped;

    private string displayName = "";
    public string DisplayName
    {
        get => displayName;
        set
        {
            Validators.DisplayName(value);
            displayName = value;
        }
    }

    public void Connect(string address, ushort port)
    {
        if (state != ChatClientState.Stopped)
        {
            throw new InvalidOperationException(
                "Cannot connect: Client already connected."
            );
        }
        client.Connect(address, port);
        state = ChatClientState.Started;
    }

    public void Authorize(ReadOnlySpan<char> username, ReadOnlySpan<char> secret, string? displayName = null)
    {
        if (state != ChatClientState.Started)
        {
            throw new InvalidOperationException(state switch
            {
                ChatClientState.Stopped =>
                    "Cannot authorize: not connected to server",
                _ => "Client already authorized.",
            });
        }

        Validators.Username(username);
        Validators.Secret(secret);

        if (displayName is not null)
        {
            DisplayName = displayName;
        }

        client.SendAuth(username, displayName, secret);
        client.Flush();

        state = ChatClientState.Authorizing;
    }

    public void Bye()
    {
        switch (state)
        {
            case ChatClientState.Stopped:
                throw new InvalidOperationException(
                    "Cannot say bye when not connected."
                );
            case ChatClientState.End:
                throw new InvalidOperationException(
                    "Cannot say bye: connection is already closed"
                );
        }

        client.SendBye();
        client.Flush();

        state = ChatClientState.End;
    }

    public void Join(ReadOnlySpan<char> channelId)
    {
        if (state != ChatClientState.Open)
        {
            throw new InvalidOperationException(
                "Cannot join channel: Not authorized"
            );
        }

        Validators.Channel(channelId);

        client.SendJoin(channelId, DisplayName);
        client.Flush();
    }

    public void Send(ReadOnlySpan<char> message)
    {
        if (state != ChatClientState.Open)
        {
            throw new InvalidOperationException(
                "Cannot send messages: Not authorized."
            );
        }

        Validators.Message(message);

        client.SendMsg(DisplayName, message);
        client.Flush();
    }

    public void Err(ReadOnlySpan<char> msg)
    {
        Validators.Message(msg);

        client.SendErr(DisplayName, msg);
        client.Flush();

        state = ChatClientState.Error;
        Bye();
    }

    public object? Receive()
    {
        if (state == ChatClientState.Stopped)
        {
            throw new InvalidOperationException(
                "Cannot recieve: Client not connected"
            );
        }

        var msg = client.Read();
        if (msg is null)
        {
            return null;
        }

        switch (state)
        {
            case ChatClientState.Started or ChatClientState.Error:
                var res = "Recieved unexpected message: Didn't expect any "
                    + "message.";
                Err(res);
                throw new ProtocolViolationException(res);
            case ChatClientState.Authorizing:
                return ReceiveAuthorize(msg);
            case ChatClientState.Open:
                return ReceiveOpen(msg);
            case ChatClientState.End:
                return ReceiveEnd(msg);
        }

        return null;
    }

    private object? ReceiveAuthorize(object m)
    {
        switch (m)
        {
            case ReplyMessage msg:
                if (msg.Ok)
                {
                    state = ChatClientState.Open;
                    return msg;
                }
                state = ChatClientState.Started;
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

    private object? ReceiveOpen(object m)
    {
        switch (m)
        {
            case null:
                return null;
            case MsgMessage msg:
                return msg;
            case ErrMessage msg:
                Bye();
                return msg;
            case ByeMessage msg:
                state = ChatClientState.End;
                return msg;
            case ReplyMessage msg:
                return msg;
            default:
                const string err =
                    "Recieved unexpected message, expected MSG, ERR or BYE.";
                Err(err);
                throw new ProtocolViolationException(err);
        }
    }

    private object? ReceiveEnd(object msg) => msg;
}
