using Newtonsoft.Json;
using SupabaseDb.Responses;

namespace SupabaseDb;

/// <summary>
///     A Singleton that represents a single, reusable connection to a Postgrest endpoint. Should be first called with the
///     `Initialize()` method.
/// </summary>
public class DbClient : IDbClient
{
    private readonly DbClientOptions _options;

    /// <summary>
    ///     API Base Url for subsequent calls.
    /// </summary>
    public string BaseUrl { get; }

    public DbClient(string baseUrl, DbClientOptions? options = null)
    {
        _options = options ?? new DbClientOptions();
        BaseUrl = baseUrl;
    }

    /// <summary>
    ///     Perform a stored procedure call.
    /// </summary>
    /// <param name="procedureName">The function name to call</param>
    /// <param name="parameters">The parameters to pass to the function call</param>
    /// <returns></returns>
    public Task<BaseResponse> Rpc(string procedureName, Dictionary<string, object> parameters)
    {
        // Build Uri
        var builder = new UriBuilder($"{BaseUrl}/rpc/{procedureName}");

        var canonicalUri = builder.Uri.ToString();

        var serializerSettings = StatelessClient.SerializerSettings(_options);

        // Prepare parameters
        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(
            JsonConvert.SerializeObject(parameters, serializerSettings));

        // Prepare headers
        var headers = Helpers.PrepareRequestHeaders(HttpMethod.Post,
            new Dictionary<string, string>(_options.Headers), _options);

        // Send request
        var request = Helpers.MakeRequest(HttpMethod.Post, canonicalUri, serializerSettings, data, headers);
        return request;
    }

    /// <summary>
    ///     Returns a Table Query Builder instance for a defined model - representative of `USE $TABLE`
    /// </summary>
    /// <typeparam name="T">Custom Model derived from `BaseModel`</typeparam>
    /// <returns></returns>
    public ITable<T> Table<T>() where T : BaseModel, new()
    {
        return new Table<T>(BaseUrl, _options, StatelessClient.SerializerSettings(_options));
    }
}
