using MediatR;

namespace StreamlineTax.Application.Receipts.Commands.UploadReceipt;

public record UploadReceiptCommand(Guid UserId, string FileName, string ContentType, Stream FileStream) : IRequest<UploadReceiptResponse>;
