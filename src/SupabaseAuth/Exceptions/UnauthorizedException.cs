using System.Diagnostics.CodeAnalysis;

namespace SupabaseAuth.Exceptions;

[ExcludeFromCodeCoverage]
public class UnauthorizedException : Exception
{
    public HttpResponseMessage Response { get; }

    public string? Content { get; }

    public UnauthorizedException(RequestException exception)
    {
        Response = exception.Response;
        Content = exception.Error.Message;
    }
}
