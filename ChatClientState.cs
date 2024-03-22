namespace IpkChat2024Client.Tcp;

/// <summary>
/// Represents the state of the client.
/// </summary>
public enum ChatClientState
{
    /// <summary>
    /// Client is not initialized.
    /// </summary>
    Stopped,
    /// <summary>
    /// Client is initialized but not authorized.
    /// </summary>
    Started,
    /// <summary>
    /// Authorization is in process.
    /// </summary>
    Authorizing,
    /// <summary>
    /// Client is successfuly authorized and may now send/receive messages or
    /// join channels.
    /// </summary>
    Open,
    /// <summary>
    /// An error occured, bye will be send immididetely.
    /// </summary>
    Error,
    /// <summary>
    /// The connection is closed.
    /// </summary>
    End,
}
