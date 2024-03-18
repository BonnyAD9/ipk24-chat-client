record struct SentMessage(ushort Id, byte[] Msg, int Length, DateTime Time)
{
    public SentMessage(ushort id, byte[] msg, int len) :
        this(id, msg, len, DateTime.Now) {}
}
