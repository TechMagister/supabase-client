using System.Text.Json.Serialization;

namespace SupabaseAuth;

/// <summary>
///     Represents a Gotrue User
///     Ref: https://supabase.github.io/gotrue-js/interfaces/User.html
/// </summary>
public class User
{
    [JsonPropertyName("action_link")]
    public string? ActionLink { get; set; }

    [JsonPropertyName("app_metadata")]
    public Dictionary<string, object> AppMetadata { get; set; } = new();

    [JsonPropertyName("aud")]
    public string? Aud { get; set; }

    [JsonPropertyName("confirmation_sent_at")]
    public DateTime? ConfirmationSentAt { get; set; }

    [JsonPropertyName("confirmed_at")]
    public DateTime? ConfirmedAt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("email_confirmed_at")]
    public DateTime? EmailConfirmedAt { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("identities")]
    public List<UserIdentity>? Identities { get; set; } = new();

    [JsonPropertyName("invited_at")]
    public DateTime? InvitedAt { get; set; }

    [JsonPropertyName("last_sign_in_at")]
    public DateTime? LastSignInAt { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("phone_confirmed_at")]
    public DateTime? PhoneConfirmedAt { get; set; }

    [JsonPropertyName("recovery_sent_at")]
    public DateTime? RecoverySentAt { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("user_metadata")]
    public Dictionary<string, string>? UserMetadata { get; set; } = new();
}

/// <summary>
///     Ref: https://supabase.github.io/gotrue-js/interfaces/UserAttributes.html
/// </summary>
public class UserAttributes
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("email_change_token")]
    public string? EmailChangeToken { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    ///     A custom data object for user_metadata that a user can modify.Can be any JSON.
    /// </summary>
    [JsonPropertyName("data")]
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
///     Ref: https://supabase.github.io/gotrue-js/interfaces/VerifyEmailOTPParams.html
/// </summary>
public class VerifyOTPParams
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }
}

public class UserList
{
    [JsonPropertyName("aud")]
    public string Aud { get; set; }

    [JsonPropertyName("users")]
    public List<User> Users { get; set; } = new();
}

/// <summary>
///     Ref: https://supabase.github.io/gotrue-js/interfaces/UserIdentity.html
/// </summary>
public class UserIdentity
{
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("identity_data")]
    public Dictionary<string, object> IdentityData { get; set; } = new();

    [JsonPropertyName("last_sign_in_at")]
    public DateTime LastSignInAt { get; set; }

    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}
