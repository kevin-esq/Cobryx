namespace Cobryx.Application.Common.Validation;

public static class TenantValidationErrors
{
    private const string Prefix = "VALIDATION.TENANT";

    public static class TaxId
    {
        private const string FieldPrefix = $"{Prefix}.TAX_ID";
        public const string Required = $"{FieldPrefix}.REQUIRED";
        public const string Invalid = $"{FieldPrefix}.INVALID";
        public const string TooLong = $"{FieldPrefix}.TOO_LONG";
    }

    public static class Industry
    {
        private const string FieldPrefix = $"{Prefix}.INDUSTRY";
        public const string Required = $"{FieldPrefix}.REQUIRED";
        public const string TooLong = $"{FieldPrefix}.TOO_LONG";
    }

    public static class BusinessAddress
    {
        private const string FieldPrefix = $"{Prefix}.BUSINESS_ADDRESS";
        public const string Required = $"{FieldPrefix}.REQUIRED";
        public const string TooLong = $"{FieldPrefix}.TOO_LONG";
    }
}
