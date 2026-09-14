using FluentValidation;
using StreamlineTax.Application.Transactions.Commands.UpdateTransaction;
using StreamlineTax.Application.Transactions.Commands.CategorizeTransaction;
using StreamlineTax.Application.Transactions.Commands.DeleteTransaction;

namespace StreamlineTax.Api.Validators;

public class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TransactionDate).NotEmpty();
        RuleFor(x => x.Category).IsInEnum().WithMessage("Invalid transaction category");
    }
}

public class CategorizeTransactionCommandValidator : AbstractValidator<CategorizeTransactionCommand>
{
    public CategorizeTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Category).IsInEnum().WithMessage("Invalid transaction category");
    }
}

public class DeleteTransactionCommandValidator : AbstractValidator<DeleteTransactionCommand>
{
    public DeleteTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
