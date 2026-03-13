using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Exceptions.Auth;

public class NotAuthenticatedException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Auth.NotAuthenticated;
    public NotAuthenticatedException() : base() { }
}

public class InvalidTokenException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Auth.InvalidToken;
    public InvalidTokenException() : base() { }
}

public class TokenMissingException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Auth.TokenMissing;
    public TokenMissingException() : base() { }
}

public class MfaAlreadyEnabledException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Auth.MfaAlreadyEnabled;
    public MfaAlreadyEnabledException() : base() { }
}

public class InvalidMfaCodeException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Auth.InvalidMfaCode;
    public InvalidMfaCodeException() : base() { }
}

public class AccountInactiveException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Auth.AccountInactive;
    public AccountInactiveException() : base() { }
}

public class ExternalLoginFailedException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Auth.ExternalLoginFailed;
    public ExternalLoginFailedException() : base() { }
}

public class AccountLockedException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Auth.AccountLocked;
    public AccountLockedException() : base() { }
}

public class EmailNotVerifiedException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Auth.EmailNotVerified;
    public EmailNotVerifiedException() : base() { }
}

public class InvalidCredentialsException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Auth.InvalidCredentials;
    public InvalidCredentialsException() : base() { }
}
