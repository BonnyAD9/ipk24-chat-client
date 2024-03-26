using System.Text.RegularExpressions;

namespace Ipk24ChatClientTester;

partial class TcpParser
{
    public static object Parse(string msg)
    {
        var r = Auth().Match(msg);
        if (r.Success)
        {
            return new AuthMsg(
                ParseId(r, "id"),
                r.Groups["un"].Value,
                r.Groups["dn"].Value,
                r.Groups["sc"].Value
            );
        }
        r = Join().Match(msg);
        if (r.Success)
        {
        }
    }

    public static ushort ParseId(Match m, string name)
    {
        if (ushort.TryParse(m.Groups["id"].Value, out ushort res))
        {
            return res;
        }
        return 0;
    }

    [GeneratedRegex(
        "^((?<id>[^:]*): )?AUTH (?<un>[^ ]*) AS (?<dn>[^ ]*) USING (?<sc>[^\r]*)\r?\n?$"
    )]
    private static partial Regex Auth();

    [GeneratedRegex(
        "^((?<id>[^:]*): )?JOIN (?<un>[^ ]*) AS (?<cn>[^\r]*)\r?\n?$"
    )]
    private static partial Regex Join();

    [GeneratedRegex(
        "^((?<id>[^:]*): )?ERR FROM (?<dn>[^ ]*) IS (?<ms>[^\r]*)\r?\n?$"
    )]
    private static partial Regex Err();

    [GeneratedRegex(
        "^((?<id>[^:]*): )?BYE\r?\n?$"
    )]
    private static partial Regex Bye();

    [GeneratedRegex(
        "^((?<id>[^:]*): )?MSG FROM (?<dn>[^ ]*) IS (?<ms>[^\r]*)\r?\n?$"
    )]
    private static partial Regex Msg();

    [GeneratedRegex(
        "^((?<id>[^\\-]*)->(?<rid>[^:]*): )?REPLY (?<ok>(OK|NOK)) IS (?<ms>[^\r]*)\r?\n?$"
    )]
    private static partial Regex Reply();

    [GeneratedRegex(
        "^(->(?<rid>[^:]*): )?CONFIRM\r?\n?$"
    )]
    private static partial Regex Confirm();
}
