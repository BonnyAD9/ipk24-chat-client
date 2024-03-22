record struct SentMessage(ushort Id, byte[] Msg, int Length, DateTime Time, int Resend)
{
    public SentMessage(ushort id, byte[] msg, int len) :
        this(id, msg, len, DateTime.Now, 0) {}
}
