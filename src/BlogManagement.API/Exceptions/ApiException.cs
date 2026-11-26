namespace BlogManagement.API.Exceptions;

public abstract class ApiException(int statusCode, string title, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
}
