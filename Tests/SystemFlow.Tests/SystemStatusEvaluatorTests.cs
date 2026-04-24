using FluentAssertions;
using SystemMonitorApp.Models;
using SystemMonitorApp.Services;
using Xunit;

namespace SystemFlow.Tests;

public class SystemStatusEvaluatorTests
{
    // ---------------------------------------------------------------------------
    // Optimal
    // ---------------------------------------------------------------------------

    [Fact]
    public void Evaluate_WhenAllValuesZero_ReturnsOptimal()
    {
        var result = SystemStatusEvaluator.Evaluate(0f, 0f, 0f);
        result.Should().Be(SystemStatusEvaluator.Optimal);
    }

    [Fact]
    public void Evaluate_WhenAllValuesLow_ReturnsOptimal()
    {
        var result = SystemStatusEvaluator.Evaluate(30f, 40f, 45f);
        result.Should().Be(SystemStatusEvaluator.Optimal);
    }

    [Fact]
    public void Evaluate_WhenNegativeTemperature_ReturnsOptimalAndDoesNotThrow()
    {
        // Negative temp = sensor unavailable; must not crash and should not trigger High
        var result = SystemStatusEvaluator.Evaluate(0f, 0f, -1f);
        result.Should().Be(SystemStatusEvaluator.Optimal);
    }

    // ---------------------------------------------------------------------------
    // Medium – exact boundary (value must be *above* threshold, so threshold+1)
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData(61f, 0f,   0f)]   // cpu alone crosses Medium
    [InlineData(0f,  71f,  0f)]   // mem alone crosses Medium
    [InlineData(0f,  0f,   61f)]  // temp alone crosses Medium
    public void Evaluate_WhenExactlyAboveMediumThreshold_ReturnsMedium(
        float cpu, float mem, float temp)
    {
        var result = SystemStatusEvaluator.Evaluate(cpu, mem, temp);
        result.Should().Be(SystemStatusEvaluator.Medium);
    }

    [Fact]
    public void Evaluate_WhenCpuAtExactMediumThreshold_ReturnsOptimal()
    {
        // 60f is NOT above 60 → no Medium
        var result = SystemStatusEvaluator.Evaluate(60f, 0f, 0f);
        result.Should().Be(SystemStatusEvaluator.Optimal);
    }

    // ---------------------------------------------------------------------------
    // High – exact boundary
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData(81f, 0f,   0f)]   // cpu alone crosses High
    [InlineData(0f,  86f,  0f)]   // mem alone crosses High
    [InlineData(0f,  0f,   76f)]  // temp alone crosses High
    public void Evaluate_WhenExactlyAboveHighThreshold_ReturnsHigh(
        float cpu, float mem, float temp)
    {
        var result = SystemStatusEvaluator.Evaluate(cpu, mem, temp);
        result.Should().Be(SystemStatusEvaluator.High);
    }

    // ---------------------------------------------------------------------------
    // High wins over Medium
    // ---------------------------------------------------------------------------

    [Fact]
    public void Evaluate_WhenBothHighAndMediumThresholdsCrossed_ReturnsHigh()
    {
        // cpu=85 (above both Medium 60 and High 80), mem=75 (above Medium 70)
        var result = SystemStatusEvaluator.Evaluate(85f, 75f, 50f);
        result.Should().Be(SystemStatusEvaluator.High);
    }

    [Fact]
    public void Evaluate_WhenCpuHighAndMemMedium_ReturnsHigh()
    {
        var result = SystemStatusEvaluator.Evaluate(81f, 71f, 0f);
        result.Should().Be(SystemStatusEvaluator.High);
    }

    // ---------------------------------------------------------------------------
    // Snapshot overload
    // ---------------------------------------------------------------------------

    [Fact]
    public void Evaluate_Snapshot_ReturnsOptimal_WhenAllMetricsLow()
    {
        var snapshot = new SystemSnapshot
        {
            CpuUsagePercent = 30f,
            MemoryUsagePercent = 40f,
            AverageTemperatureC = 45f
        };

        var result = SystemStatusEvaluator.Evaluate(snapshot);
        result.Should().Be(SystemStatusEvaluator.Optimal);
    }

    [Fact]
    public void Evaluate_Snapshot_ReturnsSameAsThreeArgumentOverload()
    {
        float cpu = 75f, mem = 88f, temp = 50f;

        var snapshot = new SystemSnapshot
        {
            CpuUsagePercent = cpu,
            MemoryUsagePercent = mem,
            AverageTemperatureC = temp
        };

        var fromSnapshot = SystemStatusEvaluator.Evaluate(snapshot);
        var fromArgs     = SystemStatusEvaluator.Evaluate(cpu, mem, temp);

        fromSnapshot.Should().Be(fromArgs);
    }

    [Fact]
    public void Evaluate_Snapshot_ReturnsHigh_WhenMemAboveHighThreshold()
    {
        var snapshot = new SystemSnapshot
        {
            CpuUsagePercent = 0f,
            MemoryUsagePercent = 90f,
            AverageTemperatureC = 0f
        };

        SystemStatusEvaluator.Evaluate(snapshot).Should().Be(SystemStatusEvaluator.High);
    }
}
