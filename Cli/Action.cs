namespace Ipk24ChatClient.Cli;

/// <summary>
/// Action to take, specified by CLI
/// </summary>
public enum Action
{
    /// <summary>
    /// This is never the result of properly parsed CLI
    /// </summary>
    None,
    Tcp,
    Udp,
    Help,
}
