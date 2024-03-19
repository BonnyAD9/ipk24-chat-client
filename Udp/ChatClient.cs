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
        state = ChatClientState.End;
    }

    public void Join(ReadOnlySpan<char> channelId)
    {
        throw new NotImplementedException();
    }

    public object? Receive()
    {
        throw new NotImplementedException();
    }

    public void Send(ReadOnlySpan<char> message)
    {
        throw new NotImplementedException();
    }
}
