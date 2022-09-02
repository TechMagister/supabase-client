using SupabaseAuth.Attributes;
using SupabaseAuth.Options;

namespace SupabaseAuth;

public interface IAuthClient
{
    /// <summary>
    ///     Signs up a user by email address
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <param name="options">Object containing redirectTo and optional user metadata (data)</param>
    /// <returns></returns>
    Task<Session?> SignUpAsync(string email, string password, SignUpOptions? options = null);

    /// <summary>
    ///     Signs up a user
    /// </summary>
    /// <param name="type"></param>
    /// <param name="identifier"></param>
    /// <param name="password"></param>
    /// <param name="options">Object containing redirectTo and optional user metadata (data)</param>
    /// <returns></returns>
    Task<Session?> SignUpAsync(SignUpType type, string identifier, string password, SignUpOptions? options = null);

    /// <summary>
    ///     Sends a Magic email login link to the specified email.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    Task<bool> SignInAsync(string email, SignInOptions? options = null);

    /// <summary>
    ///     Signs in a User.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    Task<Session?> SignInAsync(string email, string password);

    /// <summary>
    ///     Log in an existing user, or login via a third-party provider.
    /// </summary>
    /// <param name="type">Type of Credentials being passed</param>
    /// <param name="identifierOrToken">An email, phone, or RefreshToken</param>
    /// <param name="password">Password to account (optional if `RefreshToken`)</param>
    /// <param name="scopes">A space-separated list of scopes granted to the OAuth application.</param>
    /// <returns></returns>
    Task<Session?> SignInAsync(SignInType type, string identifierOrToken, string? password = null,
        string? scopes = null);

    /// <summary>
    ///     Retrieves a Url to redirect to for signing in with a <see cref="SupabaseAuth.AuthClient.Provider" />.
    ///     This method will need to be combined with <see cref="GetSessionFromUrlAsync" /> when the
    ///     Application receives the Oauth Callback.
    /// </summary>
    /// <example>
    ///     var client = Supabase.Gotrue.Client.Initialize(options);
    ///     var url = client.SignIn(Provider.Github);
    ///     // Do Redirect User
    ///     // Example code
    ///     Application.HasRecievedOauth += async (uri) => {
    ///     var session = await client.GetSessionFromUri(uri, true);
    ///     }
    /// </example>
    /// <param name="provider"></param>
    /// <param name="scopes">A space-separated list of scopes granted to the OAuth application.</param>
    /// <returns></returns>
    string SignIn(Provider provider, string? scopes = null);

    /// <summary>
    ///     Sends a Magic email login link to the specified email.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    Task<bool> SendMagicLinkAsync(string email, SignInOptions? options = null);

    /// <summary>
    ///     Log in a user given a User supplied OTP received via mobile.
    /// </summary>
    /// <param name="phone">The user's phone number.</param>
    /// <param name="token">Token sent to the user's phone.</param>
    /// <returns></returns>
    Task<Session?> VerifyOTPAsync(string phone, string token);

    /// <summary>
    ///     Signs out a user and invalidates the current token.
    /// </summary>
    /// <param name="accessToken"></param>
    /// >
    /// <returns></returns>
    Task SignOutAsync(string accessToken);

    /// <summary>
    ///     Updates a User.
    /// </summary>
    /// <param name="accessToken"></param>
    /// <param name="attributes"></param>
    /// <returns></returns>
    Task<User?> UpdateAsync(string accessToken, UserAttributes attributes);

    /// <summary>
    ///     Sends an invite email link to the specified email.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="jwt">this token needs role 'supabase_admin' or 'service_role'</param>
    /// <returns></returns>
    Task<bool> InviteUserByEmailAsync(string email, string jwt);

    /// <summary>
    ///     Deletes a User.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="jwt">this token needs role 'supabase_admin' or 'service_role'</param>
    /// <returns></returns>
    Task<bool> DeleteUserAsync(string uid, string jwt);

    /// <summary>
    ///     Lists users
    /// </summary>
    /// <param name="jwt">A valid JWT. Must be a full-access API key (e.g. service_role key).</param>
    /// <param name="filter">A string for example part of the email</param>
    /// <param name="sortBy">Snake case string of the given key, currently only created_at is suppported</param>
    /// <param name="sortOrder">asc or desc, if null desc is used</param>
    /// <param name="page">page to show for pagination</param>
    /// <param name="perPage">items per page for pagination</param>
    /// <returns></returns>
    Task<UserList?> ListUsersAsync(string jwt, string? filter = null, string? sortBy = null,
        Constants.SortOrder sortOrder = Constants.SortOrder.Descending, int? page = null, int? perPage = null);

    /// <summary>
    ///     Get User details by Id
    /// </summary>
    /// <param name="jwt">A valid JWT. Must be a full-access API key (e.g. service_role key).</param>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<User?> GetUserByIdAsync(string jwt, string userId);

    /// <summary>
    ///     Create a user (as a service_role)
    /// </summary>
    /// <param name="jwt">A valid JWT. Must be a full-access API key (e.g. service_role key).</param>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <param name="attributes"></param>
    /// <returns></returns>
    Task<User?> CreateUserAsync(string jwt, string email, string password, AdminUserAttributes? attributes = null);

    /// <summary>
    ///     Create a user (as a service_role)
    /// </summary>
    /// <param name="jwt">A valid JWT. Must be a full-access API key (e.g. service_role key).</param>
    /// <param name="attributes"></param>
    /// <returns></returns>
    Task<User?> CreateUserAsync(string jwt, AdminUserAttributes attributes);

    /// <summary>
    ///     UpdateAsync user by Id
    /// </summary>
    /// <param name="jwt">A valid JWT. Must be a full-access API key (e.g. service_role key).</param>
    /// <param name="userId"></param>
    /// <param name="userData"></param>
    /// <returns></returns>
    Task<User?> UpdateUserByIdAsync(string jwt, string userId, AdminUserAttributes userData);

    /// <summary>
    ///     Sends a reset request to an email address.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    Task<bool> ResetPasswordForEmailAsync(string email);

    /// <summary>
    ///     Refreshes the currently logged in User's Session.
    /// </summary>
    /// <param name="refreshToken"></param>
    /// >
    /// <returns></returns>
    Task<Session> RefreshSessionAsync(string refreshToken);

    /// <summary>
    ///     Parses a <see cref="Session" /> out of a <see cref="Uri" />'s Query parameters.
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="storeSession"></param>
    /// <returns></returns>
    Task<Session?> GetSessionFromUrlAsync(Uri uri, bool storeSession = true);
}
