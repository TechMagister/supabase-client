using System.Web;
using SupabaseAuth.Attributes;
using SupabaseAuth.Options;

namespace SupabaseAuth;

public class AuthClient : IAuthClient
{
    private readonly Api _api;

    public AuthClient(ClientOptions options)
    {
        _ = options ?? throw new ArgumentNullException(nameof(options));
        ArgumentNullException.ThrowIfNull(options.Url);

        _api = new Api(options.Url!, options.Headers);
    }

    /// <summary>
    ///     Refreshes a Token
    /// </summary>
    /// <returns></returns>
    internal async Task<Session> RefreshToken(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            throw new Exception("No current session.");

        var result = await _api.RefreshAccessToken(refreshToken);

        if (string.IsNullOrEmpty(result?.AccessToken))
            throw new Exception("Could not refresh token from provided session.");

        return result;
    }

    /// <summary>
    ///     Signs up a user by email address
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <param name="options">Object containing redirectTo and optional user metadata (data)</param>
    /// <returns></returns>
    public Task<Session?> SignUpAsync(string email, string password, SignUpOptions? options = null)
    {
        return SignUpAsync(SignUpType.Email, email, password, options);
    }

    /// <summary>
    ///     Signs up a user
    /// </summary>
    /// <param name="type"></param>
    /// <param name="identifier"></param>
    /// <param name="password"></param>
    /// <param name="options">Object containing redirectTo and optional user metadata (data)</param>
    /// <returns></returns>
    public async Task<Session?> SignUpAsync(SignUpType type, string identifier, string password,
        SignUpOptions? options = null)
    {
        try
        {
            Session? session = null;
            switch (type)
            {
                case SignUpType.Email:
                    session = await _api.SignUpWithEmail(identifier, password, options);
                    break;
                case SignUpType.Phone:
                    session = await _api.SignUpWithPhone(identifier, password, options);
                    break;
            }

            return session;
        }
        catch (RequestException ex)
        {
            throw ExceptionHandler.Parse(ex);
        }
    }


    /// <summary>
    ///     Sends a Magic email login link to the specified email.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public async Task<bool> SignInAsync(string email, SignInOptions? options = null)
    {
        try
        {
            await _api.SendMagicLinkEmail(email, options);
            return true;
        }
        catch (RequestException ex)
        {
            throw ExceptionHandler.Parse(ex);
        }
    }

    /// <summary>
    ///     Sends a Magic email login link to the specified email.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public Task<bool> SendMagicLinkAsync(string email, SignInOptions? options = null)
    {
        return SignInAsync(email, options);
    }


    /// <summary>
    ///     Signs in a User.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public Task<Session?> SignInAsync(string email, string password)
    {
        return SignInAsync(SignInType.Email, email, password);
    }

    /// <summary>
    ///     Log in an existing user, or login via a third-party provider.
    /// </summary>
    /// <param name="type">Type of Credentials being passed</param>
    /// <param name="identifierOrToken">An email, phone, or RefreshToken</param>
    /// <param name="password">Password to account (optional if `RefreshToken`)</param>
    /// <param name="scopes">A space-separated list of scopes granted to the OAuth application.</param>
    /// <returns></returns>
    public async Task<Session?> SignInAsync(SignInType type, string identifierOrToken, string? password = null,
        string? scopes = null)
    {
        try
        {
            Session? session = null;
            switch (type)
            {
                case SignInType.Email:
                    session = await _api.SignInWithEmail(identifierOrToken, password);
                    break;
                case SignInType.Phone:
                    if (string.IsNullOrEmpty(password))
                    {
                        await _api.SendMobileOTP(identifierOrToken);
                        return null;
                    }

                    session = await _api.SignInWithPhone(identifierOrToken, password);
                    break;
                case SignInType.RefreshToken:
                    session = await RefreshToken(identifierOrToken);
                    break;
            }

            return session;
        }
        catch (RequestException ex)
        {
            throw ExceptionHandler.Parse(ex);
        }
    }

    /// <summary>
    ///     Retrieves a Url to redirect to for signing in with a <see cref="Provider" />.
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
    public string SignIn(Provider provider, string? scopes = null)
    {
        var url = _api.GetUrlForProvider(provider, scopes);
        return url;
    }

    /// <summary>
    ///     Log in a user given a User supplied OTP received via mobile.
    /// </summary>
    /// <param name="phone">The user's phone number.</param>
    /// <param name="token">Token sent to the user's phone.</param>
    /// <returns></returns>
    public async Task<Session?> VerifyOTPAsync(string phone, string token)
    {
        try
        {
            var session = await _api.VerifyMobileOTP(phone, token);

            if (session?.AccessToken != null) return session;

            return null;
        }
        catch (RequestException ex)
        {
            throw ExceptionHandler.Parse(ex);
        }
    }

    /// <summary>
    ///     Signs out a user and invalidates the current token.
    /// </summary>
    /// <returns></returns>
    public async Task SignOutAsync(string accessToken)
    {
        await _api.SignOut(accessToken);
    }

    /// <summary>
    ///     Updates a User.
    /// </summary>
    /// <param name="accessToken"></param>
    /// <param name="attributes"></param>
    /// <returns></returns>
    public async Task<User?> UpdateAsync(string accessToken, UserAttributes attributes)
    {
        if (string.IsNullOrEmpty(accessToken))
            throw new Exception("Not Logged in.");

        try
        {
            var result = await _api.UpdateUser(accessToken, attributes);

            return result;
        }
        catch (RequestException ex)
        {
            throw ExceptionHandler.Parse(ex);
        }
    }

    /// <summary>
    ///     Sends an invite email link to the specified email.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="jwt">this token needs role 'supabase_admin' or 'service_role'</param>
    /// <returns></returns>
    public async Task<bool> InviteUserByEmailAsync(string email, string jwt)
    {
        try
        {
            var response = await _api.InviteUserByEmail(email, jwt);
            response.ResponseMessage.EnsureSuccessStatusCode();
            return true;
        }
        catch (RequestException ex)
        {
            throw ExceptionHandler.Parse(ex);
        }
    }

    /// <summary>
    ///     Deletes a User.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="jwt">this token needs role 'supabase_admin' or 'service_role'</param>
    /// <returns></returns>
    public async Task<bool> DeleteUserAsync(string uid, string jwt)
    {
        try
        {
            var result = await _api.DeleteUser(uid, jwt);
            result.ResponseMessage.EnsureSuccessStatusCode();
            return true;
        }
        catch (RequestException ex)
        {
            throw ExceptionHandler.Parse(ex);
        }
    }

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
    public async Task<UserList?> ListUsersAsync(string jwt, string? filter = null, string? sortBy = null,
        Constants.SortOrder sortOrder = Constants.SortOrder.Descending, int? page = null, int? perPage = null)
    {
        try
        {
            return await _api.ListUsers(jwt, filter, sortBy, sortOrder, page, perPage);
        }
        catch (RequestException ex)
        {
            throw ExceptionHandler.Parse(ex);
        }
    }

    /// <summary>
    ///     Get User details by Id
    /// </summary>
    /// <param name="jwt">A valid JWT. Must be a full-access API key (e.g. service_role key).</param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<User?> GetUserByIdAsync(string jwt, string userId)
    {
        try
        {
            return await _api.GetUserById(jwt, userId);
        }
        catch (RequestException ex)
        {
            throw ExceptionHandler.Parse(ex);
        }
    }

    /// <summary>
    ///     Create a user (as a service_role)
    /// </summary>
    /// <param name="jwt">A valid JWT. Must be a full-access API key (e.g. service_role key).</param>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <param name="attributes"></param>
    /// <returns></returns>
    public Task<User?> CreateUserAsync(string jwt, string email, string password,
        AdminUserAttributes? attributes = null)
    {
        if (attributes == null) attributes = new AdminUserAttributes();
        attributes.Email = email;
        attributes.Password = password;

        return CreateUserAsync(jwt, attributes);
    }

    /// <summary>
    ///     Create a user (as a service_role)
    /// </summary>
    /// <param name="jwt">A valid JWT. Must be a full-access API key (e.g. service_role key).</param>
    /// <param name="attributes"></param>
    /// <returns></returns>
    public async Task<User?> CreateUserAsync(string jwt, AdminUserAttributes attributes)
    {
        try
        {
            return await _api.CreateUser(jwt, attributes);
        }
        catch (RequestException ex)
        {
            throw ExceptionHandler.Parse(ex);
        }
    }

    /// <summary>
    ///     UpdateAsync user by Id
    /// </summary>
    /// <param name="jwt">A valid JWT. Must be a full-access API key (e.g. service_role key).</param>
    /// <param name="userId"></param>
    /// <param name="userData"></param>
    /// <returns></returns>
    public async Task<User?> UpdateUserByIdAsync(string jwt, string userId, AdminUserAttributes userData)
    {
        try
        {
            return await _api.UpdateUserById(jwt, userId, userData);
        }
        catch (RequestException ex)
        {
            throw ExceptionHandler.Parse(ex);
        }
    }

    /// <summary>
    ///     Sends a reset request to an email address.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<bool> ResetPasswordForEmailAsync(string email)
    {
        try
        {
            var result = await _api.ResetPasswordForEmail(email);
            result.ResponseMessage.EnsureSuccessStatusCode();
            return true;
        }
        catch (RequestException ex)
        {
            throw ExceptionHandler.Parse(ex);
        }
    }

    /// <summary>
    ///     Refreshes the currently logged in User's Session.
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    public async Task<Session> RefreshSessionAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            throw new Exception("Not Logged in.");

        var session = await RefreshToken(refreshToken);

        session.User = await _api.GetUser(session.AccessToken);
        return session;
    }

    /// <summary>
    ///     Parses a <see cref="Session" /> out of a <see cref="Uri" />'s Query parameters.
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="storeSession"></param>
    /// <returns></returns>
    public async Task<Session?> GetSessionFromUrlAsync(Uri uri, bool storeSession = true)
    {
        var query = string.IsNullOrEmpty(uri.Fragment)
            ? HttpUtility.ParseQueryString(uri.Query)
            : HttpUtility.ParseQueryString('?' + uri.Fragment.TrimStart('#'));

        var errorDescription = query.Get("error_description");

        if (!string.IsNullOrEmpty(errorDescription))
            throw new Exception(errorDescription);

        var accessToken = query.Get("access_token");

        if (string.IsNullOrEmpty(accessToken))
            throw new Exception("No access_token detected.");

        var expiresIn = query.Get("expires_in");

        if (string.IsNullOrEmpty(expiresIn))
            throw new Exception("No expires_in detected.");

        var refreshToken = query.Get("refresh_token");

        if (string.IsNullOrEmpty(refreshToken))
            throw new Exception("No refresh_token detected.");

        var tokenType = query.Get("token_type");

        if (string.IsNullOrEmpty(tokenType))
            throw new Exception("No token_type detected.");

        var user = await _api.GetUser(accessToken);

        var session = new Session
        {
            AccessToken = accessToken,
            ExpiresIn = int.Parse(expiresIn),
            RefreshToken = refreshToken,
            TokenType = tokenType,
            User = user
        };

        return session;
    }
}
