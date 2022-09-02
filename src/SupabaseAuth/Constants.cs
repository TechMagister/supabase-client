using SupabaseAuth.Attributes;

namespace SupabaseAuth;

public static class Constants
{
    public enum SortOrder
    {
        [MapTo("asc")]
        Ascending,

        [MapTo("desc")]
        Descending
    }

    public static readonly Dictionary<string, string> DefaultHeaders = new();
}
