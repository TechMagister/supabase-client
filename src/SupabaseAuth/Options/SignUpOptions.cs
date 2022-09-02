namespace SupabaseAuth.Options;

/// <summary>
///     Options used for signing up a user.
/// </summary>
public class SignUpOptions : SignInOptions
{
    /// <summary>
    ///     Optional user metadata.
    /// </summary>
    public Dictionary<string, object>? Data { get; set; }
}
