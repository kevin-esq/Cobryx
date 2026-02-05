using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.Users;

public class UserNotRegisteredException : CobryxException
{
    public override string ErrorCode => "USER.NOT_REGISTERED";
    public UserNotRegisteredException(string message = "User does not exist. Please register first.") : base(message) { }
}

public class UserEmailAlreadyExistsException : CobryxException
{
    public override string ErrorCode => "USER.EMAIL_ALREADY_EXISTS";
    public UserEmailAlreadyExistsException(string message = "Email already registered.") : base(message) { }
}

public class UserNotFoundException : CobryxException
{
    public override string ErrorCode => "USER.NOT_FOUND";
    public UserNotFoundException(Guid userId)
        : base($"User with ID {userId} was not found.")
    {
        Metadata.Add("UserId", userId);
    }
}
