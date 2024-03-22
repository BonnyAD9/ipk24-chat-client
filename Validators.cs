namespace IpkChat2024Client;

/// <summary>
/// Validates data from the user.
/// </summary>
static class Validators
{
    /// <summary>
    /// True if the character '.' is allowed in channel name.
    /// </summary>
    public static bool ExtendChannel { get; set; } = false;

    /// <summary>
    /// Validates a username.
    /// </summary>
    /// <param name="username">Username to validate.</param>
    public static void Username(ReadOnlySpan<char> username)
        => Validate(
            username,
            20,
            c => c is '-' or >= 'A' and <= 'z' or >= '0' and <= '9',
            "username",
            "ascii leter, digit or '-'"
        );

    /// <summary>
    /// Validates a channel.
    /// </summary>
    /// <param name="channel">Channel to validate.</param>
    public static void Channel(ReadOnlySpan<char> channel)
        => Validate(
            channel,
            20,
            c => c is '-' or >= 'A' and <= 'z' or >= '0' and <= '9'
                || (ExtendChannel && c == '.'),
            "channel id",
            "ascii leter, digit or '-'"
        );

    /// <summary>
    /// Validates a password.
    /// </summary>
    /// <param name="secret">Password to validate.</param>
    public static void Secret(ReadOnlySpan<char> secret) => Validate(
        secret,
        128,
        c => c is '-' or >= 'A' and <= 'z' or >= '0' and <= '9',
        "channel id",
        "ascii leter, digit or '-'"
    );

    /// <summary>
    /// Validates a display name.
    /// </summary>
    /// <param name="displayName">Display name to validate.</param>
    public static void DisplayName(ReadOnlySpan<char> displayName)
        => Validate(
            displayName,
            20,
            c => c >= 0x21 && c <= 0x7E,
            "display name",
            "printable ascii characters"
        );

    /// <summary>
    /// Validates a message.
    /// </summary>
    /// <param name="message">Message to validate.</param>
    public static void Message(ReadOnlySpan<char> message)
        => Validate(
            message,
            1400,
            c => c >= 0x20 && c <= 0x7E,
            "message",
            "printable ascii characters"
        );

    /// <summary>
    /// Generic method for validating stuff.
    /// </summary>
    /// <param name="field">String that is validated.</param>
    /// <param name="maxLen">Maximum allowed length of the string.</param>
    /// <param name="allowedChars">
    /// Predicate that decides what characters are valid.
    /// </param>
    /// <param name="name">
    /// Name of the validated value. Used in error messages
    /// </param>
    /// <param name="allowedDesc">
    /// Description of what the value should bes. Used in error messages.
    /// </param>
    /// <exception cref="ArgumentException">
    /// When the value is invalid
    /// </exception>
    private static void Validate(
        ReadOnlySpan<char> field,
        int maxLen,
        Func<char, bool> allowedChars,
        string name,
        string allowedDesc
    )
    {
        if (field.Length == 0) {
            throw new ArgumentException(
                $"Too short {name} (must be at least 1 character)."
            );
        }
        if (field.Length > maxLen) {
            throw new ArgumentException(
                $"Too long {name} (cannot be more than 20 characters long)."
            );
        }
        foreach (var c in field)
        {
            if (!allowedChars(c)) {
                throw new ArgumentException(
                    $"Invalid  {name}: It may contain only {allowedDesc}."
                );
            }
        }
    }
}
