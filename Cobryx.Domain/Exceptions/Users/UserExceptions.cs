using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.Users;

public class UserNotRegisteredException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.User.NotRegistered;
    public UserNotRegisteredException() : base() { }
}

public class UserEmailAlreadyExistsException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.User.EmailAlreadyExists;
    public UserEmailAlreadyExistsException() : base() { }
}

public class UserNotFoundException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.User.NotFound;
    public UserNotFoundException(Guid userId)
        : base(null!)
    {
        Metadata.Add("UserId", userId);
    }
}
