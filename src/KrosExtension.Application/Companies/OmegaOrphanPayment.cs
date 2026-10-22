namespace KrosExtension.Application.Companies;

public sealed record OmegaOrphanPayment(
    long PaymentId,
    long OrphanedDocumentId,
    string DocumentType,
    string InternalNumber,
    string? PaymentDate,
    decimal Amount,
    string Currency,
    string PaymentKind,
    string? Note);

public sealed record OmegaOrphanPaymentsResult(
    long TotalCount,
    IReadOnlyList<OmegaOrphanPayment> Items);
