namespace KrosExtension.Application.Companies;

public sealed record OlympMonthlyPeriod(
    string PeriodId,
    bool HasMzda,
    bool HasBase,
    bool HasOdvod);
