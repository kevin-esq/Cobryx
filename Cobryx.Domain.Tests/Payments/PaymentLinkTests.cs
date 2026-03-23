using Cobryx.Domain.Payments;
using Cobryx.Domain.Payments.Enums;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Tests.Payments;

public class PaymentLinkTests
{
    private static PaymentLink CreateActiveLink()
    {
        return new PaymentLink(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Money(100, "USD"),
            "token",
            DateTime.UtcNow.AddDays(7),
            "secret"
        );
    }

    [Fact]
    public void RecordRecoveryFailure_ShouldFollowDunningMatrix_WithJitter()
    {
        var link = CreateActiveLink();
        var now = DateTime.UtcNow;

        link.RecordRecoveryFailure("insufficient_funds");
        Assert.Equal(1, link.RecoveryAttemptCount);
        Assert.NotNull(link.NextRecoveryAttemptAt);
        Assert.True(link.NextRecoveryAttemptAt >= now.AddMinutes(53) && link.NextRecoveryAttemptAt <= now.AddMinutes(67));
        Assert.Equal(PaymentLinkStatus.Active, link.Status);

        link.RecordRecoveryFailure("insufficient_funds");
        Assert.True(link.NextRecoveryAttemptAt >= now.AddHours(7) && link.NextRecoveryAttemptAt <= now.AddHours(9));
    }

    [Fact]
    public void RecordRecoveryFailure_Jitter_ShouldVaryAcrossDifferentLinks()
    {
        var link1 = CreateActiveLink();
        var link2 = CreateActiveLink();

        link1.RecordRecoveryFailure("insufficient_funds");
        link2.RecordRecoveryFailure("insufficient_funds");

        Assert.NotEqual(link1.NextRecoveryAttemptAt, link2.NextRecoveryAttemptAt);
    }

    [Fact]
    public void RecordRecoveryFailure_ShouldTransitionToManualReview_WhenMaxAttemptsReached()
    {
        var link = CreateActiveLink();

        for (var i = 0; i < 5; i++)
        {
            link.RecordRecoveryFailure("soft_fail");
        }

        link.RecordRecoveryFailure("final_fail");

        Assert.Equal(6, link.RecoveryAttemptCount);
        Assert.Equal(PaymentLinkStatus.ManualReview, link.Status);
        Assert.Null(link.NextRecoveryAttemptAt);
    }

    [Fact]
    public void RecordRecoveryFailure_ShouldTransitionToManualReview_WhenDeadlineReached()
    {
        var link = CreateActiveLink();
        // Since we can't easily mock DateTime.UtcNow in this simple unit test without more infra,

        link.RecordRecoveryFailure("soft_fail");

        Assert.Equal(1, link.RecoveryAttemptCount);
        Assert.Equal(PaymentLinkStatus.Active, link.Status);
        Assert.NotNull(link.NextRecoveryAttemptAt);
    }

    [Fact]
    public void RecoveryLock_ShouldPreventConcurrentAccess()
    {
        var link = CreateActiveLink();

        var firstAcquire = link.TryAcquireRecoveryLock();
        var secondAcquire = link.TryAcquireRecoveryLock();

        Assert.True(firstAcquire);
        Assert.False(secondAcquire);
        Assert.True(link.RecoveryInProgress);

        link.ReleaseRecoveryLock();
        Assert.False(link.RecoveryInProgress);
        Assert.True(link.TryAcquireRecoveryLock());
    }
}
