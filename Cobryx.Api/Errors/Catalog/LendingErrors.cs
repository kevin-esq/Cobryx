using Cobryx.Api.Errors.Definitions;

namespace Cobryx.Api.Errors.Catalog;

public static class LendingErrors
{
    public static readonly ErrorDefinition LoanNotFound = new(404, 6001);
    public static readonly ErrorDefinition AgreementNotFound = new(404, 6002);
    public static readonly ErrorDefinition AlreadySigned = new(400, 6003);
    public static readonly ErrorDefinition ImmutableAfterSigning = new(400, 6004);
    public static readonly ErrorDefinition LoanAlreadyClosed = new(400, 6005);
    public static readonly ErrorDefinition InvalidPaymentAmount = new(400, 6006);
    public static readonly ErrorDefinition InstallmentNotFound = new(404, 6007);
    public static readonly ErrorDefinition InstallmentAlreadyPaid = new(400, 6008);
    public static readonly ErrorDefinition InvalidPolicy = new(400, 6009);
    public static readonly ErrorDefinition CreditSaleNotFound = new(404, 6010);
}
