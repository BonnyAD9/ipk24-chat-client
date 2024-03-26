using Client =  System.Net.Sockets.TcpClient;

namespace Ipk24ChatClient.Tcp;

public class TcpClient : ITcpClient
{
    private Client client = new();

    public bool DataAvailable => client.GetStream().DataAvailable;

    public void Connect(string address, ushort port) =>
        client.Connect(address, port);

    public byte ReadByte() => (byte)client.GetStream().ReadByte();

    public void Write(ReadOnlySpan<byte> data) =>
        client.GetStream().Write(data);

    public void Flush() => client.GetStream().Flush();
    
    public void Close() => client.Close();
}
