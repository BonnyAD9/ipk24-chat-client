using System.Text;

namespace IpkChat2024Client.Udp;

record struct SentMessage(
    ushort Id,
    byte[] Msg,
    int Length,
    DateTime Time,
    int Resend
) {
    public SentMessage(ushort id, byte[] msg, int len) :
        this(id, msg, len, DateTime.Now, 0) {}

    public SentMessage(ushort id, byte[] msg) :
        this(id, msg, msg.Length, DateTime.Now, 0) {}

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

    public static SentMessage Bye(ushort id)
    {
        var arr = new byte[3];
        var buf = arr.AsSpan();
        SetHeader(id, MessageType.Bye, ref buf);
        return new SentMessage(id, arr);
    }

    public static SentMessage Join(
        ushort id,
        ReadOnlySpan<char> channel,
        ReadOnlySpan<char> displayName
    ) => Make2String(id, MessageType.Join, channel, displayName);

    public static SentMessage MakeMsg(
        ushort id,
        ReadOnlySpan<char> displayName,
        ReadOnlySpan<char> content
    ) => Make2String(id, MessageType.Msg, displayName, content);

    public static SentMessage Err(
        ushort id,
        ReadOnlySpan<char> displayName,
        ReadOnlySpan<char> content
    ) => Make2String(id, MessageType.Err, displayName, content);

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
