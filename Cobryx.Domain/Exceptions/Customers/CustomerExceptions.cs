using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.Customers;

public class CustomerNotFoundException : CobryxException
{
    public override string ErrorCode => "CUSTOMER.NOT_FOUND";

    public CustomerNotFoundException(Guid customerId)
        : base($"Customer with ID {customerId} was not found.")
    {
        Metadata.Add("CustomerId", customerId);
    }
}

public class DuplicateCustomerException : CobryxException
{
    public override string ErrorCode => "CUSTOMER.DUPLICATE";

    public DuplicateCustomerException(string message) : base(message)
    {
    }
}
