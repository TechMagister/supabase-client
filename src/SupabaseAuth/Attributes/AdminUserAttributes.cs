using System.Text.Json.Serialization;

namespace SupabaseAuth.Attributes;

/// <summary>
///     Ref: https://supabase.github.io/gotrue-js/interfaces/AdminUserAttributes.html
/// </summary>
public class AdminUserAttributes : UserAttributes
{
    /// <summary>
    ///     A custom data object for app_metadata that. Can be any JSON serializable data.
    ///     Only a service role can modify
    ///     Note: GoTrue does not yest support creating a user with app metadata
    ///     (see:
    ///     https://github.com/supabase/gotrue-js/blob/d7b334a4283027c65814aa81715ffead262f0bfa/test/GoTrueApi.test.ts#L45)
    /// </summary>
    [JsonPropertyName("app_metadata")]
    public Dictionary<string, object> AppMetadata { get; set; } = new();

    /// <summary>
    ///     A custom data object for user_metadata. Can be any JSON serializable data.
    ///     Only a service role can modify.
    /// </summary>
    [JsonPropertyName("user_metadata")]
    public Dictionary<string, object> UserMetadata { get; set; } = new();

    /// <summary>
    ///     Sets if a user has confirmed their email address.
    ///     Only a service role can modify
    /// </summary>
    [JsonPropertyName("email_confirm")]
    public bool? EmailConfirm { get; set; }

    /// <summary>
    ///     Sets if a user has confirmed their phone number.
    ///     Only a service role can modify
    /// </summary>
    [JsonPropertyName("phone_confirm")]
    public bool? PhoneConfirm { get; set; }
}
