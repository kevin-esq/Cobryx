namespace Cobryx.Application.Common.Validation;

public static class ProductValidationErrors
{
    private const string Prefix = "VALIDATION.PRODUCT";

    public static class Name
    {
        private const string FieldPrefix = $"{Prefix}.NAME";
        public const string Required = $"{FieldPrefix}.REQUIRED";
        public const string TooLong = $"{FieldPrefix}.TOO_LONG";
    }

    public static class BasePrice
    {
        private const string FieldPrefix = $"{Prefix}.BASE_PRICE";
        public const string Required = $"{FieldPrefix}.REQUIRED";
    }

    public static class InterestRate
    {
        private const string FieldPrefix = $"{Prefix}.INTEREST_RATE";
        public const string NegativeForbidden = $"{FieldPrefix}.NEGATIVE_FORBIDDEN";
    }

    public static class MaxInstallments
    {
        private const string FieldPrefix = $"{Prefix}.MAX_INSTALLMENTS";
        public const string MustBePositive = $"{FieldPrefix}.POSITIVE_REQUIRED";
    }
}
