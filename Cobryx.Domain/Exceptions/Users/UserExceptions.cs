using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.Users;

public class UserNotRegisteredException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.User.NotRegistered;
    public UserNotRegisteredException() : base() { }
}

public class UserEmailAlreadyExistsException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.User.EmailAlreadyExists;
    public UserEmailAlreadyExistsException() : base() { }
}

public class UserNotFoundException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.User.NotFound;
    public UserNotFoundException(Guid userId)
        : base(null!)
    {
        Metadata.Add("UserId", userId);
    }
}
