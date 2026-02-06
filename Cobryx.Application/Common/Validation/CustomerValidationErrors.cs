namespace Cobryx.Application.Common.Validation;

public static class CustomerValidationErrors
{
    private const string Prefix = "VALIDATION.CUSTOMER";

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

    public static class Phone
    {
        private const string FieldPrefix = $"{Prefix}.PHONE";
        public const string Required = $"{FieldPrefix}.REQUIRED";
        public const string Invalid = $"{FieldPrefix}.INVALID";
    }

    public static class Address
    {
        private const string FieldPrefix = $"{Prefix}.ADDRESS";
        public const string StreetRequired = $"{FieldPrefix}.STREET_REQUIRED";
        public const string StreetTooLong = $"{FieldPrefix}.STREET_TOO_LONG";
        public const string ExtNumberRequired = $"{FieldPrefix}.EXT_NUMBER_REQUIRED";
        public const string ZipCodeRequired = $"{FieldPrefix}.ZIP_CODE_REQUIRED";
        public const string CityRequired = $"{FieldPrefix}.CITY_REQUIRED";
        public const string StateRequired = $"{FieldPrefix}.STATE_REQUIRED";
    }
}
