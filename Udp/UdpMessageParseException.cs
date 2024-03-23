namespace Ipk24ChatClient.Udp;

/// <summary>
/// Thrown when udp message fails to parse, but the id of the message is known.
/// </summary>
class UdpMessageParseException : Exception
{
    /// <summary>
    /// Id of the message.
    /// </summary>
    public ushort Id { get; init; }

    public UdpMessageParseException(
        Exception inner,
        ushort id
    ) : base(null, inner)
    {
        Id = id;
    }
}
