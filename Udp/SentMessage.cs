using System.Text;

namespace Ipk24ChatClient.Udp;

/// <summary>
/// Represents serialized message that was or can be sent via the UDP variant
/// of the IPK protocol.
/// </summary>
/// <param name="Id">Unique id of the message.</param>
/// <param name="Msg">The serialized message.</param>
/// <param name="Length">Length of the serialized message.</param>
/// <param name="Time">
/// When the message was sent, this is time of creation of this record when the message was not sent
/// </param>
/// <param name="Resend">
/// How many times it has been resent, this is 0 for new message.
/// </param>
record struct SentMessage(
    ushort Id,
    byte[] Msg,
    int Length,
    DateTime Time,
    int Resend
) {
    /// <summary>
    /// Create serialized message
    /// </summary>
    /// <param name="id">Id of the message</param>
    /// <param name="msg">The message data</param>
    /// <param name="len">Length of the message</param>
    public SentMessage(ushort id, byte[] msg, int len) :
        this(id, msg, len, DateTime.Now, 0) {}

    /// <summary>
    /// Create serialized message.
    /// </summary>
    /// <param name="id">Id of the message.</param>
    /// <param name="msg">The message data.</param>
    public SentMessage(ushort id, byte[] msg) :
        this(id, msg, msg.Length, DateTime.Now, 0) {}

    /// <summary>
    /// Create the AUTH message.
    /// </summary>
    /// <param name="id">Id of the message.</param>
    /// <param name="username">Authorization username.</param>
    /// <param name="secret">Authorization password.</param>
    /// <param name="displayName">Display name of the sender.</param>
    /// <returns>Serialized message.</returns>
    public static SentMessage Authorize(
        ushort id,
        ReadOnlySpan<char> username,
        ReadOnlySpan<char> secret,
        ReadOnlySpan<char> displayName
    ) {
        var maxLen = 6
            + username.Length
            + displayName.Length
            + secret.Length;

        byte[] arr = new byte[maxLen];
        var buf = arr.AsSpan();

        SetHeader(id, MessageType.Auth, ref buf);

        AddString(username, ref buf);
        AddString(displayName, ref buf);
        AddString(secret, ref buf);

        return LeaveEnd(id, arr, buf.Length);
    }

    /// <summary>
    /// Create BYE message.
    /// </summary>
    /// <param name="id">Id of the message.</param>
    /// <returns>Serialized BYE message.</returns>
    public static SentMessage Bye(ushort id)
    {
        var arr = new byte[3];
        var buf = arr.AsSpan();
        SetHeader(id, MessageType.Bye, ref buf);
        return new SentMessage(id, arr);
    }

    /// <summary>
    /// Create JOIN message.
    /// </summary>
    /// <param name="id">Id of the message.</param>
    /// <param name="channel">Channel to join.</param>
    /// <param name="displayName">Display name of the sender.</param>
    /// <returns>Serialized JOIN message.</returns>
    public static SentMessage Join(
        ushort id,
        ReadOnlySpan<char> channel,
        ReadOnlySpan<char> displayName
    ) => Make2String(id, MessageType.Join, channel, displayName);

    /// <summary>
    /// Crate MSG message.
    /// </summary>
    /// <param name="id">Id of the message.</param>
    /// <param name="displayName">Display name of the sender.</param>
    /// <param name="content">The message content.</param>
    /// <returns>Serialized MSG message.</returns>
    public static SentMessage MakeMsg(
        ushort id,
        ReadOnlySpan<char> displayName,
        ReadOnlySpan<char> content
    ) => Make2String(id, MessageType.Msg, displayName, content);

    /// <summary>
    /// Create ERROR message.
    /// </summary>
    /// <param name="id">Id of the message.</param>
    /// <param name="displayName">Display name of the sender.</param>
    /// <param name="content">The message content.</param>
    /// <returns>Serialized ERROR message.</returns>
    public static SentMessage Err(
        ushort id,
        ReadOnlySpan<char> displayName,
        ReadOnlySpan<char> content
    ) => Make2String(id, MessageType.Err, displayName, content);

    /// <summary>
    /// Create message that consists of two strings.
    /// </summary>
    /// <param name="id">Id of the message.</param>
    /// <param name="type">Message type</param>
    /// <param name="s1">First string.</param>
    /// <param name="s2">Second string.</param>
    /// <returns>
    /// Serialized message of the given type with the two strings.
    /// </returns>
    private static SentMessage Make2String(
        ushort id,
        MessageType type,
        ReadOnlySpan<char> s1,
        ReadOnlySpan<char> s2
    ) {
        var maxLen = 5 + s1.Length + s2.Length;
        byte[] arr = new byte[maxLen];
        var buf = arr.AsSpan();

        SetHeader(id, type, ref buf);

        AddString(s1, ref buf);
        AddString(s2, ref buf);

        return LeaveEnd(id, arr, buf.Length);
    }

    /// <summary>
    /// Creates SentMessage from message data that has padding at the end.
    /// </summary>
    /// <param name="id">Id of the message.</param>
    /// <param name="msg">The message data.</param>
    /// <param name="leave">The padding at the end.</param>
    /// <returns>The new message.</returns>
    private static SentMessage LeaveEnd(ushort id, byte[] msg, int leave) =>
        new SentMessage(id, msg, msg.Length - leave);

    private static void SetHeader(
        ushort id,
        MessageType type,
        ref Span<byte> buf
    ) {
        buf[0] = (byte)type;
        buf[1] = (byte)(id >> 8);
        buf[2] = (byte)id;
        buf = buf[3..];
    }

    private static void AddString(ReadOnlySpan<char> str, ref Span<byte> buf)
    {
        var i = Encoding.ASCII.GetBytes(str, buf);
        buf[i] = 0;
        buf = buf[(i + 1)..];
    }
}
