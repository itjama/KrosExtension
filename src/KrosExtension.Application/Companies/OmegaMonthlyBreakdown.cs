namespace KrosExtension.Application.Companies;

public sealed record OmegaMonthlyBreakdown(
    int Year,
    int Month,
    long Registered,
    long Posted,
    decimal? PostingRate);
