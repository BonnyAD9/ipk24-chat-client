using Ipk24ChatClient;
using Ipk24ChatClient.Cli;
using Ipk24ChatClient.Udp;

namespace Tests.Udp;

public class UdpChatClientTests
{
    [Fact]
    public static void TestConfirm()
    {
        UdpClient client = new([
            [
                (byte)MessageType.Bye,
                0, 10,
            ]
        ]);

        UdpChatClientWrapper chat = new(client);
        chat.Connect(Args.Parse([ "-s", "anton5.fit.vutbr.cz", "-t", "udp" ]));

        Assert.Equal(new ByeMessage(), chat.WrapReceive());
        Assert.Equal(
            [
                (byte)MessageType.Confirm,
                0, 10,
            ],
            client.LastRecv
        );
    }
}
