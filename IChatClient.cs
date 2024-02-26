namespace IpkChat2024Client;

public interface IChatClient
{
    /// <summary>
    /// Get/Set the display name
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Authorize the user
    /// </summary>
    /// <param name="username">User name of the user.</param>
    /// <param name="secret">Password of the user.</param>
    public void Authorize(
        ReadOnlySpan<char> username,
        ReadOnlySpan<char> secret
    );

    /// <summary>
    /// Joins the given channel.
    /// </summary>
    /// <param name="channelId">Id of the channel to join</param>
    public void Join(ReadOnlySpan<char> channelId);

    /// <summary>
    /// Gracefuly close the connection.
    /// </summary>
    public void Bye();

    /// <summary>
    /// Send message.
    /// </summary>
    /// <param name="message">Message to send</param>
    public void Send(ReadOnlySpan<char> message);

    /// <summary>
    /// Check for any new messages, returns all new messages. Doesn't block.
    /// </summary>
    /// <returns>All new messages.</returns>
    public List<string> Receive();
}
