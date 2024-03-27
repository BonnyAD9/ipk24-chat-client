using Ipk24ChatClient.Udp;

namespace Tests.Udp;

public class SentMessageTests
{
    [Fact]
    public static void TestAuthorize()
    {
        var msg =
            SentMessage.Authorize(10, "username", "secret", "displayName");

        Assert.Equal(
            [
                (byte)MessageType.Auth,
                0, 10,
                .."username"u8, 0,
                .."displayName"u8, 0,
                .."secret"u8, 0,
            ],
            msg.Msg[..msg.Length]
        );
    }

    [Fact]
    public static void TestBye()
    {
        var msg = SentMessage.Bye(10);

        Assert.Equal(
            [
                (byte)MessageType.Bye,
                0, 10,
            ],
            msg.Msg[..msg.Length]
        );
    }

    [Fact]
    public static void TestJoin()
    {
        var msg = SentMessage.Join(10, "channel", "displayName");

        Assert.Equal(
            [
                (byte)MessageType.Join,
                0, 10,
                .."channel"u8, 0,
                .."displayName"u8, 0,
            ],
            msg.Msg[..msg.Length]
        );
    }

    [Fact]
    public static void TestMsg()
    {
        var msg = SentMessage.MakeMsg(10, "displayName", "content");

        Assert.Equal(
            [
                (byte)MessageType.Msg,
                0, 10,
                .."displayName"u8, 0,
                .."content"u8, 0,
            ],
            msg.Msg[..msg.Length]
        );
    }

    [Fact]
    public static void TestErr()
    {
        var msg = SentMessage.Err(10, "displayName", "content");

        Assert.Equal(
            [
                (byte)MessageType.Err,
                0, 10,
                .."displayName"u8, 0,
                .."content"u8, 0,
            ],
            msg.Msg[..msg.Length]
        );
    }
}
