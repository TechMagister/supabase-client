using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Supabase;
using Supabase.Storage;
using SupabaseExample.Models;
using Client = Supabase.Client;
using FileOptions = Supabase.Storage.FileOptions;

namespace SupabaseExample
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            // Be sure to set this in your Debug Options.
            var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
            var key = Environment.GetEnvironmentVariable("SUPABASE_KEY");

            await Client.InitializeAsync(url, key,
                new SupabaseOptions { AutoConnectRealtime = true, ShouldInitializeRealtime = true });

            try
            {
                var instance = Client.Instance;
            }
            catch (Exception ex)
            {
                // Handle exception here
            }

            var reference = Client.Instance.From<Channel>();

            await reference.On(Client.ChannelEventType.All,
                (sender, ev) =>
                {
                    Debug.WriteLine($"[{ev.Response.Event}]:{ev.Response.Topic}:{ev.Response.Payload.Record}");
                });

            var channels = await reference.Get();

            //await reference.Insert(new Models.Channel { Slug = GenerateName(10), InsertedAt = DateTime.Now });

            #region Storage

            var storage = Client.Instance.Storage;

            var exists = await storage.GetBucket("testing") != null;
            if (!exists)
                await storage.CreateBucket("testing", new BucketUpsertOptions { Public = true });

            var buckets = await storage.ListBuckets();

            foreach (var b in buckets)
                Debug.WriteLine($"[{b.Id}] {b.Name}");

            var bucket = storage.From("testing");
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace("file:", "");
            var imagePath = Path.Combine(basePath, "Assets", "supabase-csharp.png");

            Debug.WriteLine(await bucket.Upload(imagePath, "supabase-csharp.png", new FileOptions { Upsert = true },
                (sender, args) => Debug.WriteLine($"Upload Progress: {args}%")));
            Debug.WriteLine(bucket.GetPublicUrl("supabase-csharp.png"));
            Debug.WriteLine(await bucket.CreateSignedUrl("supabase-csharp.png", 3600));

            var bucketItems = await bucket.List();

            foreach (var item in bucketItems)
                Debug.WriteLine($"[{item.Id}] {item.Name} - {item.CreatedAt}");

            Debug.WriteLine(await bucket.Download("supabase-csharp.png", Path.Combine(basePath, "testing-download.png"),
                (sender, args) => Debug.WriteLine($"Download Progress: {args}%")));

            await storage.EmptyBucket("testing");
            await storage.DeleteBucket("testing");

            #endregion
        }

        // From: https://stackoverflow.com/a/49922533/3629438
        private static string GenerateName(int len)
        {
            var r = new Random();
            string[] consonants =
            {
                "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "l", "n", "p", "q", "r", "s", "sh", "zh", "t", "v",
                "w", "x"
            };
            string[] vowels = { "a", "e", "i", "o", "u", "ae", "y" };
            var Name = "";
            Name += consonants[r.Next(consonants.Length)].ToUpper();
            Name += vowels[r.Next(vowels.Length)];
            var
                b = 2; //b tells how many times a new letter has been added. It's 2 right now because the first two letters are already in the name.
            while (b < len)
            {
                Name += consonants[r.Next(consonants.Length)];
                b++;
                Name += vowels[r.Next(vowels.Length)];
                b++;
            }

            return Name;
        }
    }
}
