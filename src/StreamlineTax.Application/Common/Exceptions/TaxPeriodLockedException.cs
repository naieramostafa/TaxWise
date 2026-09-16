namespace StreamlineTax.Application.Common.Exceptions;

public class TaxPeriodLockedException : Exception
{
    public string ErrorCode { get; }

    public TaxPeriodLockedException(string message)
        : base(message)
    {
        ErrorCode = "TAX_PERIOD_LOCKED";
    }

    public TaxPeriodLockedException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
