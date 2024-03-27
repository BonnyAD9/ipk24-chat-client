using Ipk24ChatClient.Tcp;

namespace Tests.Tcp;

public class TcpChatClientTests
{
    [Fact]
    public static void TestAuthorize()
    {
        var sender = new TcpSender([]);
        TcpChatClientWrapper client = new(sender)
        {
            DisplayName = "display_name"
        };

        client.WrapAuthorize("username", "my_strong_password");

        Assert.True(sender.LastRecv.SequenceEqual(
            "AUTH username AS display_name USING my_strong_password\r\n"u8
        ));
    }

    [Fact]
    public static void TestBye()
    {
        var sender = new TcpSender([]);
        TcpChatClientWrapper client = new(sender);

        client.WrapBye();

        Assert.True(sender.LastRecv.SequenceEqual("BYE\r\n"u8));
    }

    [Fact]
    public static void TestJoin()
    {
        var sender = new TcpSender([]);
        TcpChatClientWrapper client = new(sender)
        {
            DisplayName = "display_name"
        };

        client.WrapJoin("channel");

        Assert.True(sender.LastRecv.SequenceEqual(
            "JOIN channel AS display_name\r\n"u8
        ));
    }

    [Fact]
    public static void TestMsg()
    {
        var sender = new TcpSender([]);
        TcpChatClientWrapper client = new(sender);

        client.DisplayName = "display_name";
        client.WrapMsg("some message");

        Assert.True(sender.LastRecv.SequenceEqual(
            "MSG FROM display_name IS some message\r\n"u8
        ));
    }

    [Fact]
    public static void TestErr()
    {
        var sender = new TcpSender([]);
        TcpChatClientWrapper client = new(sender)
        {
            DisplayName = "display_name"
        };

        client.WrapErr("some message");

        Assert.True(sender.LastRecv.SequenceEqual(
            "ERR FROM display_name IS some message\r\n"u8
        ));
    }
}
