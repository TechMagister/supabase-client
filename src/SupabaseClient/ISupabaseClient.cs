using Postgrest.Models;
using Postgrest.Responses;

namespace SupabaseClient;

public interface ISupabaseClient
{
    /// <summary>
    ///     Gets the Postgrest client to prepare for a query.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    SupabaseTable<T> From<T>() where T : BaseModel, new();

    /// <summary>
    ///     Runs a remote procedure.
    /// </summary>
    /// <param name="procedureName"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    Task<BaseResponse> Rpc(string procedureName, Dictionary<string, object> parameters);
}
