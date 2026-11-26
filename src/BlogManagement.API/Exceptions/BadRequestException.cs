namespace BlogManagement.API.Exceptions;

public sealed class BadRequestException(string message)
    : ApiException(StatusCodes.Status400BadRequest, "Bad Request", message);
