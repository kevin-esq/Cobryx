using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.Auth;

public class NotAuthenticatedException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.Auth.NotAuthenticated;
    public NotAuthenticatedException() : base() { }
}

public class InvalidTokenException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.Auth.InvalidToken;
    public InvalidTokenException() : base() { }
}

public class TokenMissingException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.Auth.TokenMissing;
    public TokenMissingException() : base() { }
}

public class MfaAlreadyEnabledException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.Auth.MfaAlreadyEnabled;
    public MfaAlreadyEnabledException() : base() { }
}

public class InvalidMfaCodeException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.Auth.InvalidMfaCode;
    public InvalidMfaCodeException() : base() { }
}

public class AccountInactiveException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.Auth.AccountInactive;
    public AccountInactiveException() : base() { }
}

public class ExternalLoginFailedException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.Auth.ExternalLoginFailed;
    public ExternalLoginFailedException() : base() { }
}

public class AccountLockedException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.Auth.AccountLocked;
    public AccountLockedException() : base() { }
}

public class EmailNotVerifiedException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.Auth.EmailNotVerified;
    public EmailNotVerifiedException() : base() { }
}

public class InvalidCredentialsException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.Auth.InvalidCredentials;
    public InvalidCredentialsException() : base() { }
}
