using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SupabaseDb.Queries;
using SupabaseDb.Tests.Models;
using Xunit;
using static SupabaseDb.Constants;

namespace SupabaseDb.Tests;

public class DbClientTests
{
    private const string BASE_URL = "http://localhost:3000";

    [Fact(DisplayName = "Initilizes")]
    public void TestInitilization()
    {
        var client = new DbClient(BASE_URL);
        Assert.Equal(BASE_URL, client.BaseUrl);
    }

    [Fact(DisplayName = "with optional query params")]
    public void TestQueryParams()
    {
        var client = new DbClient(BASE_URL, new DbClientOptions
        {
            QueryParams = new Dictionary<string, string>
            {
                { "some-param", "foo" },
                { "other-param", "bar" }
            }
        });

        Assert.Equal($"{BASE_URL}/users?some-param=foo&other-param=bar",
            (client.Table<User>() as Table<User>).GenerateUrl());
    }

    [Fact(DisplayName = "will use TableAttribute")]
    public void TestTableAttribute()
    {
        var client = new DbClient(BASE_URL);
        Assert.Equal($"{BASE_URL}/users", (client.Table<User>() as Table<User>).GenerateUrl());
    }

    [Fact(DisplayName = "will default to Class.name in absence of TableAttribute")]
    public void TestTableAttributeDefault()
    {
        var client = new DbClient(BASE_URL);
        Assert.Equal($"{BASE_URL}/Stub", (client.Table<Stub>() as Table<Stub>).GenerateUrl());
    }

    [Fact(DisplayName = "will set header from options")]
    public void TestHeadersToken()
    {
        var headers = Helpers.PrepareRequestHeaders(HttpMethod.Get,
            new Dictionary<string, string> { { "Authorization", "Bearer token" } });

        Assert.Equal("Bearer token", headers["Authorization"]);
    }

    [Fact(DisplayName = "will set apikey as query string")]
    public void TestQueryApiKey()
    {
        var client = new DbClient(BASE_URL, new DbClientOptions
        {
            Headers =
            {
                { "apikey", "some-key" }
            }
        });
        Assert.Equal($"{BASE_URL}/users?apikey=some-key", (client.Table<User>() as Table<User>).GenerateUrl());
    }

    [Fact(DisplayName = "filters: simple")]
    public void TestFiltersSimple()
    {
        var client = new DbClient(BASE_URL);
        var dict = new Dictionary<Operator, string>
        {
            { Operator.Equals, "eq.bar" },
            { Operator.GreaterThan, "gt.bar" },
            { Operator.GreaterThanOrEqual, "gte.bar" },
            { Operator.LessThan, "lt.bar" },
            { Operator.LessThanOrEqual, "lte.bar" },
            { Operator.NotEqual, "neq.bar" },
            { Operator.Is, "is.bar" }
        };

        foreach (var pair in dict)
        {
            var filter = new QueryFilter("foo", pair.Key, "bar");
            var result = (client.Table<User>() as Table<User>).PrepareFilter(filter);
            Assert.Equal("foo", result.Key);
            Assert.Equal(pair.Value, result.Value);
        }
    }

    [Fact(DisplayName = "filters: like & ilike")]
    public void TestFiltersLike()
    {
        var client = new DbClient(BASE_URL);
        var dict = new Dictionary<Operator, string>
        {
            { Operator.Like, "like.*bar*" },
            { Operator.ILike, "ilike.*bar*" }
        };

        foreach (var pair in dict)
        {
            var filter = new QueryFilter("foo", pair.Key, "%bar%");
            var result = (client.Table<User>() as Table<User>).PrepareFilter(filter);
            Assert.Equal("foo", result.Key);
            Assert.Equal(pair.Value, result.Value);
        }
    }

    /// <summary>
    ///     See: http://postgrest.org/en/v7.0.0/api.html#operators
    /// </summary>
    [Fact(DisplayName = "filters: `In` with List<object> arguments")]
    public void TestFiltersArraysWithLists()
    {
        var client = new DbClient(BASE_URL);

        // UrlEncoded {"bar","buzz"}
        var exp = "(\"bar\",\"buzz\")";
        var dict = new Dictionary<Operator, string>
        {
            { Operator.In, $"in.{exp}" }
        };

        foreach (var pair in dict)
        {
            var list = new List<object> { "bar", "buzz" };
            var filter = new QueryFilter("foo", pair.Key, list);
            var result = (client.Table<User>() as Table<User>).PrepareFilter(filter);
            Assert.Equal("foo", result.Key);
            Assert.Equal(pair.Value, result.Value);
        }
    }

    /// <summary>
    ///     See: http://postgrest.org/en/v7.0.0/api.html#operators
    /// </summary>
    [Fact(DisplayName = "filters: `Contains`, `ContainedIn`, `Overlap` with List<object> arguments")]
    public void TestFiltersContainsArraysWithLists()
    {
        var client = new DbClient(BASE_URL);

        // UrlEncoded {bar,buzz} - according to documentation, does not accept quoted strings
        var exp = "{bar,buzz}";
        var dict = new Dictionary<Operator, string>
        {
            { Operator.Contains, $"cs.{exp}" },
            { Operator.ContainedIn, $"cd.{exp}" },
            { Operator.Overlap, $"ov.{exp}" }
        };

        foreach (var pair in dict)
        {
            var list = new List<object> { "bar", "buzz" };
            var filter = new QueryFilter("foo", pair.Key, list);
            var result = (client.Table<User>() as Table<User>).PrepareFilter(filter);
            Assert.Equal("foo", result.Key);
            Assert.Equal(pair.Value, result.Value);
        }
    }

    [Fact(DisplayName = "filters: arrays with Dictionary<string,object> arguments")]
    public void TestFiltersArraysWithDictionaries()
    {
        var client = new DbClient(BASE_URL);

        var exp = "{\"bar\":100,\"buzz\":\"zap\"}";
        var dict = new Dictionary<Operator, string>
        {
            { Operator.In, $"in.{exp}" },
            { Operator.Contains, $"cs.{exp}" },
            { Operator.ContainedIn, $"cd.{exp}" },
            { Operator.Overlap, $"ov.{exp}" }
        };

        foreach (var pair in dict)
        {
            var value = new Dictionary<string, object> { { "bar", 100 }, { "buzz", "zap" } };
            var filter = new QueryFilter("foo", pair.Key, value);
            var result = (client.Table<User>() as Table<User>).PrepareFilter(filter);
            Assert.Equal("foo", result.Key);
            Assert.Equal(pair.Value, result.Value);
        }
    }

    [Fact(DisplayName = "filters: full text search")]
    public void TestFiltersFullTextSearch()
    {
        var client = new DbClient(BASE_URL);

        // UrlEncoded [2,3]
        var exp = "(english).bar";
        var dict = new Dictionary<Operator, string>
        {
            { Operator.FTS, $"fts{exp}" },
            { Operator.PHFTS, $"phfts{exp}" },
            { Operator.PLFTS, $"plfts{exp}" },
            { Operator.WFTS, $"wfts{exp}" }
        };

        foreach (var pair in dict)
        {
            var config = new FullTextSearchConfig("bar", "english");
            var filter = new QueryFilter("foo", pair.Key, config);
            var result = (client.Table<User>() as Table<User>).PrepareFilter(filter);
            Assert.Equal("foo", result.Key);
            Assert.Equal(pair.Value, result.Value);
        }
    }

    [Fact(DisplayName = "filters: ranges")]
    public void TestFiltersRanges()
    {
        var client = new DbClient(BASE_URL);

        var exp = "[2,3]";
        var dict = new Dictionary<Operator, string>
        {
            { Operator.StrictlyLeft, $"sl.{exp}" },
            { Operator.StrictlyRight, $"sr.{exp}" },
            { Operator.NotRightOf, $"nxr.{exp}" },
            { Operator.NotLeftOf, $"nxl.{exp}" },
            { Operator.Adjacent, $"adj.{exp}" }
        };

        foreach (var pair in dict)
        {
            var config = new IntRange(2, 3);
            var filter = new QueryFilter("foo", pair.Key, config);
            var result = (client.Table<User>() as Table<User>).PrepareFilter(filter);
            Assert.Equal("foo", result.Key);
            Assert.Equal(pair.Value, result.Value);
        }
    }

    [Fact(DisplayName = "filters: not")]
    public void TestFiltersNot()
    {
        var client = new DbClient(BASE_URL);
        var filter = new QueryFilter("foo", Operator.Equals, "bar");
        var notFilter = new QueryFilter(Operator.Not, filter);
        var result = (client.Table<User>() as Table<User>).PrepareFilter(notFilter);

        Assert.Equal("foo", result.Key);
        Assert.Equal("not.eq.bar", result.Value);
    }

    [Fact(DisplayName = "filters: and & or")]
    public void TestFiltersAndOr()
    {
        var client = new DbClient(BASE_URL);
        var exp = "(a.gte.0,a.lte.100)";

        var dict = new Dictionary<Operator, string>
        {
            { Operator.And, $"and={exp}" },
            { Operator.Or, $"or={exp}" }
        };

        var subfilters = new List<QueryFilter>
        {
            new("a", Operator.GreaterThanOrEqual, "0"),
            new("a", Operator.LessThanOrEqual, "100")
        };

        foreach (var pair in dict)
        {
            var filter = new QueryFilter(pair.Key, subfilters);
            var result = (client.Table<User>() as Table<User>).PrepareFilter(filter);
            Assert.Equal(pair.Value, $"{result.Key}={result.Value}");
        }
    }

    [Fact(DisplayName = "update: basic")]
    public async Task TestBasicUpdate()
    {
        var client = new DbClient(BASE_URL);

        var user = await client.Table<User>().Filter("username", Operator.Equals, "supabot")
            .Single();

        if (user != null)
        {
            // Update user status
            user.Status = "OFFLINE";
            var response = await user.Update<User>(client);

            var updatedUser = response.Models.FirstOrDefault();

            Assert.Equal(1, response.Models.Count);
            Assert.Equal(user.Username, updatedUser.Username);
            Assert.Equal(user.Status, updatedUser.Status);
        }
    }

    [Fact(DisplayName = "Exceptions: Throws when attempting to update a non-existent record")]
    public async Task TestThrowsRequestExceptionOnNonExistantUpdate()
    {
        var client = new DbClient(BASE_URL);

        await Assert.ThrowsAsync<RequestException>(async () =>
        {
            var nonExistentRecord = new User
            {
                Username = "Foo",
                Status = "Bar"
            };
            await nonExistentRecord.Update<User>(client);
        });
    }

    [Fact(DisplayName = "insert: basic")]
    public async Task TestBasicInsert()
    {
        var client = new DbClient(BASE_URL);

        var newUser = new User
        {
            Username = Guid.NewGuid().ToString(),
            AgeRange = new IntRange(18, 22),
            Catchphrase = "what a shot",
            Status = "ONLINE"
        };

        var response = await client.Table<User>().Insert(newUser);
        var insertedUser = response.Models.First();

        Assert.Equal(1, response.Models.Count);
        Assert.Equal(newUser.Username, insertedUser.Username);
        Assert.Equal(newUser.AgeRange, insertedUser.AgeRange);
        Assert.Equal(newUser.Status, insertedUser.Status);

        await client.Table<User>().Delete(newUser);

        var response2 = await client.Table<User>()
            .Insert(newUser, new QueryOptions { Returning = QueryOptions.ReturnType.Minimal });
        Assert.Equal("", response2.Content);

        await client.Table<User>().Delete(newUser);
    }

    [Fact(DisplayName = "insert: headers generated")]
    public void TestInsertHeaderGeneration()
    {
        var option = new QueryOptions();
        Assert.Equal("return=representation", option.ToHeaders()["Prefer"]);

        option.Returning = QueryOptions.ReturnType.Minimal;
        Assert.Equal("return=minimal", option.ToHeaders()["Prefer"]);

        option.Upsert = true;
        Assert.Equal("resolution=merge-duplicates,return=minimal", option.ToHeaders()["Prefer"]);

        option.DuplicateResolution = QueryOptions.DuplicateResolutionType.IgnoreDuplicates;
        Assert.Equal("resolution=ignore-duplicates,return=minimal", option.ToHeaders()["Prefer"]);

        option.Upsert = false;
        option.Count = QueryOptions.CountType.Exact;
        Assert.Equal("return=minimal,count=exact", option.ToHeaders()["Prefer"]);
    }

    [Fact(DisplayName =
        "Exceptions: Throws when inserting a user with same primary key value as an existing one without upsert option")]
    public async Task TestThrowsRequestExceptionInsertPkConflict()
    {
        var client = new DbClient(BASE_URL);

        await Assert.ThrowsAsync<RequestException>(async () =>
        {
            var newUser = new User
            {
                Username = "supabot"
            };
            await client.Table<User>().Insert(newUser);
        });
    }

    [Fact(DisplayName = "insert: upsert")]
    public async Task TestInsertWithUpsert()
    {
        var client = new DbClient(BASE_URL);

        var supaUpdated = new User
        {
            Username = "supabot",
            AgeRange = new IntRange(3, 8),
            Status = "OFFLINE",
            Catchphrase = "fat cat"
        };

        var insertOptions = new QueryOptions
        {
            Upsert = true
        };

        var response = await client.Table<User>().Insert(supaUpdated, insertOptions);

        var kitchenSink1 = new KitchenSink
        {
            UniqueValue = "Testing"
        };

        var ks1 = await client.Table<KitchenSink>().OnConflict("unique_value").Upsert(kitchenSink1);
        var uks1 = ks1.Models.First();
        uks1.StringValue = "Testing 1";
        var ks3 = await client.Table<KitchenSink>().OnConflict("unique_value").Upsert(uks1);

        var updatedUser = response.Models.First();

        Assert.Equal(1, response.Models.Count);
        Assert.Equal(supaUpdated.Username, updatedUser.Username);
        Assert.Equal(supaUpdated.AgeRange, updatedUser.AgeRange);
        Assert.Equal(supaUpdated.Status, updatedUser.Status);
    }

    [Fact(DisplayName = "order: basic")]
    public async Task TestOrderBy()
    {
        var client = new DbClient(BASE_URL);

        var orderedResponse = await client.Table<User>().Order("username", Ordering.Descending).Get();
        var unorderedResponse = await client.Table<User>().Get();

        var supaOrderedUsers = orderedResponse.Models;
        var linqOrderedUsers = unorderedResponse.Models.OrderByDescending(u => u.Username).ToList();

        Assert.Equal(linqOrderedUsers, supaOrderedUsers);
    }

    [Fact(DisplayName = "limit: basic")]
    public async Task TestLimit()
    {
        var client = new DbClient(BASE_URL);

        var limitedUsersResponse = await client.Table<User>().Limit(2).Get();
        var usersResponse = await client.Table<User>().Get();

        var supaLimitUsers = limitedUsersResponse.Models;
        var linqLimitUsers = usersResponse.Models.Take(2).ToList();

        Assert.Equal(linqLimitUsers, supaLimitUsers);
    }

    [Fact(DisplayName = "offset: basic")]
    public async Task TestOffset()
    {
        var client = new DbClient(BASE_URL);

        var offsetUsersResponse = await client.Table<User>().Offset(2).Get();
        var usersResponse = await client.Table<User>().Get();

        var supaOffsetUsers = offsetUsersResponse.Models;
        var linqSkipUsers = usersResponse.Models.Skip(2).ToList();

        Assert.Equal(linqSkipUsers, supaOffsetUsers);
    }

    [Fact(DisplayName = "range: from")]
    public async Task TestRangeFrom()
    {
        var client = new DbClient(BASE_URL);

        var rangeUsersResponse = await client.Table<User>().Range(2).Get();
        var usersResponse = await client.Table<User>().Get();

        var supaRangeUsers = rangeUsersResponse.Models;
        var linqSkipUsers = usersResponse.Models.Skip(2).ToList();

        Assert.Equal(linqSkipUsers, supaRangeUsers);
    }

    [Fact(DisplayName = "range: from and to")]
    public async Task TestRangeFromAndTo()
    {
        var client = new DbClient(BASE_URL);

        var rangeUsersResponse = await client.Table<User>().Range(1, 3).Get();
        var usersResponse = await client.Table<User>().Get();

        var supaRangeUsers = rangeUsersResponse.Models;
        var linqRangeUsers = usersResponse.Models.Skip(1).Take(3).ToList();

        Assert.Equal(linqRangeUsers, supaRangeUsers);
    }

    [Fact(DisplayName = "range: limit and offset")]
    public async Task TestRangeWithLimitAndOffset()
    {
        var client = new DbClient(BASE_URL);

        var rangeUsersResponse = await client.Table<User>().Limit(1).Offset(3).Get();
        var usersResponse = await client.Table<User>().Get();

        var supaRangeUsers = rangeUsersResponse.Models;
        var linqRangeUsers = usersResponse.Models.Skip(3).Take(1).ToList();

        Assert.Equal(linqRangeUsers, supaRangeUsers);
    }

    [Fact(DisplayName = "filters: not")]
    public async Task TestNotFilter()
    {
        var client = new DbClient(BASE_URL);
        var filter = new QueryFilter("username", Operator.Equals, "supabot");

        var filteredResponse = await client.Table<User>().Not(filter).Get();
        var usersResponse = await client.Table<User>().Get();

        var supaFilteredUsers = filteredResponse.Models;
        var linqFilteredUsers = usersResponse.Models.Where(u => u.Username != "supabot").ToList();

        Assert.Equal(linqFilteredUsers, supaFilteredUsers);
    }

    [Fact(DisplayName = "filters: `not` shorthand")]
    public async Task TestNotShorthandFilter()
    {
        var client = new DbClient(BASE_URL);

        // Standard NOT Equal Op.
        var filteredResponse = await client.Table<User>()
            .Not("username", Operator.Equals, "supabot").Get();
        var usersResponse = await client.Table<User>().Get();

        var supaFilteredUsers = filteredResponse.Models;
        var linqFilteredUsers = usersResponse.Models.Where(u => u.Username != "supabot").ToList();

        Assert.Equal(linqFilteredUsers, supaFilteredUsers);

        // NOT `In` Shorthand Op.
        var notInFilterResponse = await client.Table<User>()
            .Not("username", Operator.In, new List<object> { "supabot", "kiwicopple" }).Get();
        var supaNotInList = notInFilterResponse.Models;
        var linqNotInList = usersResponse.Models.Where(u => u.Username != "supabot")
            .Where(u => u.Username != "kiwicopple").ToList();

        Assert.Equal(supaNotInList, linqNotInList);
    }

    [Fact(DisplayName = "filters: null operation `Equals`")]
    public async Task TestEqualsNullFilterEquals()
    {
        var client = new DbClient(BASE_URL);

        await client.Table<User>().Insert(new User { Username = "acupofjose", Status = "ONLINE", Catchphrase = null },
            new QueryOptions { Upsert = true });

        var filteredResponse = await client.Table<User>().Filter("catchphrase", Operator.Equals, null).Get();
        var usersResponse = await client.Table<User>().Get();

        var supaFilteredUsers = filteredResponse.Models;
        var linqFilteredUsers = usersResponse.Models.Where(u => u.Catchphrase == null).ToList();

        Assert.Equal(linqFilteredUsers, supaFilteredUsers);
    }

    [Fact(DisplayName = "filters: null operation `Is`")]
    public async Task TestEqualsNullFilterIs()
    {
        var client = new DbClient(BASE_URL);

        await client.Table<User>().Insert(new User { Username = "acupofjose", Status = "ONLINE", Catchphrase = null },
            new QueryOptions { Upsert = true });

        var filteredResponse = await client.Table<User>().Filter("catchphrase", Operator.Is, null).Get();
        var usersResponse = await client.Table<User>().Get();

        var supaFilteredUsers = filteredResponse.Models;
        var linqFilteredUsers = usersResponse.Models.Where(u => u.Catchphrase == null).ToList();

        Assert.Equal(linqFilteredUsers, supaFilteredUsers);
    }

    [Fact(DisplayName = "filters: null operation `NotEquals`")]
    public async Task TestEqualsNullFilterNotEquals()
    {
        var client = new DbClient(BASE_URL);

        await client.Table<User>().Insert(new User { Username = "acupofjose", Status = "ONLINE", Catchphrase = null },
            new QueryOptions { Upsert = true });

        var filteredResponse =
            await client.Table<User>().Filter("catchphrase", Operator.NotEqual, null).Get();
        var usersResponse = await client.Table<User>().Get();

        var supaFilteredUsers = filteredResponse.Models;
        var linqFilteredUsers = usersResponse.Models.Where(u => u.Catchphrase != null).ToList();

        Assert.Equal(linqFilteredUsers, supaFilteredUsers);
    }

    [Fact(DisplayName = "filters: null operation `Not`")]
    public async Task TestEqualsNullNot()
    {
        var client = new DbClient(BASE_URL);

        await client.Table<User>().Insert(new User { Username = "acupofjose", Status = "ONLINE", Catchphrase = null },
            new QueryOptions { Upsert = true });

        var filteredResponse = await client.Table<User>().Filter("catchphrase", Operator.Not, null).Get();
        var usersResponse = await client.Table<User>().Get();

        var supaFilteredUsers = filteredResponse.Models;
        var linqFilteredUsers = usersResponse.Models.Where(u => u.Catchphrase != null).ToList();

        Assert.Equal(linqFilteredUsers, supaFilteredUsers);
    }

    [Fact(DisplayName = "filters: in")]
    public async Task TestInFilter()
    {
        var client = new DbClient(BASE_URL);

        var criteria = new List<object> { "supabot", "kiwicopple" };

        var filteredResponse = await client.Table<User>().Filter("username", Operator.In, criteria)
            .Order("username", Ordering.Descending).Get();
        var usersResponse = await client.Table<User>().Get();

        var supaFilteredUsers = filteredResponse.Models;
        var linqFilteredUsers = usersResponse.Models.OrderByDescending(u => u.Username)
            .Where(u => u.Username == "supabot" || u.Username == "kiwicopple").ToList();

        Assert.Equal(linqFilteredUsers, supaFilteredUsers);
    }

    [Fact(DisplayName = "filters: eq")]
    public async Task TestEqualsFilter()
    {
        var client = new DbClient(BASE_URL);

        var filteredResponse =
            await client.Table<User>().Filter("username", Operator.Equals, "supabot").Get();
        var usersResponse = await client.Table<User>().Get();

        var supaFilteredUsers = filteredResponse.Models;
        var linqFilteredUsers = usersResponse.Models.Where(u => u.Username == "supabot").ToList();

        Assert.Equal(1, supaFilteredUsers.Count);
        Assert.Equal(linqFilteredUsers, supaFilteredUsers);
    }

    [Fact(DisplayName = "filters: gt")]
    public async Task TestGreaterThanFilter()
    {
        var client = new DbClient(BASE_URL);

        var filteredResponse = await client.Table<Message>().Filter("id", Operator.GreaterThan, "1").Get();
        var messagesResponse = await client.Table<Message>().Get();

        var supaFilteredMessages = filteredResponse.Models;
        var linqFilteredMessages = messagesResponse.Models.Where(m => m.Id > 1).ToList();

        Assert.Equal(1, supaFilteredMessages.Count);
        Assert.Equal(linqFilteredMessages, supaFilteredMessages);
    }

    [Fact(DisplayName = "filters: gte")]
    public async Task TestGreaterThanOrEqualFilter()
    {
        var client = new DbClient(BASE_URL);

        var filteredResponse =
            await client.Table<Message>().Filter("id", Operator.GreaterThanOrEqual, "1").Get();
        var messagesResponse = await client.Table<Message>().Get();

        var supaFilteredMessages = filteredResponse.Models;
        var linqFilteredMessages = messagesResponse.Models.Where(m => m.Id >= 1).ToList();

        Assert.Equal(linqFilteredMessages, supaFilteredMessages);
    }

    [Fact(DisplayName = "filters: lt")]
    public async Task TestlessThanFilter()
    {
        var client = new DbClient(BASE_URL);

        var filteredResponse = await client.Table<Message>().Filter("id", Operator.LessThan, "2").Get();
        var messagesResponse = await client.Table<Message>().Get();

        var supaFilteredMessages = filteredResponse.Models;
        var linqFilteredMessages = messagesResponse.Models.Where(m => m.Id < 2).ToList();

        Assert.Equal(linqFilteredMessages, supaFilteredMessages);
    }

    [Fact(DisplayName = "filters: lte")]
    public async Task TestLessThanOrEqualFilter()
    {
        var client = new DbClient(BASE_URL);

        var filteredResponse =
            await client.Table<Message>().Filter("id", Operator.LessThanOrEqual, "2").Get();
        var messagesResponse = await client.Table<Message>().Get();

        var supaFilteredMessages = filteredResponse.Models;
        var linqFilteredMessages = messagesResponse.Models.Where(m => m.Id <= 2).ToList();

        Assert.Equal(linqFilteredMessages, supaFilteredMessages);
    }

    [Fact(DisplayName = "filters: nqe")]
    public async Task TestNotEqualFilter()
    {
        var client = new DbClient(BASE_URL);

        var filteredResponse = await client.Table<Message>().Filter("id", Operator.NotEqual, "2").Get();
        var messagesResponse = await client.Table<Message>().Get();

        var supaFilteredMessages = filteredResponse.Models;
        var linqFilteredMessages = messagesResponse.Models.Where(m => m.Id != 2).ToList();

        Assert.Equal(linqFilteredMessages, supaFilteredMessages);
    }

    [Fact(DisplayName = "filters: like")]
    public async Task TestLikeFilter()
    {
        var client = new DbClient(BASE_URL);

        var filteredResponse = await client.Table<Message>().Filter("username", Operator.Like, "s%").Get();
        var messagesResponse = await client.Table<Message>().Get();

        var supaFilteredMessages = filteredResponse.Models;
        var linqFilteredMessages = messagesResponse.Models.Where(m => m.UserName.StartsWith('s')).ToList();

        Assert.Equal(linqFilteredMessages, supaFilteredMessages);
    }

    [Fact(DisplayName = "filters: cs")]
    public async Task TestContainsFilter()
    {
        var client = new DbClient(BASE_URL);

        await client.Table<User>()
            .Insert(new User { Username = "skikra", Status = "ONLINE", AgeRange = new IntRange(1, 3) },
                new QueryOptions { Upsert = true });
        var filteredResponse = await client.Table<User>()
            .Filter("age_range", Operator.Contains, new IntRange(1, 2)).Get();
        var usersResponse = await client.Table<User>().Get();

        var testAgainst = usersResponse.Models.Where(m => m.AgeRange?.Start.Value <= 1 && m.AgeRange?.End.Value >= 2)
            .ToList();

        Assert.Equal(testAgainst, filteredResponse.Models);
    }

    [Fact(DisplayName = "filters: cd")]
    public async Task TestContainedFilter()
    {
        var client = new DbClient(BASE_URL);

        var filteredResponse = await client.Table<User>()
            .Filter("age_range", Operator.ContainedIn, new IntRange(25, 35)).Get();
        var usersResponse = await client.Table<User>().Get();

        var testAgainst = usersResponse.Models.Where(m => m.AgeRange?.Start.Value >= 25 && m.AgeRange?.End.Value <= 35)
            .ToList();

        Assert.Equal(testAgainst, filteredResponse.Models);
    }

    [Fact(DisplayName = "filters: sr")]
    public async Task TestStrictlyLeftFilter()
    {
        var client = new DbClient(BASE_URL);

        await client.Table<User>()
            .Insert(new User { Username = "minds3t", Status = "ONLINE", AgeRange = new IntRange(3, 6) },
                new QueryOptions { Upsert = true });
        var filteredResponse = await client.Table<User>()
            .Filter("age_range", Operator.StrictlyLeft, new IntRange(7, 8)).Get();
        var usersResponse = await client.Table<User>().Get();

        var testAgainst = usersResponse.Models.Where(m => m.AgeRange?.Start.Value < 7 && m.AgeRange?.End.Value < 7)
            .ToList();

        Assert.Equal(testAgainst, filteredResponse.Models);
    }

    [Fact(DisplayName = "filters: sl")]
    public async Task TestStrictlyRightFilter()
    {
        var client = new DbClient(BASE_URL);

        var filteredResponse = await client.Table<User>()
            .Filter("age_range", Operator.StrictlyRight, new IntRange(1, 2)).Get();
        var usersResponse = await client.Table<User>().Get();

        var testAgainst = usersResponse.Models.Where(m => m.AgeRange?.Start.Value > 2 && m.AgeRange?.End.Value > 2)
            .ToList();

        Assert.Equal(testAgainst, filteredResponse.Models);
    }

    [Fact(DisplayName = "filters: nxl")]
    public async Task TestNotExtendToLeftFilter()
    {
        var client = new DbClient(BASE_URL);

        var filteredResponse = await client.Table<User>()
            .Filter("age_range", Operator.NotLeftOf, new IntRange(2, 4)).Get();
        var usersResponse = await client.Table<User>().Get();

        var testAgainst = usersResponse.Models.Where(m => m.AgeRange?.Start.Value >= 2 && m.AgeRange?.End.Value >= 2)
            .ToList();

        Assert.Equal(testAgainst, filteredResponse.Models);
    }

    [Fact(DisplayName = "filters: nxr")]
    public async Task TestNotExtendToRightFilter()
    {
        var client = new DbClient(BASE_URL);

        var filteredResponse = await client.Table<User>()
            .Filter("age_range", Operator.NotRightOf, new IntRange(2, 4)).Get();
        var usersResponse = await client.Table<User>().Get();

        var testAgainst = usersResponse.Models.Where(m => m.AgeRange?.Start.Value <= 4 && m.AgeRange?.End.Value <= 4)
            .ToList();

        Assert.Equal(testAgainst, filteredResponse.Models);
    }

    [Fact(DisplayName = "filters: adj")]
    public async Task TestAdjacentFilter()
    {
        var client = new DbClient(BASE_URL);

        var filteredResponse = await client.Table<User>()
            .Filter("age_range", Operator.Adjacent, new IntRange(1, 2)).Get();
        var usersResponse = await client.Table<User>().Get();

        var testAgainst = usersResponse.Models.Where(m => m.AgeRange?.End.Value == 0 || m.AgeRange?.Start.Value == 3)
            .ToList();

        Assert.Equal(testAgainst, filteredResponse.Models);
    }

    [Fact(DisplayName = "filters: ov")]
    public async Task TestOverlapFilter()
    {
        var client = new DbClient(BASE_URL);

        var filteredResponse = await client.Table<User>()
            .Filter("age_range", Operator.Overlap, new IntRange(2, 4)).Get();
        var usersResponse = await client.Table<User>().Get();

        var testAgainst = usersResponse.Models.Where(m => m.AgeRange?.Start.Value <= 4 && m.AgeRange?.End.Value >= 2)
            .ToList();

        Assert.Equal(testAgainst, filteredResponse.Models);
    }

    [Fact(DisplayName = "filters: ilike")]
    public async Task TestILikeFilter()
    {
        var client = new DbClient(BASE_URL);

        var filteredResponse =
            await client.Table<Message>().Filter("username", Operator.ILike, "%SUPA%").Get();
        var messagesResponse = await client.Table<Message>().Get();

        var supaFilteredMessages = filteredResponse.Models;
        var linqFilteredMessages = messagesResponse.Models
            .Where(m => m.UserName.Contains("SUPA", StringComparison.OrdinalIgnoreCase)).ToList();

        Assert.Equal(linqFilteredMessages, supaFilteredMessages);
    }

    [Fact(DisplayName = "filters: fts")]
    public async Task TestFullTextSearch()
    {
        var client = new DbClient(BASE_URL);
        var config = new FullTextSearchConfig("'fat' & 'cat'", "english");

        var filteredResponse = await client.Table<User>().Filter("catchphrase", Operator.FTS, config).Get();

        Assert.Equal(1, filteredResponse.Models.Count);
        Assert.Equal("supabot", filteredResponse.Models.FirstOrDefault()?.Username);
    }

    [Fact(DisplayName = "filters: plfts")]
    public async Task TestPlaintoFullTextSearch()
    {
        var client = new DbClient(BASE_URL);
        var config = new FullTextSearchConfig("'fat' & 'cat'", "english");

        var filteredResponse = await client.Table<User>().Filter("catchphrase", Operator.PLFTS, config).Get();

        Assert.Equal(1, filteredResponse.Models.Count);
        Assert.Equal("supabot", filteredResponse.Models.FirstOrDefault()?.Username);
    }

    [Fact(DisplayName = "filters: phfts")]
    public async Task TestPhrasetoFullTextSearch()
    {
        var client = new DbClient(BASE_URL);
        var config = new FullTextSearchConfig("'cat'", "english");

        var filteredResponse = await client.Table<User>().Filter("catchphrase", Operator.PHFTS, config).Get();
        var usersResponse = await client.Table<User>().Filter("catchphrase", Operator.NotEqual, null).Get();

        var testAgainst = usersResponse.Models.Where(u => u.Catchphrase.Contains("'cat'")).ToList();
        Assert.Equal(testAgainst, filteredResponse.Models);
    }

    [Fact(DisplayName = "filters: wfts")]
    public async Task TestWebFullTextSearch()
    {
        var client = new DbClient(BASE_URL);
        var config = new FullTextSearchConfig("'fat' & 'cat'", "english");

        var filteredResponse = await client.Table<User>().Filter("catchphrase", Operator.WFTS, config).Get();

        Assert.Equal(1, filteredResponse.Models.Count);
        Assert.Equal("supabot", filteredResponse.Models.FirstOrDefault()?.Username);
    }

    [Fact(DisplayName = "filters: match")]
    public async Task TestMatchFilter()
    {
        //Arrange
        var client = new DbClient(BASE_URL);
        var usersResponse = await client.Table<User>().Get();
        var testAgaint = usersResponse.Models.Where(u => u.Username == "kiwicopple" && u.Status == "OFFLINE").ToList();

        //Act
        var filters = new Dictionary<string, string>
        {
            { "username", "kiwicopple" },
            { "status", "OFFLINE" }
        };
        var filteredResponse = await client.Table<User>().Match(filters).Get();

        //Assert
        Assert.Equal(testAgaint, filteredResponse.Models);
    }

    [Fact(DisplayName = "select: basic")]
    public async Task TestSelect()
    {
        var client = new DbClient(BASE_URL);

        var response = await client.Table<User>().Select("username").Get();
        foreach (var user in response.Models)
        {
            Assert.NotNull(user.Username);
            Assert.Null(user.Catchphrase);
            Assert.Null(user.Status);
        }
    }

    [Fact(DisplayName = "select: multiple columns")]
    public async Task TestSelectWithMultipleColumns()
    {
        var client = new DbClient(BASE_URL);

        var response = await client.Table<User>().Select("username, status").Get();
        foreach (var user in response.Models)
        {
            Assert.NotNull(user.Username);
            Assert.NotNull(user.Status);
            Assert.Null(user.Catchphrase);
        }
    }

    [Fact(DisplayName = "insert: bulk")]
    public async Task TestInsertBulk()
    {
        var client = new DbClient(BASE_URL);
        var rocketUser = new User
        {
            Username = "rocket",
            AgeRange = new IntRange(35, 40),
            Status = "ONLINE"
        };

        var aceUser = new User
        {
            Username = "ace",
            AgeRange = new IntRange(21, 28),
            Status = "OFFLINE"
        };

        var users = new List<User>
        {
            rocketUser,
            aceUser
        };

        var response = await client.Table<User>().Insert(users);
        var insertedUsers = response.Models;


        Assert.Equal(users, insertedUsers);

        await client.Table<User>().Delete(rocketUser);
        await client.Table<User>().Delete(aceUser);
    }

    [Fact(DisplayName = "count")]
    public async Task TestCount()
    {
        var client = new DbClient(BASE_URL);

        var resp = await client.Table<User>().Count(CountType.Exact);
        // Lame, I know. We should check an actual number. However, the tests are run asynchronously
        // so we get inconsitent counts depending on the order that the tests are actually executed.
        Assert.NotNull(resp);
    }

    [Fact(DisplayName = "count: with filter")]
    public async Task TestCountWithFilter()
    {
        var client = new DbClient(BASE_URL);

        var resp = await client.Table<User>().Filter("status", Operator.Equals, "ONLINE")
            .Count(CountType.Exact);
        Assert.NotNull(resp);
    }

    [Fact(DisplayName = "support: int arrays")]
    public async Task TestSupportIntArraysAsLists()
    {
        var client = new DbClient(BASE_URL);

        var numbers = new List<int> { 1, 2, 3 };
        var result = await client.Table<User>()
            .Insert(
                new User
                {
                    Username = "WALRUS", Status = "ONLINE", Catchphrase = "I'm a walrus", FavoriteNumbers = numbers,
                    AgeRange = new IntRange(15, 25)
                }, new QueryOptions { Upsert = true });

        Assert.Equal(numbers, result.Models.First().FavoriteNumbers);
    }

    [Fact(DisplayName = "stored procedure")]
    public async Task TestStoredProcedure()
    {
        //Arrange 
        var client = new DbClient(BASE_URL);

        //Act 
        var parameters = new Dictionary<string, object>
        {
            { "name_param", "supabot" }
        };
        var response = await client.Rpc("get_status", parameters);

        //Assert 
        Assert.Equal(true, response.ResponseMessage.IsSuccessStatusCode);
        Assert.Equal(true, response.Content.Contains("OFFLINE"));
    }

    [Fact(DisplayName = "switch schema")]
    public async Task TestSwitchSchema()
    {
        //Arrange
        var options = new DbClientOptions
        {
            Schema = "personal"
        };
        var client = new DbClient(BASE_URL, options);

        //Act 
        var response = await client.Table<User>().Filter("username", Operator.Equals, "leroyjenkins").Get();

        //Assert 
        Assert.Equal(1, response.Models.Count);
        Assert.Equal("leroyjenkins", response.Models.FirstOrDefault()?.Username);
    }

    [Fact(DisplayName = "JSON.NET NullValueHandling is processed on Columns")]
    public async Task TestNullValueHandlingOnColumn()
    {
        var client = new DbClient(BASE_URL);
        var now = DateTime.UtcNow;
        var model = new KitchenSink
        {
            DateTimeValue = now,
            DateTimeValue1 = now
        };

        var insertResponse = await client.Table<KitchenSink>().Insert(model);

        Assert.Equal(now.ToString(), insertResponse.Models[0].DateTimeValue.ToString());
        Assert.Equal(now.ToString(), insertResponse.Models[0].DateTimeValue1.ToString());

        insertResponse.Models[0].DateTimeValue = null;
        insertResponse.Models[0].DateTimeValue1 = null;

        var updatedResponse = await client.Table<KitchenSink>().Update(insertResponse.Models[0]);

        Assert.Null(updatedResponse.Models[0].DateTimeValue);
        Assert.NotNull(updatedResponse.Models[0].DateTimeValue1);
    }
}
