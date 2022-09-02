namespace SupabaseAuth;

/// <summary>
///     Specifies the functionality expected from the `SignInAsync` method
/// </summary>
public enum SignInType
{
    Email,
    Phone,
    RefreshToken
}
