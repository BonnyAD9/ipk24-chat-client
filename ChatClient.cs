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

    /// <summary>
    /// Initialize the connection based on the arguments.
    /// </summary>
    /// <param name="args">Arguments for initialization.</param>
    /// <exception cref="InvalidOperationException">
    /// When in invalid state
    /// </exception>
    public void Connect(Args args)
    {
        // Check if the action is valid in the current state.
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
        // Check if the action is valid in the current state.
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

        // Validate the arguments
        Validators.Username(username);
        Validators.Secret(secret);

        if (displayName is not null)
        {
            DisplayName = displayName;
        }

        // Send the mssage.
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
        // Check if the action is valid in the current state.
        if (state != ChatClientState.Open)
        {
            throw new InvalidOperationException(
                "Cannot join channel: Not authorized"
            );
        }

        // Validate the argument.
        Validators.Channel(channelId);

        // Send the message.
        SendJoin(channelId);
        Flush();
    }

    /// <summary>
    /// Gracefuly close the connection.
    /// </summary>
    public void Bye()
    {
        // Check if the action is valid in the current state.
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

        // Send the message.
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
        // Check if the acion is valid in the current state.
        if (state != ChatClientState.Open)
        {
            throw new InvalidOperationException(
                "Cannot send messages: Not authorized."
            );
        }

        // Validate the argument.
        Validators.Message(message);

        // Send the message.
        SendMsg(message);
        Flush();
    }


    private void Err(ReadOnlySpan<char> msg)
    {
        // This is valid in every state.

        // Validate the argument.
        Validators.Message(msg);

        // Send the message
        SendMsg(msg);

        // Exit the connection
        Bye();
    }

    /// <summary>
    /// Check for any new messages.
    /// </summary>
    /// <returns>Next message, null if there is no next message.</returns>
    public object? Receive()
    {
        // Check if this is valid in the current state.
        if (state == ChatClientState.Stopped)
        {
            throw new InvalidOperationException(
                "Cannot recieve: Client not connected"
            );
        }

        // Receive the new message.
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

        // Update the state when it is appropriate.
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

    /// <summary>
    /// Initializes the client with the CLI arguments.
    /// </summary>
    /// <param name="args">Arguments for inicialization.</param>
    protected abstract void Init(Args args);

    /// <summary>
    /// Send the AUTH message. The arguments are already validated.
    /// </summary>
    /// <param name="username">Authorization username</param>
    /// <param name="secret">Authorization password</param>
    protected abstract void SendAuthorize(
        ReadOnlySpan<char> username,
        ReadOnlySpan<char> secret
    );

    /// <summary>
    /// Send the JOIN message. The argument is already validated.
    /// </summary>
    /// <param name="channel">Name of the channel to join.</param>
    protected abstract void SendJoin(ReadOnlySpan<char> channel);

    /// <summary>
    /// Send the MSG message. The argument is already validated.
    /// </summary>
    /// <param name="content">The message content.</param>
    protected abstract void SendMsg(ReadOnlySpan<char> content);

    /// <summary>
    /// Send the ERROR message. The argument is already validated.
    /// </summary>
    /// <param name="content">The error message.</param>
    protected abstract void SendErr(ReadOnlySpan<char> content);

    /// <summary>
    /// Send bye message.
    /// </summary>
    protected abstract void SendBye();

    /// <summary>
    /// Flush all the messages.
    /// </summary>
    protected abstract void Flush();

    /// <summary>
    /// Close the connectoin.
    /// </summary>
    protected abstract void Close();

    /// <summary>
    /// Try to receive new messages.
    /// </summary>
    /// <returns>Next message. null when there is no next message.</returns>
    protected abstract object? TryReceive();
}
