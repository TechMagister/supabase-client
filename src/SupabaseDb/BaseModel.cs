using Newtonsoft.Json;
using SupabaseDb.Attributes;
using SupabaseDb.Responses;

namespace SupabaseDb;

/// <summary>
///     Abstract class that must be implemented by C# Postgrest Models.
/// </summary>
public abstract class BaseModel
{
    /// <summary>
    ///     Gets the value of the PrimaryKey from a model's instance as defined by the [PrimaryKey] attribute on a property on
    ///     the model.
    /// </summary>
    [JsonIgnore]
    public object? PrimaryKeyValue
    {
        get
        {
            var props = GetType().GetProperties();

            foreach (var prop in props)
            {
                var hasAttr = Attribute.GetCustomAttribute(prop, typeof(PrimaryKeyAttribute));

                if (hasAttr is PrimaryKeyAttribute) return prop.GetValue(this);
            }

            throw new Exception("Models must specify their Primary Key via the [PrimaryKey] Attribute");
        }
    }

    [JsonIgnore]
    public string TableName
    {
        get
        {
            var attribute = Attribute.GetCustomAttribute(GetType(), typeof(TableAttribute));

            return attribute is TableAttribute tableAttr
                ? tableAttr.Name
                : GetType().Name;
        }
    }

    /// <summary>
    ///     Gets the name of the PrimaryKey column on a model's instance as defined by the [PrimaryKey] attribute on a property
    ///     on the model.
    /// </summary>
    [JsonIgnore]
    public string PrimaryKeyColumn
    {
        get
        {
            var propertyInfos = GetType().GetProperties();

            foreach (var info in propertyInfos)
            {
                var hasAttr = Attribute.GetCustomAttribute(info, typeof(PrimaryKeyAttribute));

                if (hasAttr is PrimaryKeyAttribute pka) return pka.ColumnName;
            }

            throw new Exception("Models must specify their Primary Key via the [PrimaryKey] Attribute");
        }
    }

    public virtual Task<ModeledResponse<T>> Update<T>(IDbClient dbClientClient) where T : BaseModel, new()
    {
        return dbClientClient.Table<T>().Update((T)this);
    }

    public virtual Task Delete<T>(IDbClient dbClientClient) where T : BaseModel, new()
    {
        return dbClientClient.Table<T>().Delete((T)this);
    }
}
