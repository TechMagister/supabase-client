namespace SupabaseDb.Extensions;

public static class RangeExtensions
{
    /// <summary>
    ///     Transforms a C# Range to a Postgrest String.
    /// </summary>
    /// <param name="range"></param>
    /// <returns></returns>
    internal static string ToPostgresString(this IntRange range)
    {
        return $"[{range.Start},{range.End}]";
    }
}
