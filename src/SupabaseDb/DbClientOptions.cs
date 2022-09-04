using System.Globalization;

namespace SupabaseDb;

/// <summary>
///     Options that can be passed to the Client configuration
/// </summary>
public class DbClientOptions
{
    public const string DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFK";

    public readonly DateTimeStyles DateTimeStyles = DateTimeStyles.AdjustToUniversal;
    public string Schema { get; set; } = "public";

    public Dictionary<string, string> Headers { get; } = new();

    public Dictionary<string, string> QueryParams { get; set; } = new();
}
