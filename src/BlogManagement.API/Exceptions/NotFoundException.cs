namespace BlogManagement.API.Exceptions;

public sealed class NotFoundException(string message)
    : ApiException(StatusCodes.Status404NotFound, "Not Found", message);
