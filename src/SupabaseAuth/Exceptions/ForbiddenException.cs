namespace SupabaseAuth.Exceptions;

public class ForbiddenException : Exception
{
    public HttpResponseMessage Response { get; }
    public string? Content { get; }

    public ForbiddenException(RequestException exception)
    {
        Response = exception.Response;
        Content = exception.Error.Message;
    }
}
