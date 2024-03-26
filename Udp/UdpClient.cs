using System.Net;
using Ipk24ChatClient.Udp;
using Client = System.Net.Sockets.UdpClient;

class UdpClient : IUdpClient
{
    private Client client = new();

    public int Available => client.Available;

    public void Close() => client.Close();

    public byte[] Receive(ref IPEndPoint endPoint) =>
        client.Receive(ref endPoint);

    public void Send(ReadOnlySpan<byte> msg, IPEndPoint endPoint) =>
        client.Send(msg, endPoint);
}
