using SupabaseDb.Queries;
using SupabaseDb.Responses;

namespace SupabaseDb;

public interface ITable<T> where T : BaseModel, new()
{
    /// <summary>
    ///     Name of the Table parsed by the Model.
    /// </summary>
    string TableName { get; }

    /// <summary>
    ///     Add a Filter to a query request
    /// </summary>
    /// <param name="columnName">Column Name in Table.</param>
    /// <param name="op">Operation to perform.</param>
    /// <param name="criterion">
    ///     Value to filter with, must be a `string`, `List
    ///     <object>`, `Dictionary<string, object>`, `FullTextSearchConfig`, or `Range`
    /// </param>
    /// <returns></returns>
    Table<T> Filter(string columnName, Constants.Operator op, object? criterion);

    /// <summary>
    ///     Adds a NOT filter to the current query args.
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    Table<T> Not(QueryFilter? filter);

    /// <summary>
    ///     Adds a NOT filter to the current query args.
    ///     Allows queries like:
    ///     <code>
    /// await client.Table<User>().Not("status", Operators.Equal, "OFFLINE").Get();
    /// </code>
    /// </summary>
    /// <param name="columnName"></param>
    /// <param name="op"></param>
    /// <param name="criterion"></param>
    /// <returns></returns>
    Table<T> Not(string columnName, Constants.Operator op, string? criterion);

    /// <summary>
    ///     Adds a NOT filter to the current query args.
    ///     Allows queries like:
    ///     <code>
    /// await client.Table<User>().Not("status", Operators.In, new List<string> {"AWAY", "OFFLINE"}).Get();
    /// </code>
    /// </summary>
    /// <param name="columnName"></param>
    /// <param name="op"></param>
    /// <param name="criteria"></param>
    /// <returns></returns>
    Table<T> Not(string columnName, Constants.Operator op, List<object>? criteria);

    /// <summary>
    ///     Adds a NOT filter to the current query args.
    /// </summary>
    /// <param name="columnName"></param>
    /// <param name="op"></param>
    /// <param name="criteria"></param>
    /// <returns></returns>
    Table<T> Not(string columnName, Constants.Operator op, Dictionary<string, object>? criteria);

    /// <summary>
    ///     Adds an AND Filter to the current query args.
    /// </summary>
    /// <param name="filters"></param>
    /// <returns></returns>
    Table<T> And(List<QueryFilter>? filters);

    /// <summary>
    ///     Adds a NOT Filter to the current query args.
    /// </summary>
    /// <param name="filters"></param>
    /// <returns></returns>
    Table<T> Or(List<QueryFilter>? filters);

    /// <summary>
    ///     Finds all rows whose columns match the specified `query` object.
    /// </summary>
    /// <param name="query">The object to filter with, with column names as keys mapped to their filter values.</param>
    /// <returns></returns>
    Table<T> Match(Dictionary<string, string?> query);

    /// <summary>
    ///     Adds an ordering to the current query args.
    /// </summary>
    /// <param name="column">Column Name</param>
    /// <param name="ordering"></param>
    /// <param name="nullPosition"></param>
    /// <returns></returns>
    Table<T> Order(string column, Constants.Ordering ordering,
        Constants.NullPosition nullPosition = Constants.NullPosition.First);

    /// <summary>
    ///     Adds an ordering to the current query args.
    /// </summary>
    /// <param name="foreignTable"></param>
    /// <param name="column"></param>
    /// <param name="ordering"></param>
    /// <param name="nullPosition"></param>
    /// <returns></returns>
    Table<T> Order(string? foreignTable, string column, Constants.Ordering ordering,
        Constants.NullPosition nullPosition = Constants.NullPosition.First);

    /// <summary>
    ///     Sets a FROM range, similar to a `StartAt` query.
    /// </summary>
    /// <param name="from"></param>
    /// <returns></returns>
    Table<T> Range(int from);

    /// <summary>
    ///     Sets a bounded range to the current query.
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    Table<T> Range(int from, int to);

    /// <summary>
    ///     Select columns for query.
    /// </summary>
    /// <param name="columnQuery"></param>
    /// <returns></returns>
    Table<T> Select(string? columnQuery);

    /// <summary>
    ///     Sets a limit with an optional foreign table reference.
    /// </summary>
    /// <param name="limit"></param>
    /// <param name="foreignTableName"></param>
    /// <returns></returns>
    Table<T> Limit(int limit, string? foreignTableName = null);

    /// <summary>
    ///     By specifying the onConflict query parameter, you can make UPSERT work on a column(s) that has a UNIQUE constraint.
    /// </summary>
    /// <param name="columnName"></param>
    /// <returns></returns>
    Table<T> OnConflict(string? columnName);

    /// <summary>
    ///     Sets an offset with an optional foreign table reference.
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="foreignTableName"></param>
    /// <returns></returns>
    Table<T> Offset(int offset, string? foreignTableName = null);

    /// <summary>
    ///     Executes an INSERT query using the defined query params on the current instance.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="options"></param>
    /// <returns>A typed model response from the database.</returns>
    Task<ModeledResponse<T>> Insert(T model, QueryOptions? options = null);

    /// <summary>
    ///     Executes a BULK INSERT query using the defined query params on the current instance.
    /// </summary>
    /// <param name="models"></param>
    /// <param name="options"></param>
    /// <returns>A typed model response from the database.</returns>
    Task<ModeledResponse<T>> Insert(ICollection<T> models, QueryOptions? options = null);

    /// <summary>
    ///     Executes an UPSERT query using the defined query params on the current instance.
    ///     By default the new record is returned. Set QueryOptions.ReturnType to Minimal if you don't need this value.
    ///     By specifying the QueryOptions.OnConflict parameter, you can make UPSERT work on a column(s) that has a UNIQUE
    ///     constraint.
    ///     QueryOptions.DuplicateResolution.IgnoreDuplicates Specifies if duplicate rows should be ignored and not inserted.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    Task<ModeledResponse<T>> Upsert(T model, QueryOptions? options = null);

    /// <summary>
    ///     Executes an UPSERT query using the defined query params on the current instance.
    ///     By default the new record is returned. Set QueryOptions.ReturnType to Minimal if you don't need this value.
    ///     By specifying the QueryOptions.OnConflict parameter, you can make UPSERT work on a column(s) that has a UNIQUE
    ///     constraint.
    ///     QueryOptions.DuplicateResolution.IgnoreDuplicates Specifies if duplicate rows should be ignored and not inserted.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    Task<ModeledResponse<T>> Upsert(ICollection<T> model, QueryOptions? options = null);

    /// <summary>
    ///     Executes an UPDATE query using the defined query params on the current instance.
    /// </summary>
    /// <param name="model"></param>
    /// <returns>A typed response from the database.</returns>
    Task<ModeledResponse<T>> Update(T model, QueryOptions? options = null);

    /// <summary>
    ///     Executes a delete request using the defined query params on the current instance.
    /// </summary>
    /// <returns></returns>
    Task Delete(QueryOptions? options = null);

    /// <summary>
    ///     Executes a delete request using the model's primary key as the filter for the request.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    Task<ModeledResponse<T>> Delete(T model, QueryOptions? options = null);

    /// <summary>
    ///     Returns ONLY a count from the specified query.
    ///     See: https://postgrest.org/en/v7.0.0/api.html?highlight=count
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    Task<int> Count(Constants.CountType type);

    /// <summary>
    ///     Executes a query that expects to have a single object returned, rather than returning list of models
    ///     it will return a single model.
    /// </summary>
    /// <returns></returns>
    Task<T?> Single();

    /// <summary>
    ///     Executes the query using the defined filters on the current instance.
    /// </summary>
    /// <returns></returns>
    Task<ModeledResponse<T>> Get();

    /// <summary>
    ///     Clears currently defined query values.
    /// </summary>
    void Clear();
}
