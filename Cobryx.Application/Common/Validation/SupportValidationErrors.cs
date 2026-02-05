namespace Cobryx.Application.Common.Validation;

public static class SupportValidationErrors
{
    private const string Prefix = "VALIDATION.SUPPORT";

    public static class Subject
    {
        private const string FieldPrefix = $"{Prefix}.SUBJECT";
        public const string Required = $"{FieldPrefix}.REQUIRED";
        public const string TooLong = $"{FieldPrefix}.TOO_LONG";
    }

    public static class Description
    {
        private const string FieldPrefix = $"{Prefix}.DESCRIPTION";
        public const string Required = $"{FieldPrefix}.REQUIRED";
    }
}
