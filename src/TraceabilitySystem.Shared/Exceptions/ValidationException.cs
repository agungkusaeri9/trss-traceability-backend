namespace TraceabilitySystem.Shared.Exceptions;

public class ValidationException : AppException
{
    public List<string> ValidationErrors { get; }

    public ValidationException(List<string> errors)
        : base("One or more validation errors occurred.", 422)
    {
        ValidationErrors = errors;
    }
}
