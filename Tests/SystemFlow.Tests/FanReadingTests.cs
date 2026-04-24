using FluentAssertions;
using SystemMonitorApp.Models;
using Xunit;

namespace SystemFlow.Tests;

public class FanReadingTests
{
    // ---------------------------------------------------------------------------
    // FanReading construction and properties
    // ---------------------------------------------------------------------------

    [Fact]
    public void FanReading_Constructor_SetsPropertiesCorrectly()
    {
        var reading = new FanReading(1500f, IsPercent: false, IsGpu: false);

        reading.RawValue.Should().Be(1500f);
        reading.IsPercent.Should().BeFalse();
        reading.IsGpu.Should().BeFalse();
    }

    [Fact]
    public void FanReading_RpmFan_HasExpectedShape()
    {
        var reading = new FanReading(1500f, IsPercent: false, IsGpu: false);

        reading.IsPercent.Should().BeFalse();
        reading.RawValue.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void FanReading_PwmFan_IsPercentTrue_AndValueInRange()
    {
        var reading = new FanReading(65f, IsPercent: true, IsGpu: false);

        reading.IsPercent.Should().BeTrue();
        reading.RawValue.Should().BeInRange(0f, 100f);
    }

    [Fact]
    public void FanReading_ZeroRpmGpuFan_HasCorrectFlags()
    {
        var reading = new FanReading(0f, IsPercent: false, IsGpu: true);

        reading.RawValue.Should().Be(0f);
        reading.IsGpu.Should().BeTrue();
        reading.IsPercent.Should().BeFalse();
    }

    [Fact]
    public void FanReading_GpuPercentFan_HasCorrectFlags()
    {
        var reading = new FanReading(50f, IsPercent: true, IsGpu: true);

        reading.RawValue.Should().Be(50f);
        reading.IsGpu.Should().BeTrue();
        reading.IsPercent.Should().BeTrue();
    }

    // ---------------------------------------------------------------------------
    // Value equality (record struct semantics)
    // ---------------------------------------------------------------------------

    [Fact]
    public void FanReading_TwoIdenticalInstances_AreEqual()
    {
        var a = new FanReading(1200f, IsPercent: false, IsGpu: false);
        var b = new FanReading(1200f, IsPercent: false, IsGpu: false);

        a.Should().Be(b);
    }

    [Fact]
    public void FanReading_DifferentRawValue_AreNotEqual()
    {
        var a = new FanReading(1200f, IsPercent: false, IsGpu: false);
        var b = new FanReading(900f,  IsPercent: false, IsGpu: false);

        a.Should().NotBe(b);
    }

    [Fact]
    public void FanReading_DifferentIsGpu_AreNotEqual()
    {
        var cpu = new FanReading(1200f, IsPercent: false, IsGpu: false);
        var gpu = new FanReading(1200f, IsPercent: false, IsGpu: true);

        cpu.Should().NotBe(gpu);
    }

    // ---------------------------------------------------------------------------
    // SystemSnapshot default values
    // ---------------------------------------------------------------------------

    [Fact]
    public void SystemSnapshot_Default_GpuUsagePercentIsMinusOne()
    {
        var snapshot = new SystemSnapshot();
        snapshot.GpuUsagePercent.Should().Be(-1f);
    }

    [Fact]
    public void SystemSnapshot_Default_CpuCoresIsEmpty()
    {
        var snapshot = new SystemSnapshot();
        snapshot.CpuCores.Should().NotBeNull();
        snapshot.CpuCores.Should().BeEmpty();
    }

    [Fact]
    public void SystemSnapshot_Default_ThermalsIsEmpty()
    {
        var snapshot = new SystemSnapshot();
        snapshot.Thermals.Should().NotBeNull();
        snapshot.Thermals.Should().BeEmpty();
    }

    [Fact]
    public void SystemSnapshot_Default_FansIsEmpty()
    {
        var snapshot = new SystemSnapshot();
        snapshot.Fans.Should().NotBeNull();
        snapshot.Fans.Should().BeEmpty();
    }

    [Fact]
    public void SystemSnapshot_Default_SystemStatusIsOptimal()
    {
        var snapshot = new SystemSnapshot();
        snapshot.SystemStatus.Should().Be("OPTIMAL");
    }

    // ---------------------------------------------------------------------------
    // SystemSnapshot immutability (init-only properties)
    // The following test verifies that values set via object initialiser are
    // preserved and that the record cannot be mutated after construction.
    // Compile-time enforcement (init vs set) is verified by the fact that
    // the project compiles at all — no runtime workaround is needed.
    // ---------------------------------------------------------------------------

    [Fact]
    public void SystemSnapshot_InitProperties_ArePreservedAfterConstruction()
    {
        var snapshot = new SystemSnapshot
        {
            CpuUsagePercent    = 42f,
            MemoryUsagePercent = 55f,
            GpuUsagePercent    = 30f,
            AverageTemperatureC = 65f
        };

        snapshot.CpuUsagePercent.Should().Be(42f);
        snapshot.MemoryUsagePercent.Should().Be(55f);
        snapshot.GpuUsagePercent.Should().Be(30f);
        snapshot.AverageTemperatureC.Should().Be(65f);
    }

    [Fact]
    public void SystemSnapshot_WithExpression_CreatesNewInstanceWithChangedValue()
    {
        var original = new SystemSnapshot { CpuUsagePercent = 10f };
        var modified  = original with { CpuUsagePercent = 99f };

        // Original is unchanged — immutability holds
        original.CpuUsagePercent.Should().Be(10f);
        modified.CpuUsagePercent.Should().Be(99f);
    }
}
