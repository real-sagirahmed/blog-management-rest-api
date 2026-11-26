namespace BlogManagement.API.Exceptions;

public sealed class ConflictException(string message)
    : ApiException(StatusCodes.Status409Conflict, "Conflict", message);
