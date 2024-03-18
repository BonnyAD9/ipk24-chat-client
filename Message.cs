namespace IpkChat2024Client;

record class ErrMessage(string Content, string DisplayName);

record class ReplyMessage(bool Ok, string Content, ushort RefId);

record class MsgMessage(string Sender, string Content);

record class ByeMessage;

record class ConfirmMessage(ushort Id);

record class AuthMessage(string Username, string DisplayName, string Secret);

record class JoinMessage(string Channel, string DisplayName);
