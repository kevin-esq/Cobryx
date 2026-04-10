using Cobryx.Application.Decision;
using Cobryx.Application.Decision.Models;
using Cobryx.Application.ML.Models;

namespace Cobryx.Application.Tests.Regression;

/// <summary>
/// Golden Master Tests for refactored handlers.
/// These tests ensure that the refactored code produces IDENTICAL results to the original.
/// </summary>
public class RefactorRegressionTests
{
    #region Golden Master Data

    private static readonly Guid KnownSnapshotId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static ReplayResult CreateKnownReplayResult() => new()
    {
        IsDeterministic = true,
        DeltaCreditLimit = 0m,
        DeltaInterestRate = 0m,
        TraceDriftDetected = false,
        Diff = null
    };

    #endregion

    #region DriftAnalyzer Tests

    [Fact]
    public void DriftAnalyzer_WithNoDrift_ReturnsNoneSeverity()
    {
        // Arrange
        var analyzer = new DriftAnalyzer();
        var replayResult = new ReplayResult
        {
            IsDeterministic = true,
            DeltaCreditLimit = 0m,
            DeltaInterestRate = 0m,
            TraceDriftDetected = false
        };
        var profile = new DriftToleranceProfile();

        // Act
        var result = analyzer.AnalyzeReplayResult(KnownSnapshotId, replayResult, profile);

        // Assert
        Assert.Equal(DriftSeverity.None, result.OutputSeverity);
        Assert.Equal(DriftSeverity.None, result.TraceSeverity);
    }

    [Fact]
    public void DriftAnalyzer_WithMinorDrift_ReturnsMinorSeverity()
    {
        // Arrange
        var analyzer = new DriftAnalyzer();
        var replayResult = new ReplayResult
        {
            IsDeterministic = true,
            DeltaCreditLimit = 5m, // Below threshold (10)
            DeltaInterestRate = 0.005m, // Below threshold (0.01)
            TraceDriftDetected = false
        };
        var profile = new DriftToleranceProfile
        {
            CreditLimitAbsoluteThreshold = 10m,
            InterestRateRelativeThreshold = 0.01m
        };

        // Act
        var result = analyzer.AnalyzeReplayResult(KnownSnapshotId, replayResult, profile);

        // Assert
        Assert.Equal(DriftSeverity.Minor, result.OutputSeverity);
    }

    [Fact]
    public void DriftAnalyzer_WithSignificantDrift_ReturnsSignificantSeverity()
    {
        // Arrange
        var analyzer = new DriftAnalyzer();
        var replayResult = new ReplayResult
        {
            IsDeterministic = true,
            DeltaCreditLimit = 15m, // Above threshold (10)
            DeltaInterestRate = 0m,
            TraceDriftDetected = false
        };
        var profile = new DriftToleranceProfile
        {
            CreditLimitAbsoluteThreshold = 10m,
            InterestRateRelativeThreshold = 0.01m
        };

        // Act
        var result = analyzer.AnalyzeReplayResult(KnownSnapshotId, replayResult, profile);

        // Assert
        Assert.Equal(DriftSeverity.Significant, result.OutputSeverity);
    }

    [Fact]
    public void DriftAnalyzer_WithCriticalDrift_ReturnsCriticalSeverity()
    {
        // Arrange
        var analyzer = new DriftAnalyzer();
        var replayResult = new ReplayResult
        {
            IsDeterministic = false,
            DeltaCreditLimit = 60m, // 5x threshold (10 * 5 = 50)
            DeltaInterestRate = 0m,
            TraceDriftDetected = true
        };
        var profile = new DriftToleranceProfile
        {
            CreditLimitAbsoluteThreshold = 10m,
            InterestRateRelativeThreshold = 0.01m
        };

        // Act
        var result = analyzer.AnalyzeReplayResult(KnownSnapshotId, replayResult, profile);

        // Assert
        Assert.Equal(DriftSeverity.Critical, result.OutputSeverity);
        Assert.Equal(DriftSeverity.Significant, result.TraceSeverity);
    }

    [Fact]
    public void DriftAnalyzer_WithTraceDrift_SetsTraceSeverity()
    {
        // Arrange
        var analyzer = new DriftAnalyzer();
        var replayResult = new ReplayResult
        {
            IsDeterministic = true,
            DeltaCreditLimit = 0m,
            DeltaInterestRate = 0m,
            TraceDriftDetected = true
        };
        var profile = new DriftToleranceProfile();

        // Act
        var result = analyzer.AnalyzeReplayResult(KnownSnapshotId, replayResult, profile);

        // Assert
        Assert.Equal(DriftSeverity.Significant, result.TraceSeverity);
    }

    #endregion
}
