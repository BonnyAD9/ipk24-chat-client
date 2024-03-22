using System.Net;
using IpkChat2024Client.Cli;
using IpkChat2024Client.Tcp;

namespace IpkChat2024Client;

public abstract class ChatClient
{
    private ChatClientState state = ChatClientState.Stopped;

    private string displayName = "?";
    /// <summary>
    /// Get/Set the display name
    /// </summary>
    public string DisplayName
    {
        get => displayName;
        set
        {
            Validators.DisplayName(value);
            displayName = value;
        }
    }

    public void Connect(Args args)
    {
        if (state != ChatClientState.Stopped)
        {
            throw new InvalidOperationException(
                "Cannot connect: Client is already connected"
            );
        }

        Init(args);

        state = ChatClientState.Started;
    }

    /// <summary>
    /// Authorize the user
    /// </summary>
    /// <param name="username">User name of the user</param>
    /// <param name="secret">Password of the user</param>
    /// <param name="displayName">Display name of the user</param>
    public void Authorize(
        ReadOnlySpan<char> username,
        ReadOnlySpan<char> secret,
        string? displayName = null
    ) {
        if (state != ChatClientState.Started)
        {
            throw new InvalidOperationException(state switch
                {
                    ChatClientState.Stopped
                        => "Cannot authorize: not connected to server",
                    _ => "Client is already authorized",
                }
            );
        }

        Validators.Username(username);
        Validators.Secret(secret);

        if (displayName is not null)
        {
            DisplayName = displayName;
        }

        SendAuthorize(username, secret);
        Flush();

        state = ChatClientState.Authorizing;
    }

    /// <summary>
    /// Joins the given channel.
    /// </summary>
    /// <param name="channelId">Id of the channel to join</param>
    public void Join(ReadOnlySpan<char> channelId)
    {
        if (state != ChatClientState.Open)
        {
            throw new InvalidOperationException(
                "Cannot join channel: Not authorized"
            );
        }

        Validators.Channel(channelId);

        SendJoin(channelId);
        Flush();
    }

    /// <summary>
    /// Gracefuly close the connection.
    /// </summary>
    public void Bye()
    {
        switch (state)
        {
            case ChatClientState.Stopped:
                throw new InvalidOperationException(
                    "Cannot say bye when not connected"
                );
            case ChatClientState.End:
                throw new InvalidOperationException(
                    "Cannot say bye: connection is alreaady closed"
                );
        }

        SendBye();
        Flush();

        state = ChatClientState.End;

        Close();
    }

    /// <summary>
    /// Send message.
    /// </summary>
    /// <param name="message">Message to send</param>
    public void Send(ReadOnlySpan<char> message)
    {
        if (state != ChatClientState.Open)
        {
            throw new InvalidOperationException(
                "Cannot send messages: Not authorized."
            );
        }

        Validators.Message(message);

        SendMsg(message);
        Flush();
    }


    private void Err(ReadOnlySpan<char> msg)
    {
        Validators.Message(msg);

        SendMsg(msg);

        Bye();
    }

    /// <summary>
    /// Check for any new messages, returns all new messages. Doesn't block.
    /// </summary>
    /// <returns>All new messages.</returns>
    public object? Receive()
    {
        if (state == ChatClientState.Stopped)
        {
            throw new InvalidOperationException(
                "Cannot recieve: Client not connected"
            );
        }

        object? m;

        try
        {
            m = TryReceive();
        }
        catch (Exception ex)
        {
            Err(ex.Message);
            throw;
        }


        switch (m)
        {
            case ByeMessage msg:
                Bye();
                return msg;
            case ReplyMessage msg:
                if (state is ChatClientState.Authorizing)
                {
                    state = ChatClientState.Open;
                }
                return msg;
            default:
                return m;
        }
    }

    protected abstract void Init(Args args);

    protected abstract void SendAuthorize(
        ReadOnlySpan<char> username,
        ReadOnlySpan<char> secret
    );

    protected abstract void SendJoin(ReadOnlySpan<char> channel);

    protected abstract void SendMsg(ReadOnlySpan<char> content);

    protected abstract void SendErr(ReadOnlySpan<char> content);

    protected abstract void SendBye();

    protected abstract void Flush();

    protected abstract void Close();

    protected abstract object? TryReceive();
}
