using Postgrest;
using Postgrest.Models;
using Supabase.Realtime;
using Client = Supabase.Realtime.Client;
using ClientOptions = Postgrest.ClientOptions;

namespace SupabaseClient;

public class SupabaseTable<T> : Table<T> where T : BaseModel, new()
{
    private readonly ClientOptions _options;
    private readonly Client _realtimeClient;
    private Channel? channel;

    public SupabaseTable(string restUrl, ClientOptions options, Client realtimeClient) : base(restUrl, options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _realtimeClient = realtimeClient ?? throw new ArgumentNullException(nameof(realtimeClient));
    }

    public async Task<Channel> On(ChannelEventType e, Action<object?, SocketResponseEventArgs> action)
    {
        if (channel == null)
        {
            var parameters = new Dictionary<string, string>();

            // In regard to: https://github.com/supabase/supabase-js/pull/270
            var headers = _options.Headers;
            if (headers.ContainsKey("Authorization"))
                parameters.Add("user_token", headers["Authorization"].Split(' ')[1]);

            channel = _realtimeClient.Channel("realtime", _options.Schema, TableName, parameters: parameters);
        }

        if (_realtimeClient.Socket == null || !_realtimeClient.Socket.IsConnected)
            await _realtimeClient.ConnectAsync();

        switch (e)
        {
            case ChannelEventType.Insert:
                channel.OnInsert += (sender, args) => action.Invoke(sender, args);
                break;
            case ChannelEventType.Update:
                channel.OnUpdate += (sender, args) => action.Invoke(sender, args);
                break;
            case ChannelEventType.Delete:
                channel.OnDelete += (sender, args) => action.Invoke(sender, args);
                break;
            case ChannelEventType.All:
                channel.OnMessage += (sender, args) => action.Invoke(sender, args);
                break;
        }

        try
        {
            await channel.Subscribe();
        }
        catch
        {
        }

        return channel;
    }
}
