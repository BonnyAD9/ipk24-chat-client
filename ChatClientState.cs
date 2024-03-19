namespace IpkChat2024Client.Tcp;

public enum ChatClientState
{
    Stopped,
    Started,
    Authorizing,
    Open,
    Error,
    End,
}
