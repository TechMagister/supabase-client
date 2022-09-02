using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using SupabaseAuth.Attributes;
using SupabaseAuth.Exceptions;
using SupabaseAuth.Options;
using Xunit;

namespace SupabaseAuth.Tests;

public class ClientTests
{
    private static readonly Random Random = new();
    private readonly IAuthClient _client;

    private readonly string _password = "I@M@SuperP@ssWord";

    public ClientTests()
    {
        _client = new AuthClient(new ClientOptions
        {
            Url = "http://localhost:9999",
            AllowUnconfirmedUserSessions = true
        });
    }

    private static string RandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Next(s.Length)]).ToArray());
    }

    private static string GetRandomPhoneNumber()
    {
        const string chars = "123456789";
        var inner = new string(Enumerable.Repeat(chars, 10)
            .Select(s => s[Random.Next(s.Length)]).ToArray());

        return $"+1{inner}";
    }

    private string GenerateServiceRoleToken()
    {
        var signingKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("37c304f8-51aa-419a-a1af-06154e63707a")); // using GOTRUE_JWT_SECRET

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            IssuedAt = DateTime.Now,
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials =
                new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature),
            Claims = new Dictionary<string, object>
            {
                {
                    "role", "service_role"
                }
            }
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(securityToken);
    }

    [Fact(DisplayName = "Client: Signs Up User")]
    public async Task ClientSignsUpUser()
    {
        Session? session = null;
        var email = $"{RandomString(12)}@supabase.io";
        session = await _client.SignUpAsync(email, _password);

        Assert.NotNull(session?.AccessToken);
        Assert.NotNull(session?.RefreshToken);
        Assert.IsType<User>(session?.User);


        var phone1 = GetRandomPhoneNumber();
        session = await _client.SignUpAsync(SignUpType.Phone, phone1, _password,
            new SignUpOptions { Data = new Dictionary<string, object> { { "firstName", "Testing" } } });

        Assert.NotNull(session.AccessToken);
        Assert.Equal("Testing", session.User.UserMetadata["firstName"]);
    }

    [Fact(DisplayName = "Client: Signs Up the same user twice should throw BadRequestException")]
    public async Task ClientSignsUpUserTwiceShouldReturnBadRequest()
    {
        var email = $"{RandomString(12)}@supabase.io";
        var result1 = await _client.SignUpAsync(email, _password);

        await Assert.ThrowsAsync<BadRequestException>(async () => { await _client.SignUpAsync(email, _password); });
    }

    [Fact(DisplayName = "Client: Signs In User (Email, Phone, Refresh token)")]
    public async Task ClientSignsIn()
    {
        var refreshToken = "";

        // Emails
        var email = $"{RandomString(12)}@supabase.io";
        var session = await _client.SignUpAsync(email, _password);

        await _client.SignOutAsync(session.AccessToken);

        session = await _client.SignInAsync(email, _password);

        Assert.NotNull(session.AccessToken);
        Assert.NotNull(session.RefreshToken);
        Assert.IsType<User>(session.User);

        // Phones
        var phone = GetRandomPhoneNumber();
        session = await _client.SignUpAsync(SignUpType.Phone, phone, _password);

        await _client.SignOutAsync(session.AccessToken);

        session = await _client.SignInAsync(SignInType.Phone, phone, _password);

        Assert.NotNull(session.AccessToken);
        Assert.NotNull(session.RefreshToken);
        Assert.IsType<User>(session.User);

        // Refresh Token
        refreshToken = session.RefreshToken;

        var newSession = await _client.SignInAsync(SignInType.RefreshToken, refreshToken);

        Assert.NotNull(newSession.AccessToken);
        Assert.NotNull(newSession.RefreshToken);
        Assert.IsType<User>(newSession.User);
    }

    [Fact(DisplayName = "Client: Sends Magic Login Email")]
    public async Task ClientSendsMagicLoginEmail()
    {
        var user = $"{RandomString(12)}@supabase.io";
        var session = await _client.SignUpAsync(user, _password);

        await _client.SignOutAsync(session.AccessToken);

        var result = await _client.SignInAsync(user);
        Assert.True(result);
    }

    [Fact(DisplayName = "Client: Sends Magic Login Email (Alias)")]
    public async Task ClientSendsMagicLoginEmailAlias()
    {
        var user = $"{RandomString(12)}@supabase.io";
        var user2 = $"{RandomString(12)}@supabase.io";
        var session = await _client.SignUpAsync(user, _password);

        await _client.SignOutAsync(session.AccessToken);

        var result = await _client.SendMagicLinkAsync(user);
        var result2 = await _client.SendMagicLinkAsync(user2,
            new SignInOptions { RedirectTo = $"com.{RandomString(12)}.deeplink://login" });

        Assert.True(result);
        Assert.True(result2);
    }

    [Fact(DisplayName = "Client: Returns Auth Url for Provider")]
    public void ClientReturnsAuthUrlForProvider()
    {
        var result1 = _client.SignIn(Provider.Google);
        Assert.Equal("http://localhost:9999/authorize?provider=google", result1);

        var result2 = _client.SignIn(Provider.Google, "special scopes please");
        Assert.Equal("http://localhost:9999/authorize?provider=google&scopes=special+scopes+please", result2);
    }

    [Fact(DisplayName = "Client: Update user")]
    public async Task ClientUpdateUser()
    {
        var email = $"{RandomString(12)}@supabase.io";
        var session = await _client.SignUpAsync(email, _password);

        var attributes = new UserAttributes
        {
            Data = new Dictionary<string, object>
            {
                { "hello", "world" }
            }
        };
        var result = await _client.UpdateAsync(session.AccessToken, attributes);
        Assert.Equal(email, session.User.Email);
        Assert.NotNull(session.User.UserMetadata);

        await _client.SignOutAsync(session.AccessToken);
        var token = GenerateServiceRoleToken();
        var result2 = await _client.UpdateUserByIdAsync(token, session.User.Id, new AdminUserAttributes
        {
            UserMetadata = new Dictionary<string, object>
            {
                { "hello", "updated" }
            }
        });

        Assert.NotEqual(result.UserMetadata["hello"], result2.UserMetadata["hello"]);
    }

    [Fact(DisplayName = "Client: Returns current user")]
    public async Task ClientGetUser()
    {
        var email = $"{RandomString(12)}@supabase.io";
        var session = await _client.SignUpAsync(email, _password);

        Assert.Equal(email, session.User.Email);
    }

    [Fact(DisplayName = "Client: Throws Exception on Invalid Username and Password")]
    public async Task ClientSignsInUserWrongPassword()
    {
        var user = $"{RandomString(12)}@supabase.io";
        var session = await _client.SignUpAsync(user, _password);

        await _client.SignOutAsync(session.AccessToken);

        await Assert.ThrowsAsync<BadRequestException>(async () =>
        {
            var result = await _client.SignInAsync(user, _password + "$");
        });
    }

    [Fact(DisplayName = "Client: Sends Invite Email")]
    public async Task ClientSendsInviteEmail()
    {
        var user = $"{RandomString(12)}@supabase.io";
        var serviceRoleKey = GenerateServiceRoleToken();
        var result = await _client.InviteUserByEmailAsync(user, serviceRoleKey);
        Assert.True(result);
    }

    [Fact(DisplayName = "Client: Lists users")]
    public async Task ClientListUsers()
    {
        var serviceRoleKey = GenerateServiceRoleToken();
        var result = await _client.ListUsersAsync(serviceRoleKey);

        Assert.True(result.Users.Count > 0);
    }

    [Fact(DisplayName = "Client: Lists users pagination")]
    public async Task ClientListUsersPagination()
    {
        var serviceRoleKey = GenerateServiceRoleToken();

        var page1 = await _client.ListUsersAsync(serviceRoleKey, page: 1, perPage: 1);
        var page2 = await _client.ListUsersAsync(serviceRoleKey, page: 2, perPage: 1);

        Assert.Equal(page1.Users.Count, 1);
        Assert.Equal(page2.Users.Count, 1);
        Assert.NotEqual(page1.Users[0].Id, page2.Users[0].Id);
    }

    [Fact(DisplayName = "Client: Lists users sort")]
    public async Task ClientListUsersSort()
    {
        var serviceRoleKey = GenerateServiceRoleToken();

        var result1 = await _client.ListUsersAsync(serviceRoleKey, sortBy: "created_at",
            sortOrder: Constants.SortOrder.Ascending);
        var result2 = await _client.ListUsersAsync(serviceRoleKey, sortBy: "created_at",
            sortOrder: Constants.SortOrder.Descending);

        Assert.NotEqual(result1.Users[0].Id, result2.Users[0].Id);
    }

    [Fact(DisplayName = "Client: Lists users filter")]
    public async Task ClientListUsersFilter()
    {
        var serviceRoleKey = GenerateServiceRoleToken();

        var user = $"{RandomString(12)}@supabase.io";
        var result = await _client.SignUpAsync(user, _password);

        var result1 = await _client.ListUsersAsync(serviceRoleKey, "@nonexistingrandomemailprovider.com");
        var result2 = await _client.ListUsersAsync(serviceRoleKey, "@supabase.io");

        Assert.NotEqual(result2.Users.Count, 0);
        Assert.Equal(result1.Users.Count, 0);
        Assert.NotEqual(result1.Users.Count, result2.Users.Count);
    }

    [Fact(DisplayName = "Client: Get User by Id")]
    public async Task ClientGetUserById()
    {
        var serviceRoleKey = GenerateServiceRoleToken();
        var result = await _client.ListUsersAsync(serviceRoleKey, page: 1, perPage: 1);

        var userResult = result.Users[0];
        var userByIdResult = await _client.GetUserByIdAsync(serviceRoleKey, userResult.Id);

        Assert.Equal(userResult.Id, userByIdResult.Id);
        Assert.Equal(userResult.Email, userByIdResult.Email);
    }

    [Fact(DisplayName = "Client: Create a user")]
    public async Task ClientCreateUser()
    {
        var serviceRoleKey = GenerateServiceRoleToken();
        var result = await _client.CreateUserAsync(serviceRoleKey, $"{RandomString(12)}@supabase.io", _password);

        Assert.NotNull(result);


        var attributes = new AdminUserAttributes
        {
            UserMetadata = new Dictionary<string, object> { { "firstName", "123" } },
            AppMetadata = new Dictionary<string, object> { { "roles", new List<string> { "editor", "publisher" } } }
        };

        var result2 =
            await _client.CreateUserAsync(serviceRoleKey, $"{RandomString(12)}@supabase.io", _password, attributes);
        Assert.Equal("123", result2.UserMetadata["firstName"]);

        var result3 = await _client.CreateUserAsync(serviceRoleKey,
            new AdminUserAttributes { Email = $"{RandomString(12)}@supabase.io", Password = _password });
        Assert.NotNull(result3);
    }


    [Fact(DisplayName = "Client: Update User by Id")]
    public async Task ClientUpdateUserById()
    {
        var serviceRoleKey = GenerateServiceRoleToken();
        var createdUser = await _client.CreateUserAsync(serviceRoleKey, $"{RandomString(12)}@supabase.io", _password);

        Assert.NotNull(createdUser);

        var updatedUser = await _client.UpdateUserByIdAsync(serviceRoleKey, createdUser.Id,
            new AdminUserAttributes { Email = $"{RandomString(12)}@supabase.io" });

        Assert.NotNull(updatedUser);

        Assert.Equal(createdUser.Id, updatedUser.Id);
        Assert.NotEqual(createdUser.Email, updatedUser.Email);
    }

    [Fact(DisplayName = "Client: Deletes User")]
    public async Task ClientDeletesUser()
    {
        var user = $"{RandomString(12)}@supabase.io";
        var session = await _client.SignUpAsync(user, _password);
        var uid = session.User.Id;

        var serviceRoleKey = GenerateServiceRoleToken();
        var result = await _client.DeleteUserAsync(uid, serviceRoleKey);

        Assert.True(result);
    }

    [Fact(DisplayName = "Client: Sends Reset Password Email")]
    public async Task ClientSendsResetPasswordForEmail()
    {
        var email = $"{RandomString(12)}@supabase.io";
        await _client.SignUpAsync(email, _password);
        var result = await _client.ResetPasswordForEmailAsync(email);
        Assert.True(result);
    }
}
