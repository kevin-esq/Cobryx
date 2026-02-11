using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.Customers;

public class CustomerNotFoundException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Customer.NotFound;

    public CustomerNotFoundException(Guid customerId)
        : base()
    {
        Metadata.Add("CustomerId", customerId);
    }
}

public class DuplicateCustomerException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Customer.Duplicate;

    public DuplicateCustomerException() : base()
    {
    }
}
