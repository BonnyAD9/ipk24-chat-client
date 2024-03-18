using System.Net.Sockets;

namespace IpkChat2024Client.Udp;

class ChatClient : IChatClient
{
    public string DisplayName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public void Authorize(ReadOnlySpan<char> username, ReadOnlySpan<char> secret, string? displayName = null)
    {

    }

    public void Bye()
    {
        throw new NotImplementedException();
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
