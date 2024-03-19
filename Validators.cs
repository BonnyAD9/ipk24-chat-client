namespace IpkChat2024Client;

static class Validators
{
    public static bool ExtendChannel { get; set; } = false;

    public static void Username(ReadOnlySpan<char> username)
        => Validate(
            username,
            20,
            c => c is '-' or >= 'A' and <= 'z' or >= '0' and <= '9',
            "username",
            "ascii leter, digit or '-'"
        );

    public static void Channel(ReadOnlySpan<char> channel)
        => Validate(
            channel,
            20,
            c => c is '-' or >= 'A' and <= 'z' or >= '0' and <= '9'
                || (ExtendChannel && c == '.'),
            "channel id",
            "ascii leter, digit or '-'"
        );

    public static void Secret(ReadOnlySpan<char> secret) => Validate(
        secret,
        128,
        c => c is '-' or >= 'A' and <= 'z' or >= '0' and <= '9',
        "channel id",
        "ascii leter, digit or '-'"
    );

    public static void DisplayName(ReadOnlySpan<char> displayName)
        => Validate(
            displayName,
            20,
            c => c >= 0x21 && c <= 0x7E,
            "display name",
            "printable ascii characters"
        );

    public static void Message(ReadOnlySpan<char> message)
        => Validate(
            message,
            1400,
            c => c >= 0x20 && c <= 0x7E,
            "message",
            "printable ascii characters"
        );

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
