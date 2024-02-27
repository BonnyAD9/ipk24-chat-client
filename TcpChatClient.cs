using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IpkChat2024Client;

public class TcpChatClient : IChatClient
{
    private string? displayName;

    public string DisplayName
    {
        get => displayName ?? "name";
        set {
            if (value.Length > 20) {
                throw
                    new ArgumentException("The max DisplayName length is 20");
            }
            if (value.Any(c => c < 0x21 || c > 0x7e)) {
                throw new ArgumentException(
                    "The DisplayName can contain only ascii printable "
                    + "characters."
                );
            }
            displayName = value;
        }
    }

    private TcpClient tcpClient = new TcpClient();

    public void Connect(IPAddress address, ushort port)
        => tcpClient.Connect(address, port);

    public void Authorize(ReadOnlySpan<char> username, ReadOnlySpan<char> secret, string? displayName = null)
    {
        var stream = tcpClient.GetStream();
        ArrayBufferWriter<byte> writer = new();

        stream.Write("AUTH "u8);

        Encoding.ASCII.GetBytes(username, writer);
        stream.Write(writer.WrittenSpan);

        stream.Write(" USING "u8);

        writer.Clear();
        Encoding.ASCII.GetBytes(secret, writer);
        stream.Write(writer.WrittenSpan);

        stream.Write("\r\n"u8);

        stream.Flush();
    }

    public void Bye()
    {
        var stream = tcpClient.GetStream();
        stream.Write("BYE\r\n"u8);
        stream.Flush();
    }

    public void Join(ReadOnlySpan<char> channelId)
    {
        var stream = tcpClient.GetStream();
        ArrayBufferWriter<byte> writer = new();

        stream.Write("JOIN "u8);

        Encoding.ASCII.GetBytes(channelId, writer);
        stream.Write(writer.WrittenSpan);

        stream.Write(" AS "u8);

        writer.Clear();
        Encoding.ASCII.GetBytes(DisplayName, writer);
        stream.Write(writer.WrittenSpan);

        stream.Write("\n\r"u8);
        stream.Flush();
    }

    public void Send(ReadOnlySpan<char> message)
    {
        var stream = tcpClient.GetStream();
        ArrayBufferWriter<byte> writer = new();

        stream.Write("MSG FROM "u8);

        Encoding.ASCII.GetBytes(DisplayName, writer);
        stream.Write(writer.WrittenSpan);

        stream.Write(" IS "u8);

        writer.Clear();
        Encoding.ASCII.GetBytes(message, writer);
        stream.Write(writer.WrittenSpan);

        stream.Flush();
    }

    public List<string> Receive()
    {
        throw new NotImplementedException();
    }
}
