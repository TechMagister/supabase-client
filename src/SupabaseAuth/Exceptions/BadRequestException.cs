namespace SupabaseAuth.Exceptions;

public class BadRequestException : Exception
{
    public HttpResponseMessage Response { get; }

    public string? Content { get; }

    public BadRequestException(RequestException exception)
    {
        Response = exception.Response;
        Content = exception.Error.Message;
    }
}
