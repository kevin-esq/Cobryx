namespace Cobryx.Application.Common.Validation;

public static class AuthValidationErrors
{
    private const string Prefix = "VALIDATION.AUTH";

    public static class Email
    {
        private const string FieldPrefix = $"{Prefix}.EMAIL";
        public const string Required = $"{FieldPrefix}.REQUIRED";
        public const string Invalid = $"{FieldPrefix}.INVALID";
    }

    public static class Password
    {
        private const string FieldPrefix = $"{Prefix}.PASSWORD";
        public const string Required = $"{FieldPrefix}.REQUIRED";
        public const string TooShort = $"{FieldPrefix}.TOO_SHORT";
        public const string NoUppercase = $"{FieldPrefix}.NO_UPPERCASE";
        public const string NoLowercase = $"{FieldPrefix}.NO_LOWERCASE";
        public const string NoNumber = $"{FieldPrefix}.NO_NUMBER";
        public const string NoSpecial = $"{FieldPrefix}.NO_SPECIAL";
    }

    public static class FirstName
    {
        private const string FieldPrefix = $"{Prefix}.FIRST_NAME";
        public const string Required = $"{FieldPrefix}.REQUIRED";
        public const string TooLong = $"{FieldPrefix}.TOO_LONG";
    }

    public static class LastName
    {
        private const string FieldPrefix = $"{Prefix}.LAST_NAME";
        public const string Required = $"{FieldPrefix}.REQUIRED";
        public const string TooLong = $"{FieldPrefix}.TOO_LONG";
    }

    public static class BusinessName
    {
        private const string FieldPrefix = $"{Prefix}.BUSINESS_NAME";
        public const string Required = $"{FieldPrefix}.REQUIRED";
        public const string TooLong = $"{FieldPrefix}.TOO_LONG";
    }

    public static class Token
    {
        private const string FieldPrefix = $"{Prefix}.TOKEN";
        public const string Required = $"{FieldPrefix}.REQUIRED";
    }

    public static class Captcha
    {
        private const string FieldPrefix = $"{Prefix}.CAPTCHA";
        public const string Required = $"{FieldPrefix}.REQUIRED";
        public const string Invalid = $"{FieldPrefix}.INVALID";
    }
}
