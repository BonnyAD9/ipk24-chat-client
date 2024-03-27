using Ipk24ChatClient.Tcp;

namespace Tests.Tcp;

class TcpChatClientWrapper : TcpChatClient
{
    public TcpChatClientWrapper(ITcpClient client) : base(client) {}

    public void WrapAuthorize(
        ReadOnlySpan<char> username,
        ReadOnlySpan<char> secret
    ) => SendAuthorize(username, secret);

    public void WrapBye() => SendBye();

    public void WrapJoin(ReadOnlySpan<char> channel) => SendJoin(channel);

    public void WrapMsg(ReadOnlySpan<char> content) => SendMsg(content);

    public void WrapErr(ReadOnlySpan<char> content) => SendErr(content);
}
