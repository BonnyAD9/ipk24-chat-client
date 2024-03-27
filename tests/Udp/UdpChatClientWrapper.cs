using Ipk24ChatClient.Udp;

namespace Tests.Udp;

class UdpChatClientWrapper : UdpChatClient
{
    public UdpChatClientWrapper(IUdpClient client) : base(client) {}

    public object? WrapReceive() => TryReceive();
}
