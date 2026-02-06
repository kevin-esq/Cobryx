namespace Cobryx.Application.Common.Validation;

public static class CreditValidationErrors
{
    private const string Prefix = "VALIDATION.CREDIT";

    public static class Amount
    {
        private const string FieldPrefix = $"{Prefix}.AMOUNT";
        public const string MustBePositive = $"{FieldPrefix}.POSITIVE_REQUIRED";
    }

    public static class InterestRate
    {
        private const string FieldPrefix = $"{Prefix}.INTEREST_RATE";
        public const string NegativeForbidden = $"{FieldPrefix}.NEGATIVE_FORBIDDEN";
    }

    public static class Installments
    {
        private const string FieldPrefix = $"{Prefix}.INSTALLMENTS";
        public const string MustBePositive = $"{FieldPrefix}.POSITIVE_REQUIRED";
    }
}
