using FluentValidation;

namespace StreamlineTax.Application.Receipts.Commands.UploadReceipt;

public class UploadReceiptCommandValidator : AbstractValidator<UploadReceiptCommand>
{
    public UploadReceiptCommandValidator()
    {
        RuleFor(v => v.UserId).NotEmpty();
        RuleFor(v => v.FileName).NotEmpty().MaximumLength(255);
        RuleFor(v => v.ContentType).NotEmpty();
        RuleFor(v => v.FileStream).NotNull();
    }
}
