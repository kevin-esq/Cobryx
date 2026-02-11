namespace Cobryx.Domain.Entities.Lending.Enums;

public enum PaymentFrequency
{
    Weekly = 1,
    Biweekly = 2,
    Monthly = 3,
    Custom = 4  // Uses DaysBetweenPayments
}

public enum RoundingMode
{
    None = 0,
    ToNearest = 1,
    ToTen = 2,
    ToFifty = 3,
    ToHundred = 4
}
