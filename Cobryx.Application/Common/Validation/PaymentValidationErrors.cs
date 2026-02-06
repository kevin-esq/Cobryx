namespace Cobryx.Application.Common.Validation;

public static class PaymentValidationErrors
{
    private const string Prefix = "VALIDATION.PAYMENT";

    public static class Amount
    {
        private const string FieldPrefix = $"{Prefix}.AMOUNT";
        public const string MustBePositive = $"{FieldPrefix}.POSITIVE_REQUIRED";
    }

    public static class Currency
    {
        private const string FieldPrefix = $"{Prefix}.CURRENCY";
        public const string Required = $"{FieldPrefix}.REQUIRED";
        public const string InvalidLength = $"{FieldPrefix}.INVALID_LENGTH";
    }

    public static class Date
    {
        private const string FieldPrefix = $"{Prefix}.DATE";
        public const string Required = $"{FieldPrefix}.REQUIRED";
    }
}
