using Ipk24ChatClient;
using Ipk24ChatClient.Udp;

namespace Tests.Udp;

public class MessageParserTests
{
    [Fact]
    public static void TestConfirm()
    {
        var (id, msg) = MessageParser.Parse([
            (byte)MessageType.Confirm,
            0, 10
        ]);

        Assert.Equal(10, id);
        Assert.Equal(msg, new ConfirmMessage(10));
    }

    [Fact]
    public static void TestReply()
    {
        var (id, msg) = MessageParser.Parse([
            (byte)MessageType.Reply,
            0, 10,
            1, 0, 11,
            .."some message"u8, 0,
        ]);

        Assert.Equal(10, id);
        Assert.Equal(new ReplyMessage(true, "some message", 11), msg);
    }

    [Fact]
    public static void TestMsg()
    {
        var (id, msg) = MessageParser.Parse([
            (byte)MessageType.Msg,
            0, 10,
            .."display name"u8, 0,
            .."content"u8, 0,
        ]);

        Assert.Equal(10, id);
        Assert.Equal(new MsgMessage("display name", "content"), msg);
    }

    [Fact]
    public static void TestErr()
    {
        var (id, msg) = MessageParser.Parse([
            (byte)MessageType.Err,
            0, 10,
            .."display name"u8, 0,
            .."content"u8, 0,
        ]);

        Assert.Equal(10, id);
        Assert.Equal(new ErrMessage("content", "display name"), msg);
    }

    [Fact]
    public static void TestBye()
    {
        var (id, msg) = MessageParser.Parse([
            (byte)MessageType.Bye,
            0, 10
        ]);

        Assert.Equal(10, id);
        Assert.Equal(msg, new ByeMessage());
    }
}
