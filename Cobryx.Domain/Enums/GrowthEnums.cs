namespace Cobryx.Domain.Enums;

public enum ChurnRisk
{
    Low = 0,
    Medium = 50,
    High = 90,
    Churned = 100
}

public enum ChurnType
{
    None = 0,
    Voluntary = 1,
    NonPayment = 2,
    Contraction = 3
}

public enum MRRChangeType
{
    None = 0,
    New = 1,
    Expansion = 2,
    Contraction = 3,
    Churn = 4,
    Reactivation = 5
}
