namespace StreamlineTax.Application.Common.Exceptions;

public class NotFoundException : ApplicationException
{
    public NotFoundException(string message)
        : base(message, "NOT_FOUND")
    {
    }

    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.", "NOT_FOUND")
    {
    }
}
