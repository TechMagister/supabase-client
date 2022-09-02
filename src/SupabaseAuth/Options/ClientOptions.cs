namespace SupabaseAuth.Options;

/// <summary>
///     Class represention options available to the <see cref="SupabaseAuth.AuthClient" />.
/// </summary>
public class ClientOptions
{
    /// <summary>
    ///     Headers to be sent with subsequent requests.
    /// </summary>
    public Dictionary<string, string> Headers = new(Constants.DefaultHeaders);

    /// <summary>
    ///     Function to destroy a session.
    /// </summary>
    public Func<Task<bool>> SessionDestroyer = () => Task.FromResult(true);

    /// <summary>
    ///     Gotrue Endpoint
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    ///     Very unlikely this flag needs to be changed except in very specific contexts.
    ///     Enables tests to be E2E tests to be run without requiring users to have
    ///     confirmed emails - mirrors the Gotrue server's configuration.
    /// </summary>
    public bool AllowUnconfirmedUserSessions { get; set; } = false;
}
