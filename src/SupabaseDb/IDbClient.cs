using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SupabaseDb.Tests")]

namespace SupabaseDb;

public interface IDbClient
{
    public ITable<T> Table<T>() where T : BaseModel, new();
}
