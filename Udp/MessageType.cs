namespace IpkChat2024Client.Udp;

/// <summary>
/// Type of message, values are the same as the message type in the UDP variant
/// of the IPK protocol.
/// </summary>
enum MessageType : byte
{
    Confirm = 0x00,
    Reply = 0x01,
    Auth = 0x02,
    Join = 0x03,
    Msg = 0x04,
    Err = 0xFE,
    Bye = 0xFF,
}
