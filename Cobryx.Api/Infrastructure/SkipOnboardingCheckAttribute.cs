namespace Cobryx.Api.Infrastructure;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SkipOnboardingCheckAttribute : Attribute
{
}
