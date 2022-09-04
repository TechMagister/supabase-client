using System.Collections;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Common.Attributes;
using Newtonsoft.Json;
using SupabaseDb.Attributes;
using SupabaseDb.Extensions;
using SupabaseDb.Queries;
using SupabaseDb.Responses;
using static SupabaseDb.Constants;

namespace SupabaseDb;

/// <summary>
///     Class created from a model derived from `BaseModel` that can generate query requests to a Postgrest Endpoint.
///     Representative of a `USE $TABLE` command.
/// </summary>
/// <typeparam name="T">Model derived from `BaseModel`.</typeparam>
public class Table<T> : ITable<T> where T : BaseModel, new()
{
    private readonly string _baseUrl;
    private readonly List<QueryFilter> _filters = new();
    private readonly DbClientOptions _options;
    private readonly List<QueryOrderer> _orderers = new();
    private readonly JsonSerializerSettings _serializerSettings;

    private string? _columnQuery;

    private int _limit = int.MinValue;
    private string? _limitForeignKey;

    private HttpMethod _method = HttpMethod.Get;

    private int _offset = int.MinValue;
    private string? _offsetForeignKey;

    private string? _onConflict;

    private int _rangeFrom = int.MinValue;
    private int _rangeTo = int.MinValue;

    /// <summary>
    ///     Typically called from the Client Singleton using `Client.Instance.Table&lt;T&gt;`
    /// </summary>
    /// <param name="baseUrl"></param>
    /// <param name="options">Optional client configuration.</param>
    public Table(string baseUrl, DbClientOptions? options = null)
    {
        options ??= new DbClientOptions();

        _baseUrl = baseUrl;
        _options = options;

        _serializerSettings = StatelessClient.SerializerSettings(options);

        var attr = Attribute.GetCustomAttribute(typeof(T), typeof(TableAttribute));

        if (attr is TableAttribute tableAttr)
        {
            TableName = tableAttr.Name;
            return;
        }

        TableName = typeof(T).Name;
    }

    /// <summary>
    ///     Constructor that specifies the serializer settings
    /// </summary>
    /// <param name="baseUrl"></param>
    /// <param name="options"></param>
    /// <param name="serializerSettings"></param>
    public Table(string baseUrl, DbClientOptions options, JsonSerializerSettings serializerSettings) : this(baseUrl,
        options)
    {
        _serializerSettings = serializerSettings;
    }

    /// <summary>
    ///     Generates the encoded URL with defined query parameters that will be sent to the Postgrest API.
    /// </summary>
    /// <returns></returns>
    internal string GenerateUrl()
    {
        var builder = new UriBuilder($"{_baseUrl}/{TableName}");
        var query = HttpUtility.ParseQueryString(builder.Query);

        foreach (var param in _options.QueryParams) query.Add(param.Key, param.Value);

        if (_options.Headers.ContainsKey("apikey")) query.Add("apikey", _options.Headers["apikey"]);

        foreach (var filter in _filters)
        {
            var parsedFilter = PrepareFilter(filter);
            query.Add(parsedFilter.Key, parsedFilter.Value);
        }

        foreach (var orderer in _orderers)
        {
            var nullPosAttr = orderer.NullPosition.GetAttribute<MapToAttribute>();
            var orderingAttr = orderer.Ordering.GetAttribute<MapToAttribute>();
            if (nullPosAttr is { } nullPosAsAttribute && orderingAttr is { } orderingAsAttribute)
            {
                var key = !string.IsNullOrEmpty(orderer.ForeignTable) ? $"{orderer.ForeignTable}.order" : "order";
                query.Add(key, $"{orderer.Column}.{orderingAsAttribute.Mapping}.{nullPosAsAttribute.Mapping}");
            }
        }

        if (!string.IsNullOrEmpty(_columnQuery)) query["select"] = Regex.Replace(_columnQuery, @"\s", "");

        if (!string.IsNullOrEmpty(_onConflict)) query["on_conflict"] = _onConflict;

        if (_limit != int.MinValue)
        {
            var key = _limitForeignKey != null ? $"{_limitForeignKey}.limit" : "limit";
            query[key] = _limit.ToString();
        }

        if (_offset != int.MinValue)
        {
            var key = _offsetForeignKey != null ? $"{_offsetForeignKey}.offset" : "offset";
            query[key] = _offset.ToString();
        }

        builder.Query = query.ToString();
        return builder.Uri.ToString();
    }

    /// <summary>
    ///     Transforms an object into a string mapped list/dictionary using `JsonSerializerSettings`.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    internal IEnumerable? PrepareRequestData(object? data)
    {
        switch (data)
        {
            case null:
                return new Dictionary<string, string>();
            // Check if data is a Collection for the Insert Bulk case
            case ICollection<T>:
            {
                var serialized = JsonConvert.SerializeObject(data, _serializerSettings);
                return JsonConvert.DeserializeObject<List<object>>(serialized);
            }
            default:
            {
                var serialized = JsonConvert.SerializeObject(data, _serializerSettings);
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(serialized, _serializerSettings);
            }
        }
    }

    /// <summary>
    ///     Transforms the defined filters into the expected Postgrest format.
    ///     See: http://postgrest.org/en/v7.0.0/api.html#operators
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    internal KeyValuePair<string, string> PrepareFilter(QueryFilter filter)
    {
        var attr = filter.Op.GetAttribute<MapToAttribute>();
        if (attr is { } asAttribute)
        {
            var strBuilder = new StringBuilder();
            switch (filter.Op)
            {
                case Operator.Or:
                case Operator.And:
                    if (filter.Criteria is List<QueryFilter> subFilters)
                    {
                        var list = new List<KeyValuePair<string, string>>();
                        foreach (var subFilter in subFilters)
                            list.Add(PrepareFilter(subFilter));

                        foreach (var preppedFilter in list)
                            strBuilder.Append($"{preppedFilter.Key}.{preppedFilter.Value},");

                        return new KeyValuePair<string, string>(asAttribute.Mapping,
                            $"({strBuilder.ToString().Trim(',')})");
                    }

                    break;
                case Operator.Not:
                    if (filter.Criteria is QueryFilter notFilter)
                    {
                        var prepped = PrepareFilter(notFilter);
                        return new KeyValuePair<string, string>(prepped.Key, $"not.{prepped.Value}");
                    }

                    break;
                case Operator.Like:
                case Operator.ILike:
                    if (filter.Criteria is string likeCriteria)
                        return new KeyValuePair<string, string>(filter.Property,
                            $"{asAttribute.Mapping}.{likeCriteria.Replace("%", "*")}");
                    break;
                case Operator.In:
                    if (filter.Criteria is List<object> inCriteria)
                    {
                        foreach (var item in inCriteria)
                            strBuilder.Append($"\"{item}\",");

                        return new KeyValuePair<string, string>(filter.Property,
                            $"{asAttribute.Mapping}.({strBuilder.ToString().Trim(',')})");
                    }

                    if (filter.Criteria is Dictionary<string, object> filtCriteria)
                        return new KeyValuePair<string, string>(filter.Property,
                            $"{asAttribute.Mapping}.{JsonConvert.SerializeObject(filtCriteria)}");
                    break;
                case Operator.Contains:
                case Operator.ContainedIn:
                case Operator.Overlap:
                    if (filter.Criteria is List<object> listCriteria)
                    {
                        foreach (var item in listCriteria)
                            strBuilder.Append($"{item},");

                        return new KeyValuePair<string, string>(filter.Property,
                            $"{asAttribute.Mapping}.{{{strBuilder.ToString().Trim(',')}}}");
                    }

                    if (filter.Criteria is Dictionary<string, object> dictCriteria)
                        return new KeyValuePair<string, string>(filter.Property,
                            $"{asAttribute.Mapping}.{JsonConvert.SerializeObject(dictCriteria)}");
                    if (filter.Criteria is IntRange rangeCriteria)
                        return new KeyValuePair<string, string>(filter.Property,
                            $"{asAttribute.Mapping}.{rangeCriteria.ToPostgresString()}");
                    break;
                case Operator.StrictlyLeft:
                case Operator.StrictlyRight:
                case Operator.NotRightOf:
                case Operator.NotLeftOf:
                case Operator.Adjacent:
                    if (filter.Criteria is IntRange rangeCritera)
                        return new KeyValuePair<string, string>(filter.Property,
                            $"{asAttribute.Mapping}.{rangeCritera.ToPostgresString()}");
                    break;
                case Operator.FTS:
                case Operator.PHFTS:
                case Operator.PLFTS:
                case Operator.WFTS:
                    if (filter.Criteria is FullTextSearchConfig searchConfig)
                        return new KeyValuePair<string, string>(filter.Property,
                            $"{asAttribute.Mapping}({searchConfig.Config}).{searchConfig.QueryText}");
                    break;
                default:
                    return new KeyValuePair<string, string>(filter.Property,
                        $"{asAttribute.Mapping}.{filter.Criteria}");
            }
        }

        return new KeyValuePair<string, string>();
    }


    /// <summary>
    ///     Performs an INSERT Request.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    private Task<ModeledResponse<T>> PerformInsert(object data, QueryOptions? options = null)
    {
        _method = HttpMethod.Post;
        if (options == null)
            options = new QueryOptions();

        if (!string.IsNullOrEmpty(options.OnConflict)) OnConflict(options.OnConflict);

        var request = Send<T>(_method, data, options.ToHeaders());

        Clear();

        return request;
    }

    private Task<BaseResponse> Send(HttpMethod method, object? data, Dictionary<string, string>? headers = null)
    {
        var requestHeaders = Helpers.PrepareRequestHeaders(method, headers, _options, _rangeFrom, _rangeTo);
        return Helpers.MakeRequest(method, GenerateUrl(), _serializerSettings, PrepareRequestData(data),
            requestHeaders);
    }

    private Task<ModeledResponse<TU>> Send<TU>(HttpMethod method, object? data,
        Dictionary<string, string>? headers = null) where TU : BaseModel, new()
    {
        var requestHeaders = Helpers.PrepareRequestHeaders(method, headers, _options, _rangeFrom, _rangeTo);
        return Helpers.MakeRequest<TU>(method, GenerateUrl(), _serializerSettings, PrepareRequestData(data),
            requestHeaders);
    }

    /// <summary>
    ///     Name of the Table parsed by the Model.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    ///     Add a Filter to a query request
    /// </summary>
    /// <param name="columnName">Column Name in Table.</param>
    /// <param name="op">Operation to perform.</param>
    /// <param name="criterion">
    ///     Value to filter with, must be a `string`, `List&lt;object&gt;`,
    ///     `Dictionary&lt;string, object&gt;`, `FullTextSearchConfig`, or `Range`
    /// </param>
    /// <returns></returns>
    public Table<T> Filter(string columnName, Operator op, object? criterion)
    {
        if (criterion == null)
        {
            switch (op)
            {
                case Operator.Equals:
                case Operator.Is:
                    _filters.Add(new QueryFilter(columnName, Operator.Is, QueryFilter.NULL_VAL));
                    break;
                case Operator.Not:
                case Operator.NotEqual:
                    _filters.Add(new QueryFilter(columnName, Operator.Not,
                        new QueryFilter(columnName, Operator.Is, QueryFilter.NULL_VAL)));
                    break;
                default:
                    throw new Exception("NOT filters must use the `Equals`, `Is`, `Not` or `NotEqual` operators");
            }

            return this;
        }

        if (criterion is string stringCriterion)
        {
            _filters.Add(new QueryFilter(columnName, op, stringCriterion));
            return this;
        }

        if (criterion is int intCriterion)
        {
            _filters.Add(new QueryFilter(columnName, op, intCriterion));
            return this;
        }

        if (criterion is float floatCriterion)
        {
            _filters.Add(new QueryFilter(columnName, op, floatCriterion));
            return this;
        }

        if (criterion is List<object> listCriteria)
        {
            _filters.Add(new QueryFilter(columnName, op, listCriteria));
            return this;
        }

        if (criterion is Dictionary<string, object> dictCriteria)
        {
            _filters.Add(new QueryFilter(columnName, op, dictCriteria));
            return this;
        }

        if (criterion is IntRange rangeCriteria)
        {
            _filters.Add(new QueryFilter(columnName, op, rangeCriteria));
            return this;
        }

        if (criterion is FullTextSearchConfig fullTextSearchCriteria)
        {
            _filters.Add(new QueryFilter(columnName, op, fullTextSearchCriteria));
            return this;
        }

        throw new Exception(
            "Unknown criterion type, is it of type `string`, `int`, `float`, `List`, `Dictionary<string, object>`, `FullTextSearchConfig`, or `Range`?");
    }

    /// <summary>
    ///     Adds a NOT filter to the current query args.
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public Table<T> Not(QueryFilter? filter)
    {
        _filters.Add(new QueryFilter(Operator.Not, filter));
        return this;
    }

    /// <summary>
    ///     Adds a NOT filter to the current query args.
    ///     Allows queries like:
    ///     <code>
    /// await client.Table&lt;User&gt;().Not("status", Operators.Equal, "OFFLINE").Get();
    /// </code>
    /// </summary>
    /// <param name="columnName"></param>
    /// <param name="op"></param>
    /// <param name="criterion"></param>
    /// <returns></returns>
    public Table<T> Not(string columnName, Operator op, string? criterion)
    {
        return Not(new QueryFilter(columnName, op, criterion));
    }

    /// <summary>
    ///     Adds a NOT filter to the current query args.
    ///     Allows queries like:
    ///     <code>
    /// await client.Table&lt;User&gt;().Not("status", Operators.In, new List&lt;string&gt; {"AWAY", "OFFLINE"}).Get();
    /// </code>
    /// </summary>
    /// <param name="columnName"></param>
    /// <param name="op"></param>
    /// <param name="criteria"></param>
    /// <returns></returns>
    public Table<T> Not(string columnName, Operator op, List<object>? criteria)
    {
        return Not(new QueryFilter(columnName, op, criteria));
    }

    /// <summary>
    ///     Adds a NOT filter to the current query args.
    /// </summary>
    /// <param name="columnName"></param>
    /// <param name="op"></param>
    /// <param name="criteria"></param>
    /// <returns></returns>
    public Table<T> Not(string columnName, Operator op, Dictionary<string, object>? criteria)
    {
        return Not(new QueryFilter(columnName, op, criteria));
    }

    /// <summary>
    ///     Adds an AND Filter to the current query args.
    /// </summary>
    /// <param name="filters"></param>
    /// <returns></returns>
    public Table<T> And(List<QueryFilter>? filters)
    {
        _filters.Add(new QueryFilter(Operator.And, filters));
        return this;
    }

    /// <summary>
    ///     Adds a NOT Filter to the current query args.
    /// </summary>
    /// <param name="filters"></param>
    /// <returns></returns>
    public Table<T> Or(List<QueryFilter>? filters)
    {
        _filters.Add(new QueryFilter(Operator.Or, filters));
        return this;
    }

    /// <summary>
    ///     Finds all rows whose columns match the specified `query` object.
    /// </summary>
    /// <param name="query">The object to filter with, with column names as keys mapped to their filter values.</param>
    /// <returns></returns>
    public Table<T> Match(Dictionary<string, string?> query)
    {
        foreach (var param in query) _filters.Add(new QueryFilter(param.Key, Operator.Equals, param.Value));

        return this;
    }

    /// <summary>
    ///     Adds an ordering to the current query args.
    /// </summary>
    /// <param name="column">Column Name</param>
    /// <param name="ordering"></param>
    /// <param name="nullPosition"></param>
    /// <returns></returns>
    public Table<T> Order(string column, Ordering ordering,
        NullPosition nullPosition = NullPosition.First)
    {
        _orderers.Add(new QueryOrderer(null, column, ordering, nullPosition));
        return this;
    }

    /// <summary>
    ///     Adds an ordering to the current query args.
    /// </summary>
    /// <param name="foreignTable"></param>
    /// <param name="column"></param>
    /// <param name="ordering"></param>
    /// <param name="nullPosition"></param>
    /// <returns></returns>
    public Table<T> Order(string? foreignTable, string column, Ordering ordering,
        NullPosition nullPosition = NullPosition.First)
    {
        _orderers.Add(new QueryOrderer(foreignTable, column, ordering, nullPosition));
        return this;
    }

    /// <summary>
    ///     Sets a FROM range, similar to a `StartAt` query.
    /// </summary>
    /// <param name="from"></param>
    /// <returns></returns>
    public Table<T> Range(int from)
    {
        _rangeFrom = from;
        return this;
    }

    /// <summary>
    ///     Sets a bounded range to the current query.
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public Table<T> Range(int from, int to)
    {
        _rangeFrom = from;
        _rangeTo = to;
        return this;
    }

    /// <summary>
    ///     Select columns for query.
    /// </summary>
    /// <param name="columnQuery"></param>
    /// <returns></returns>
    public Table<T> Select(string? columnQuery)
    {
        _method = HttpMethod.Get;
        _columnQuery = columnQuery;
        return this;
    }


    /// <summary>
    ///     Sets a limit with an optional foreign table reference.
    /// </summary>
    /// <param name="limit"></param>
    /// <param name="foreignTableName"></param>
    /// <returns></returns>
    public Table<T> Limit(int limit, string? foreignTableName = null)
    {
        _limit = limit;
        _limitForeignKey = foreignTableName;
        return this;
    }

    /// <summary>
    ///     By specifying the onConflict query parameter, you can make UPSERT work on a column(s) that has a UNIQUE constraint.
    /// </summary>
    /// <param name="columnName"></param>
    /// <returns></returns>
    public Table<T> OnConflict(string? columnName)
    {
        _onConflict = columnName;
        return this;
    }


    /// <summary>
    ///     Sets an offset with an optional foreign table reference.
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="foreignTableName"></param>
    /// <returns></returns>
    public Table<T> Offset(int offset, string? foreignTableName = null)
    {
        _offset = offset;
        _offsetForeignKey = foreignTableName;
        return this;
    }

    /// <summary>
    ///     Executes an INSERT query using the defined query params on the current instance.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="options"></param>
    /// <returns>A typed model response from the database.</returns>
    public Task<ModeledResponse<T>> Insert(T model, QueryOptions? options = null)
    {
        return PerformInsert(model, options);
    }

    /// <summary>
    ///     Executes a BULK INSERT query using the defined query params on the current instance.
    /// </summary>
    /// <param name="models"></param>
    /// <param name="options"></param>
    /// <returns>A typed model response from the database.</returns>
    public Task<ModeledResponse<T>> Insert(ICollection<T> models, QueryOptions? options = null)
    {
        return PerformInsert(models, options);
    }

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
    public Task<ModeledResponse<T>> Upsert(T model, QueryOptions? options = null)
    {
        options ??= new QueryOptions();

        // Enforce Upsert
        options.Upsert = true;

        return PerformInsert(model, options);
    }

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
    public Task<ModeledResponse<T>> Upsert(ICollection<T> model, QueryOptions? options = null)
    {
        options ??= new QueryOptions();

        // Enforce Upsert
        options.Upsert = true;

        return PerformInsert(model, options);
    }

    /// <summary>
    ///     Executes an UPDATE query using the defined query params on the current instance.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="options"></param>
    /// <exception cref="InvalidOperationException">PrimaryKeyColumn should not be null</exception>
    /// <returns>A typed response from the database.</returns>
    public Task<ModeledResponse<T>> Update(T model, QueryOptions? options = null)
    {
        options ??= new QueryOptions();

        _method = new HttpMethod("PATCH");

        _filters.Add(new QueryFilter(
            model.PrimaryKeyColumn ?? throw new InvalidOperationException("PrimaryKeyColumn should not be null"),
            Operator.Equals,
            model.PrimaryKeyValue?.ToString()));

        var request = Send<T>(_method, model, options.ToHeaders());

        Clear();

        return request;
    }

    /// <summary>
    ///     Executes a delete request using the defined query params on the current instance.
    /// </summary>
    /// <returns></returns>
    public Task Delete(QueryOptions? options = null)
    {
        options ??= new QueryOptions();

        _method = HttpMethod.Delete;

        var request = Send(_method, null, options.ToHeaders());

        Clear();

        return request;
    }

    /// <summary>
    ///     Executes a delete request using the model's primary key as the filter for the request.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public Task<ModeledResponse<T>> Delete(T model, QueryOptions? options = null)
    {
        options ??= new QueryOptions();

        _method = HttpMethod.Delete;
        Filter(model.PrimaryKeyColumn, Operator.Equals, model.PrimaryKeyValue?.ToString());
        var request = Send<T>(_method, null, options.ToHeaders());
        Clear();
        return request;
    }

    /// <summary>
    ///     Returns ONLY a count from the specified query.
    ///     See: https://postgrest.org/en/v7.0.0/api.html?highlight=count
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public Task<int> Count(CountType type)
    {
        var tsc = new TaskCompletionSource<int>();

        Task.Run(async () =>
        {
            _method = HttpMethod.Head;

            var attr = type.GetAttribute<MapToAttribute>();

            var headers = new Dictionary<string, string>
            {
                { "Prefer", $"count={attr.Mapping}" }
            };

            var request = Send(_method, null, headers);
            Clear();

            try
            {
                var response = await request;
                var countStr = response.ResponseMessage.Content.Headers.GetValues("Content-Range").FirstOrDefault();
                if (countStr != null && countStr.Contains('/'))
                    // Returns X-Y/COUNT [0-3/4]
                    tsc.SetResult(int.Parse(countStr.Split('/')[1]));
                tsc.SetException(new Exception("Failed to parse response."));
            }
            catch (Exception ex)
            {
                tsc.SetException(ex);
            }
        });

        return tsc.Task;
    }

    /// <summary>
    ///     Executes a query that expects to have a single object returned, rather than returning list of models
    ///     it will return a single model.
    /// </summary>
    /// <returns></returns>
    public Task<T?> Single()
    {
        var tsc = new TaskCompletionSource<T?>();

        Task.Run(async () =>
        {
            _method = HttpMethod.Get;
            var headers = new Dictionary<string, string>
            {
                { "Accept", "application/vnd.pgrst.object+json" },
                { "Prefer", "return=representation" }
            };

            var request = Send<T>(_method, null, headers);

            Clear();

            try
            {
                var result = await request;
                tsc.SetResult(result.Models.FirstOrDefault());
            }
            catch (RequestException e)
            {
                // No rows returned
                if (e.Response.StatusCode == HttpStatusCode.NotAcceptable)
                    tsc.SetResult(null);
                else
                    tsc.SetException(e);
            }
            catch (Exception e)
            {
                tsc.SetException(e);
            }
        });

        return tsc.Task;
    }

    /// <summary>
    ///     Executes the query using the defined filters on the current instance.
    /// </summary>
    /// <returns></returns>
    public Task<ModeledResponse<T>> Get()
    {
        var request = Send<T>(_method, null);
        Clear();
        return request;
    }

    /// <summary>
    ///     Clears currently defined query values.
    /// </summary>
    public void Clear()
    {
        _columnQuery = null;

        _filters.Clear();
        _orderers.Clear();

        _rangeFrom = int.MinValue;
        _rangeTo = int.MinValue;

        _limit = int.MinValue;
        _limitForeignKey = null;

        _offset = int.MinValue;
        _offsetForeignKey = null;

        _onConflict = null;
    }
}
