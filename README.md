<p align="center">
<img width="300" src="./docs/WIP.jpg"/>
</p>
<h3 align="center">Stage: Alpha</h3>

Integrate your [Supabase](https://supabase.io) projects with C#.

Includes C# features to make supabase function more like an ORM - specifically the ability to leverage **strongly typed
models**.

Inspired by and reuse the code from : 
- [Supabase.CSharp](https://github.com/supabase-community/Supabase-client)
- [Postgrest](https://github.com/supabase-community/postgrest-csharp)
- [Gotrue](https://github.com/supabase-community/gotrue-csharp)

## Why this project and not Supabase-client ? 

- Remove singletons
- Allow better integration with DI
- Use nullable everywhere
- Use System.Text.Json instead of Newtonsoft (except when it's not possible)

## Status

- [x] Integration with GoTrue
- [x] Integration with Postgrest
- [ ] Integration with Realtime
- [ ] Integration with Storage
- [ ] Integration with Edge Functions
- [ ] Nuget Release


## Getting Started

Getting started will be pretty easy, allowing dependencies injection.

@TODO 

### Models:

Supabase-client is _heavily_ dependent on Models deriving from `BaseModel`). To interact with the API, one must have the associated model specified.

Leverage `Table`,`PrimaryKey`, and `Column` attributes to specify names of classes/properties that are different from
their C# Versions.

```c#
[Table("messages")]
public class Message : SupabaseModel
{
    // `ShouldInsert` Set to false so-as to honor DB generated key
    // If the primary key was set by the application, this could be omitted.
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("username")]
    public string UserName { get; set; }

    [Column("channel_id")]
    public int ChannelId { get; set; }
}
```

## Contributing

We are more than happy to have contributions! Please submit a PR.
