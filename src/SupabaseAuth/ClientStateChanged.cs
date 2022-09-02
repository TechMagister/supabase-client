namespace SupabaseAuth;

/// <summary>
///     States that the Auth Client will raise events for.
/// </summary>
public enum AuthState
{
    SignedIn,
    SignedOut,
    UserUpdated,
    PasswordRecovery,
    TokenRefreshed
}

/// <summary>
///     Class representing a state change on the <see cref="SupabaseAuth.AuthClient" />.
/// </summary>
public class ClientStateChanged : EventArgs
{
    public AuthState? State { get; }

    public ClientStateChanged(AuthState state)
    {
        State = state;
    }
}
