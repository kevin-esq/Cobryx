using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.Auth;

public class NotAuthenticatedException : CobryxException
{
    public override string ErrorCode => "AUTH.NOT_AUTHENTICATED";
    public NotAuthenticatedException(string message = "User not authenticated.") : base(message) { }
}

public class InvalidTokenException : CobryxException
{
    public override string ErrorCode => "AUTH.TOKEN.INVALID";
    public InvalidTokenException(string message = "Invalid or expired token.") : base(message) { }
}

public class TokenMissingException : CobryxException
{
    public override string ErrorCode => "AUTH.TOKEN.MISSING";
    public TokenMissingException(string message = "Refresh token cookie missing.") : base(message) { }
}

public class MfaAlreadyEnabledException : CobryxException
{
    public override string ErrorCode => "AUTH.MFA.ALREADY_ENABLED";
    public MfaAlreadyEnabledException(string message = "MFA is already enabled.") : base(message) { }
}

public class InvalidMfaCodeException : CobryxException
{
    public override string ErrorCode => "AUTH.MFA.INVALID_CODE";
    public InvalidMfaCodeException(string message = "Invalid verification code.") : base(message) { }
}

public class AccountInactiveException : CobryxException
{
    public override string ErrorCode => "AUTH.ACCOUNT_INACTIVE";
    public AccountInactiveException(string message = "Your account is inactive.") : base(message) { }
}

public class ExternalLoginFailedException : CobryxException
{
    public override string ErrorCode => "AUTH.EXTERNAL_LOGIN_FAILED";
    public ExternalLoginFailedException(string message = "External authentication failed.") : base(message) { }
}

public class AccountLockedException : CobryxException
{
    public override string ErrorCode => "AUTH.ACCOUNT_LOCKED";
    public AccountLockedException(string message = "Account is locked, disabled or missing.") : base(message) { }
}

public class EmailNotVerifiedException : CobryxException
{
    public override string ErrorCode => "AUTH.EMAIL_NOT_VERIFIED";
    public EmailNotVerifiedException(string message = "Email verification is required.") : base(message) { }
}

public class InvalidCredentialsException : CobryxException
{
    public override string ErrorCode => "AUTH.INVALID_CREDENTIALS";
    public InvalidCredentialsException(string message = "Invalid credentials.") : base(message) { }
}
