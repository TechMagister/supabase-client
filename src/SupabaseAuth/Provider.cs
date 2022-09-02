using SupabaseAuth.Attributes;

namespace SupabaseAuth;

/// <summary>
///     Providers available to Supabase
///     Ref: https://supabase.github.io/gotrue-js/modules.html#Provider
/// </summary>
public enum Provider
{
    [MapTo("apple")]
    Apple,

    [MapTo("azure")]
    Azure,

    [MapTo("bitbucket")]
    Bitbucket,

    [MapTo("discord")]
    Discord,

    [MapTo("facebook")]
    Facebook,

    [MapTo("github")]
    Github,

    [MapTo("gitlab")]
    Gitlab,

    [MapTo("google")]
    Google,

    [MapTo("keycloak")]
    KeyCloak,

    [MapTo("linkedin")]
    LinkedIn,

    [MapTo("notion")]
    Notion,

    [MapTo("slack")]
    Slack,

    [MapTo("spotify")]
    Spotify,

    [MapTo("twitch")]
    Twitch,

    [MapTo("twitter")]
    Twitter,

    [MapTo("workos")]
    WorkOS
}
