using Ipk24ChatClient.Tcp;

namespace Tests.Tcp;

class TcpSender : ITcpClient
{
    private byte[] data;
    private int idx = 0;
    private List<byte> lastRecv;

    public ReadOnlySpan<byte> LastRecv => lastRecv.ToArray();

    public TcpSender(byte[] data)
    {
        this.data = data;
        this.lastRecv = [];
    }
    public bool DataAvailable => idx < data.Length;

    public void Close()
    {
        idx = data.Length;
    }

    public void Connect(string address, ushort port)
    {
        _ = address;
        _ = port;
    }

    public void Flush() {}

    public byte ReadByte() => data[idx++];

    public void Write(ReadOnlySpan<byte> data) =>
        lastRecv.AddRange(data);
}
