using System.Text;
using System.Text.Json;
using System.Web;
using SupabaseAuth.Attributes;
using SupabaseAuth.Responses;

namespace SupabaseAuth;

internal static class Helpers
{
    private static readonly HttpClient Client = new();

    public static T? GetPropertyValue<T>(object obj, string propName)
    {
        return (T?)obj.GetType().GetProperty(propName)?.GetValue(obj, null);
    }

    public static T? GetCustomAttribute<T>(object obj) where T : Attribute
    {
        return (T?)Attribute.GetCustomAttribute(obj.GetType(), typeof(T));
    }

    public static T? GetCustomAttribute<T>(Type type) where T : Attribute
    {
        return (T?)Attribute.GetCustomAttribute(type, typeof(T));
    }

    public static MapToAttribute GetMappedToAttr(Enum obj)
    {
        var type = obj.GetType();
        var name = Enum.GetName(type, obj);

        return type.GetField(name).GetCustomAttributes(false).OfType<MapToAttribute>().SingleOrDefault();
    }

    /// <summary>
    ///     Adds query params to a given Url
    /// </summary>
    /// <param name="url"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    internal static Uri AddQueryParams(string url, Dictionary<string, string> data)
    {
        var builder = new UriBuilder(url);
        var query = HttpUtility.ParseQueryString(builder.Query);

        foreach (var param in data)
            query[param.Key] = param.Value;

        builder.Query = query.ToString();

        return builder.Uri;
    }

    /// <summary>
    ///     Helper to make a request using the defined parameters to an API Endpoint and coerce into a model.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="method"></param>
    /// <param name="url"></param>
    /// <param name="data"></param>
    /// <param name="headers"></param>
    /// <returns></returns>
    public static async Task<T?> MakeRequest<T>(HttpMethod method, string url, object? data = null,
        Dictionary<string, string>? headers = null)
    {
        var baseResponse = await MakeRequest(method, url, data, headers);
        return JsonSerializer.Deserialize<T>(baseResponse.Content);
    }

    /// <summary>
    ///     Helper to make a request using the defined parameters to an API Endpoint.
    /// </summary>
    /// <param name="method"></param>
    /// <param name="url"></param>
    /// <param name="data"></param>
    /// <param name="headers"></param>
    /// <returns></returns>
    public static async Task<BaseResponse> MakeRequest(HttpMethod method, string url, object? data = null,
        Dictionary<string, string>? headers = null)
    {
        var builder = new UriBuilder(url);
        var query = HttpUtility.ParseQueryString(builder.Query);

        if (data != null && method == HttpMethod.Get)
            // Case if it's a Get request the data object is a dictionary<string,string>
            if (data is Dictionary<string, string> reqParams)
                foreach (var param in reqParams)
                    query[param.Key] = param.Value;

        builder.Query = query.ToString();

        using (var requestMessage = new HttpRequestMessage(method, builder.Uri))
        {
            if (data != null && method != HttpMethod.Get)
                requestMessage.Content =
                    new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

            if (headers != null)
                foreach (var kvp in headers)
                    requestMessage.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);

            var response = await Client.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var obj = new ErrorResponse
                {
                    Content = content,
                    Message = content
                };
                throw new RequestException(response, obj);
            }

            return new BaseResponse { Content = content, ResponseMessage = response };
        }
    }
}

public class RequestException : Exception
{
    public HttpResponseMessage Response { get; }
    public ErrorResponse Error { get; }

    public RequestException(HttpResponseMessage response, ErrorResponse error) : base(error.Message)
    {
        Response = response;
        Error = error;
    }
}
