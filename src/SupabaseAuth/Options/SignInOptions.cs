namespace SupabaseAuth.Options;

/// Options used for signing in a user.
/// </summary>
public class SignInOptions
{
    /// <summary>
    ///     A URL or mobile address to send the user to after they are confirmed.
    /// </summary>
    public string? RedirectTo { get; set; }
}
