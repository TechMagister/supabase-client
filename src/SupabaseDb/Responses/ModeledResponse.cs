using Newtonsoft.Json;

namespace SupabaseDb.Responses;

/// <summary>
///     A representation of a successful Postgrest response that transforms the string response into a C# Modelled
///     response.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ModeledResponse<T> : BaseResponse
{
    public List<T> Models { get; } = new();

    public ModeledResponse(BaseResponse baseResponse, JsonSerializerSettings serializerSettings,
        bool shouldParse = true)
    {
        Content = baseResponse.Content.Trim();
        ResponseMessage = baseResponse.ResponseMessage;

        if (!shouldParse || string.IsNullOrEmpty(Content)) return;

        var token = Content[0];

        switch (token)
        {
            case '[':
                Models = JsonConvert.DeserializeObject<List<T>>(Content, serializerSettings) ?? new List<T>();
                break;
            case '{':
            {
                Models.Clear();
                var obj = JsonConvert.DeserializeObject<T>(Content, serializerSettings);
                if (obj != null) Models.Add(obj);
                break;
            }
        }
    }
}
