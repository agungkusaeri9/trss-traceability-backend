namespace TraceabilitySystem.Shared.Exceptions;

public class NotFoundException : AppException
{
    public NotFoundException(string name, object key)
        : base($"{name} with id '{key}' was not found.", 404) { }
}
