using System.Net;

namespace Ipk24ChatClient.Udp;

public interface IUdpClient
{
    public int Available { get; }

    public void Close();
    public byte[] Receive(ref IPEndPoint endPoint);
    public void Send(ReadOnlySpan<byte> msg, IPEndPoint endPoint);
}
