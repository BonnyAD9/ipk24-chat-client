using System.Diagnostics;
using System.Net;
using Ipk24ChatClient.Udp;

namespace Tests.Udp;

class UdpClient : IUdpClient
{
    private byte[][] data;
    private int idx = 0;
    public byte[]? LastRecv { get; private set; } = null;

    public UdpClient(byte[][] data)
    {
        this.data = data;
    }

    public int Available => idx < data.Length ? data[idx].Length : 0;

    public void Close() => idx = data.Length;

    public byte[] Receive(ref IPEndPoint endPoint) => data[idx++];

    public void Send(ReadOnlySpan<byte> msg, IPEndPoint endPoint) =>
        LastRecv = msg.ToArray();
}
