# ipk24chat-client
**Documentation**

Author: **xstigl00**

## Contents
- [About](#about)
- [Code structure](#code-structure)
    - [`Ipk24ChatClient`](#ipk24chatclient)
        - [Program entry point](#program-entry-point)
        - [`ChatClient`](#chatclient)
        - [Message records](#message-records)
        - [User input in interactive terminal](#user-input-in-interactive-terminal)
    - [`IpkChatClient.Udp`](#ipkchatclientudp)
        - [`UdpChatClient`](#udpchatclient)
            - [Sending](#sending)
            - [Receiving](#receiving)
    - [`IpkChatClient.Tcp`](#ipk24chatclienttcp)
    - [`IpkChatClient.Cli`](#ipk24chatclientcli)
- [Testing](#testing)
- [Extra functionality](#extra-functionality)


## About
The project is chat client using the `IPK24-CHAT` protocol in both its `UDP`
and `TCP` variant.

I decided to implement this project in C# because I know the language well and
its standard library has lots of features that I can just use without the need
to implement them.

The implementation uses only single thread. In order to achieve that, all
operations are non-blocking. Reading from console is done by setting terminal
to raw mode (C# does this by default) and than reading one key at a time only
when there are any available keys. Non-blocking receiving is achieved by
checking whether there are any new data before calling the function that
receives.

## Code structure
The code is divided into 4 namespaces:
- `Ipk24ChatClient`: the main namespace, contains the `Main` function, class
  `ConsoleReader` for non-blocking reading and writing to console and classes
  that are common for both the TCP and UDP variant of the protocol.
- `Ipk24ChatClient.Cli`: Contains structures and logic for parsing command line
  arguments.
- `Ipk24ChatClient.Udp`: Logic for the UDP variant.
- `Ipk24ChatClient.Tcp`: Logic for the TCP variant.

All errors are propagated using exceptions.

### `Ipk24ChatClient`
This namespace is the base of the project. It contains the `Main` function,
logic for the interactive console and structures common for UDP and TCP variant
of the protocol.

#### Program entry point
The main function is located in the file *Program.cs*. The `Main` function
itself is just a wrapper around the function `Start`. Configuration useful for
the man loop is stored in static fields and properties of the class `Program`.

The main loop of the program is in the static method `Program.RunClient`. In
each iteration of the loop, it checks for the user input and than for new data
from the server. These operations are non-blocking. To prevent the CPU from
doing a lot of work for nothing, the loop also contains `Thread.sleep` that
by default sleeps for 10 ms.

Colored printing is achieved by setting static fields that represent color
modes either as empty strings when colors are disabled, or with their
respective ansi codes when colored printing is enabled. This is implemented in
the static function `Program.InitANSI`.

#### `ChatClient`
This is abstract class that represents chat client. It is independent of the
prtocol variant. Its main purpose is to track and transition between the client
states and to check whether operation requested by user is valid in the current
state.

It also decides when it is apropriate to send error messages to the server.

User input is validated using static methods in the static class `Validators`.

#### Message records
Messages received by the server are represented by record classes. They are
declared in `Message.cs`

#### User input in interactive terminal
User input and output is handled by the class `ConsoleReader`. It provides
method for non-blocking variant of `Console.ReadLine` and methods that can
print to console at the same time that user types to the console.

User input is readed one key at a time from the terminal that is set raw mode.
The standard C# library provides basic logic for manipulating the raw terminal,
but it has only few features and some of them are slow, so I decided to use the
nuget package `Bny.Console` for aditional functionality.

Apart from reading from the standard input, `ConsoleReader` also handles
special input from the user (such as moving the cursor and editing what was
already typed) and ensures that the text and cursor position in the console is
properly displayed to the user.

### `IpkChatClient.Udp`
This namespace is located in the folder `Udp` and contains logic specific to
the UDP variant of the protocol. The logic is more complicated compared to the
TCP variant so it is split into several units.

Parsing of messages from binary is done by static class `MessageParser`.

Serializing of messages to binary is done by static methods on the record
`SentMessage`.

#### `UdpChatClient`
This class contains the main logic of the UDP client and implements the
abstract class `ChatClient`.

It handles the lower level details of the protocol that are specific to the UDP
variant such as confirmations (sending/receiving) and retransmition of messages
for which there was no confirmation in the specified timeout.

The logic is achieved by using two buffers for both received and sent messages
(4 total). One queue and one list for each direction.

##### Sending
When sending message it is first stored in a queue where it waits to be sent.
It is not sent imidietely because the specification doesn't say how many
unconfirmed messages may be sent at a time and so I decided that this is
implicitly supposed to be only one message (Next message is sent only after
the previous has been confirmed.). This can be changed using one of the
extensions.

When message is sent to the server it is stored in the list where it waits to
be confirmed or potentially retransmitted if timeout is reached.

##### Receiving
All received messages are stored in the list. Every time new messages are
received, the client checks their *id*. If the *id* matches *id* of the next
expected message, it is moved to the queue where it waits to be readed trough
the `ChatClient` abstract method implementation.

In the reference server, there is a bug that it sends its *id* in wrong byte
order. To make the client compatible with the reference server and also comply
with the specification, there is a method `CheckEndianness` that swaps the
endianness of the *id* if the other endianness seems more likely to be correct.

### `Ipk24ChatClient.Tcp`
Compared to UDP the TCP variant is much simpler. The main logic is in the class
`TcpChatClient` that implements the abstract class `ChatClient`.

Because TCP is reliable, there is no special logic for confirmations or
reordering of the messages, so the serialization of the messages to binary is
done directly in the implementation of the abstract methods.

On the other hand, there are no boundaries between data so there is special
class for parsing the messages: `MessageParser`. The parser reads from the TCP
stream and parses the data. It also handles situations where whole message may
not be received at once and it is necesary to wait for new data.

### `Ipk24ChatClient.Cli`
This namespace contains the logic for parsing the command line argumens. The
parsing logic is implemented in methods of the class `Args`. The class itself
than holds the information about the configuration from the command line.

## Testing
Core networking functionality is tested using unit tests in the folder `tests`.

I also checked functionality of the app as whole manually with the reference
server.

## Extra functionality
- **Useful extra prints**
    - enabled with flag `-e` or by setting environment variable `IPK_EXTEND` to
      `YES`
- **Colored printing**
    - enabled automatically when *extra prints* are enabled and the standard
    output is terminal.
    - can be also forced by `--color=always`
- **Setting how many unconfirmend UDP messages may be sent**
    - can be set with the flag `-w`.
    - This is by default `1`.
+ Command `/clear` and `/claer` (I often mistype *clear* in this way)

All of the extensions are also documented in the help commands.
