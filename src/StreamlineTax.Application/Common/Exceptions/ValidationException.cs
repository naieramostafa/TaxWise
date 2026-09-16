using FluentValidation.Results;

namespace StreamlineTax.Application.Common.Exceptions;

public class ValidationException : ApplicationException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base("Validation failed.", "VALIDATION_ERROR")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(ValidationFailure[] failures)
        : base("Validation failed.", "VALIDATION_ERROR")
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }
}
