using Ipk24ChatClient;
using Ipk24ChatClient.Tcp;

namespace Tests;

public class MessageParserTests
{
    [Fact]
    public static void TestErr()
    {
        var msg = ParseMessage("ERR FROM Test IS some error\r\n"u8);

        Assert.True(msg is ErrMessage);

        var emsg = (ErrMessage)msg;

        Assert.Equal("Test", emsg.DisplayName);
        Assert.Equal("some error", emsg.Content);
    }

    [Fact]
    public static void TestReplyOK()
    {
        var m = ParseMessage("REPLY OK IS some message\r\n"u8);

        Assert.True(m is ReplyMessage);

        var msg = (ReplyMessage)m;

        Assert.True(msg.Ok);
        Assert.Equal("some message", msg.Content);
    }

    [Fact]
    public static void TestReplyNOK()
    {
        var m = ParseMessage("REPLY NOK IS some message\r\n"u8);

        Assert.True(m is ReplyMessage);

        var msg = (ReplyMessage)m;

        Assert.False(msg.Ok);
        Assert.Equal("some message", msg.Content);
    }

    [Fact]
    public static void TestMsg()
    {
        var m = ParseMessage("MSG FROM DisplayName IS some message\r\n"u8);

        Assert.True(m is MsgMessage);

        var msg = (MsgMessage)m;

        Assert.Equal("DisplayName", msg.Sender);
        Assert.Equal("some message", msg.Content);
    }

    [Fact]
    public static void TestBye()
    {
        var m = ParseMessage("BYE\r\n"u8);

        Assert.True(m is ByeMessage);
    }

    private static object? ParseMessage(ReadOnlySpan<byte> data)
    {
        TcpSender sender = new(data.ToArray());
        MessageParser parser = new();
        return parser.Parse(sender);
    }
}
