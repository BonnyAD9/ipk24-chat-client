using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Data;
using IpkChat2024Client.Cli;

namespace IpkChat2024Client.Tcp;

public class TcpChatClient : ChatClient
{
    private TcpClient client = new();
    private MessageParser parser = new();

    protected override void Init(Args args) =>
        client.Connect(args.Address!, args.Port);

    protected override void SendAuthorize(
        ReadOnlySpan<char> username,
        ReadOnlySpan<char> secret
    ) {
        ArrayBufferWriter<byte> writer = new();

        writer.Write("AUTH "u8);
        Encoding.ASCII.GetBytes(username, writer);

        writer.Write(" AS "u8);
        Encoding.ASCII.GetBytes(DisplayName, writer);

        writer.Write(" USING "u8);
        Encoding.ASCII.GetBytes(secret, writer);

        writer.Write("\r\n"u8);

        client.GetStream().Write(writer.WrittenSpan);
    }

    protected override void SendBye() => client.GetStream().Write("BYE\r\n"u8);

    protected override void SendJoin(ReadOnlySpan<char> channel)
    {
        ArrayBufferWriter<byte> writer = new();

        writer.Write("JOIN "u8);
        Encoding.ASCII.GetBytes(channel, writer);

        writer.Write(" AS "u8);
        Encoding.ASCII.GetBytes(DisplayName, writer);

        writer.Write("\r\n"u8);

        client.GetStream().Write(writer.WrittenSpan);
    }

    protected override void SendMsg(ReadOnlySpan<char> content)
    {
        ArrayBufferWriter<byte> writer = new();

        writer.Write("MSG FROM "u8);
        Encoding.ASCII.GetBytes(DisplayName, writer);

        writer.Write(" IS "u8);
        Encoding.ASCII.GetBytes(content, writer);

        writer.Write("\r\n"u8);

        client.GetStream().Write(writer.WrittenSpan);
    }

    protected override void SendErr(ReadOnlySpan<char> content)
    {
        ArrayBufferWriter<byte> writer = new();

        writer.Write("ERR FROM "u8);
        Encoding.ASCII.GetBytes(DisplayName, writer);

        writer.Write(" IS "u8);
        Encoding.ASCII.GetBytes(content, writer);

        writer.Write("\r\n"u8);

        client.GetStream().Write(writer.WrittenSpan);
    }

    protected override object? TryReceive() =>
        parser.Parse(client.GetStream());

    protected override void Flush() => client.GetStream().Flush();

    protected override void Close() => client.Close();
}
