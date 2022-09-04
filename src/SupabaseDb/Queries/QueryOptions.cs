using Common.Attributes;
using SupabaseDb.Extensions;

namespace SupabaseDb.Queries;

public class QueryOptions
{
    public enum CountType
    {
        [MapTo("none")]
        None,

        [MapTo("exact")]
        Exact,

        [MapTo("planned")]
        Planned,

        [MapTo("estimated")]
        Estimated
    }

    public enum DuplicateResolutionType
    {
        [MapTo("merge-duplicates")]
        MergeDuplicates,

        [MapTo("ignore-duplicates")]
        IgnoreDuplicates
    }

    public enum ReturnType
    {
        [MapTo("minimal")]
        Minimal,

        [MapTo("representation")]
        Representation
    }

    /// <summary>
    ///     By default the new record is returned. Set this to 'Minimal' if you don't need this value.
    /// </summary>
    public ReturnType Returning { get; set; } = ReturnType.Representation;

    /// <summary>
    ///     Specifies if duplicate rows should be ignored and not inserted.
    /// </summary>
    public DuplicateResolutionType DuplicateResolution { get; set; } = DuplicateResolutionType.MergeDuplicates;

    /// <summary>
    ///     Count algorithm to use to count rows in a table.
    /// </summary>
    public CountType Count { get; set; } = CountType.None;

    /// <summary>
    ///     If the record should be upserted
    /// </summary>
    public bool Upsert { get; set; }

    /// <summary>
    ///     /// By specifying the onConflict query parameter, you can make UPSERT work on a column(s) that has a UNIQUE
    ///     constraint.
    /// </summary>
    public string? OnConflict { get; set; }

    public Dictionary<string, string> ToHeaders()
    {
        var headers = new Dictionary<string, string>();
        var prefersHeaders = new List<string>();

        if (Upsert)
        {
            var resolverAttr = DuplicateResolution.GetAttribute<MapToAttribute>();
            prefersHeaders.Add($"resolution={resolverAttr.Mapping}");
        }

        var returnAttr = Returning.GetAttribute<MapToAttribute>();
        if (returnAttr != null) prefersHeaders.Add($"return={returnAttr.Mapping}");

        var countAttr = Count.GetAttribute<MapToAttribute>();
        if (Count != CountType.None && countAttr != null) prefersHeaders.Add($"count={countAttr.Mapping}");

        headers.Add("Prefer", string.Join(",", prefersHeaders.ToArray()));

        if (Returning == ReturnType.Minimal) headers.Add("Accept", "*/*");

        return headers;
    }
}
