using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SupabaseDb.Converters;

namespace SupabaseDb;

/// <summary>
///     A StatelessClient that allows one-off API interactions.
/// </summary>
public static class StatelessClient
{
    /// <summary>
    ///     Custom Serializer resolvers and converters that will be used for encoding and decoding Postgrest JSON responses.
    ///     By default, Postgrest seems to use a date format that C# and Newtonsoft do not like, so this initial
    ///     configuration handles that.
    /// </summary>
    internal static JsonSerializerSettings SerializerSettings(DbClientOptions? options = null)
    {
        options ??= new DbClientOptions();

        return new JsonSerializerSettings
        {
            ContractResolver = new PostgrestContractResolver(),
            Converters =
            {
                // 2020-08-28T12:01:54.763231
                new IsoDateTimeConverter
                {
                    DateTimeStyles = options.DateTimeStyles,
                    DateTimeFormat = DbClientOptions.DateTimeFormat
                }
            }
        };
    }
}
