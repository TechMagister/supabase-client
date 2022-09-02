namespace SupabaseAuth.Exceptions;

public class InvalidEmailOrPasswordException : Exception
{
    public HttpResponseMessage Response { get; }
    public string? Content { get; }

    public InvalidEmailOrPasswordException(RequestException exception)
    {
        Response = exception.Response;
        Content = exception.Error.Message;
    }
}
