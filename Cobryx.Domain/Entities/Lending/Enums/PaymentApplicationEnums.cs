namespace Cobryx.Domain.Entities.Lending.Enums;

public enum PaymentApplicationType
{
    LateFees = 1,
    Interest = 2,
    Principal = 3
}

public enum PaymentApplicationMode
{
    Standard = 1,  // LateFees -> Interest -> Principal
    Custom = 2
}
