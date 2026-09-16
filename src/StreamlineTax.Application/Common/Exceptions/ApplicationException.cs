namespace StreamlineTax.Application.Common.Exceptions;

public class ApplicationException : Exception
{
    public string ErrorCode { get; }

    public ApplicationException(string message)
        : base(message)
    {
        ErrorCode = "APPLICATION_ERROR";
    }

    public ApplicationException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public ApplicationException(string message, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
