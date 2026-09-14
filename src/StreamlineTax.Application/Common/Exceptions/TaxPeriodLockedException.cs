namespace StreamlineTax.Application.Common.Exceptions;

public class TaxPeriodLockedException : Exception
{
    public TaxPeriodLockedException(string message) : base(message) { }
}