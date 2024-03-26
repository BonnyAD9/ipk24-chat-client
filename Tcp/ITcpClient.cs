using System.Buffers;

namespace Ipk24ChatClient.Tcp;

public interface ITcpClient
{
    public bool DataAvailable { get; }

    public void Connect(string address, ushort port);
    public void Write(ReadOnlySpan<byte> data);
    public byte ReadByte();
    public void Flush();
    public void Close();
}
