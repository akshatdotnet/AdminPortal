using Common.Application.Behaviors;

namespace Common.Application.Exceptions;

public sealed class ValidationException(IEnumerable<ValidationError> errors)
    : Exception("One or more validation failures occurred.")
{
    public IEnumerable<ValidationError> Errors { get; } = errors;
}

public sealed class NotFoundException(string entity, object key)
    : Exception($"{entity} with id '{key}' was not found.");

public sealed class BusinessRuleException(string message) : Exception(message);
public sealed class ConflictException(string message) : Exception(message);
