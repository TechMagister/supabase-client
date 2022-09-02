namespace SupabaseAuth.Exceptions;

public class ExistingUserException : Exception
{
    public HttpResponseMessage Response { get; }
    public string? Content { get; }

    public ExistingUserException(RequestException exception)
    {
        Response = exception.Response;
        Content = exception.Error.Message;
    }
}
