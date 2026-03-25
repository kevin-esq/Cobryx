namespace Cobryx.Application.Common.Exceptions;

public class DeterminismViolationException : Exception
{
    public DeterminismViolationException()
        : base("Mathematical determinism violated during decision replay. The execution trace drifted from the original snapshot.")
    {
    }

    public DeterminismViolationException(string message)
        : base(message)
    {
    }

    public DeterminismViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
