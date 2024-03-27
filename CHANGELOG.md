# CHANGELOG

### CLI
- Choose the protocol with `-t`, available protocols are `tcp` and `udp`.
- Choose the server address with `-s`
- Choose the port with `-p` (default port is `4567`)
- Choose the udp timeout with `-d` (dafault is `250`ms)
- Choose the max number of udp retransmitions with `-r` (default is `3`)
- Show help with `-h`
- Enable extended features with `-e`
- Set the max number of udp message to send before receiving confirmation with
  `-w`
- Set when colored printing should be used with `--color`

### Commands
- Authorize to server with `/auth`
- Change the channel with `/join`
- Change the dsiplay name with `/rename`
- Show the help with `/help`
- Clear the screen with `/clear`
- Exit the program by pressing `Ctrl+C`
- Send messages when the first character is not `/`

### Environment variables
- Enable extended features with the variable `IPK_EXTEND`

I am unaware of any limitations.
