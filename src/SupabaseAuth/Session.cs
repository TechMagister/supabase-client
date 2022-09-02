using System.Text.Json.Serialization;

namespace SupabaseAuth;

/// <summary>
///     Represents a Gotrue Session
/// </summary>
public class Session
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("user")]
    public User? User { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; } = DateTime.Now;

    public DateTime ExpiresAt()
    {
        return new DateTime(CreatedAt.Ticks).AddSeconds(ExpiresIn);
    }
}
