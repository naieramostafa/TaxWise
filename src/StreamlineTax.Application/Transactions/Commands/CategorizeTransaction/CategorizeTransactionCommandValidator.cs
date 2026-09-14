using FluentValidation;

namespace StreamlineTax.Application.Transactions.Commands.CategorizeTransaction;

public class CategorizeTransactionCommandValidator : AbstractValidator<CategorizeTransactionCommand>
{
    public CategorizeTransactionCommandValidator()
    {
        RuleFor(v => v.TransactionId).NotEmpty();
        RuleFor(v => v.UserId).NotEmpty();
        RuleFor(v => v.Category).IsInEnum();
    }
}
