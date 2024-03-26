namespace Ipk24ChatClient;

// Here are classes only for messages that are sent by the server.

/// <summary>
/// Error message.
/// </summary>
/// <param name="Content">The error message.</param>
/// <param name="DisplayName">The origin of the message.</param>
public record class ErrMessage(string Content, string DisplayName);

/// <summary>
/// Reply message from the server.
/// </summary>
/// <param name="Ok">True if OK false if NOK.</param>
/// <param name="Content">Message for the user.</param>
/// <param name="RefId">Id of the confirmed message. This is 0 for TCP.</param>
public record class ReplyMessage(bool Ok, string Content, ushort RefId);

/// <summary>
/// Message that should be shown to the user.
/// </summary>
/// <param name="Sender">Sender of the message.</param>
/// <param name="Content">Message for the user.</param>
public record class MsgMessage(string Sender, string Content);

/// <summary>
/// Ends the connection.
/// </summary>
public record class ByeMessage;

/// <summary>
/// Confirms that message was received. Used only with UDP.
/// </summary>
/// <param name="Id">Id of the confirmed message.</param>
public record class ConfirmMessage(ushort Id);
