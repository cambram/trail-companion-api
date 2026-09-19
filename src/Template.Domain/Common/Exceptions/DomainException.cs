namespace Template.Domain.Common.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.")
    {
    }
}

public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}

public class ConflictException : DomainException
{
    public string Code { get; }

    public ConflictException(string code, string message) : base(message)
    {
        Code = code;
    }

    public ConflictException(string code, string message, Exception innerException) : base(message, innerException)
    {
        Code = code;
    }
}

public record ValidationFailure(string Code, string Message);

public class ValidationFailedException : DomainException
{
    public IReadOnlyList<ValidationFailure> Failures { get; }

    public ValidationFailedException(string message, IReadOnlyList<ValidationFailure> failures) : base(message)
    {
        Failures = failures;
    }
}
