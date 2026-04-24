using FluentAssertions;
using SystemMonitorApp.Services;
using Xunit;

namespace SystemFlow.Tests;

public class OperatingSystemNamesTests
{
    // Helper: construct a Win32NT OperatingSystem from major.minor.build
    private static System.OperatingSystem Win32(int major, int minor, int build)
        => new System.OperatingSystem(System.PlatformID.Win32NT, new Version(major, minor, build, 0));

    // ---------------------------------------------------------------------------
    // Windows 11 — Build >= 22000
    // ---------------------------------------------------------------------------

    [Fact]
    public void GetFriendlyName_Win10Build22000_ReturnsWindows11()
    {
        var os = Win32(10, 0, 22000);
        OperatingSystemNames.GetFriendlyName(os).Should().Be("Windows 11");
    }

    [Fact]
    public void GetFriendlyName_Win10BuildAbove22000_ReturnsWindows11()
    {
        var os = Win32(10, 0, 26100);
        OperatingSystemNames.GetFriendlyName(os).Should().Be("Windows 11");
    }

    // ---------------------------------------------------------------------------
    // Windows 10 — Major=10 and Build < 22000
    // ---------------------------------------------------------------------------

    [Fact]
    public void GetFriendlyName_Win10Build19045_ReturnsWindows10()
    {
        var os = Win32(10, 0, 19045);
        OperatingSystemNames.GetFriendlyName(os).Should().Be("Windows 10");
    }

    [Fact]
    public void GetFriendlyName_Win10Build0_ReturnsWindows10()
    {
        var os = Win32(10, 0, 0);
        OperatingSystemNames.GetFriendlyName(os).Should().Be("Windows 10");
    }

    // ---------------------------------------------------------------------------
    // Windows 8.1 — Major=6, Minor=3
    // ---------------------------------------------------------------------------

    [Fact]
    public void GetFriendlyName_Win63_ReturnsWindows81()
    {
        var os = Win32(6, 3, 9600);
        OperatingSystemNames.GetFriendlyName(os).Should().Be("Windows 8.1");
    }

    // ---------------------------------------------------------------------------
    // Windows 8 — Major=6, Minor=2
    // ---------------------------------------------------------------------------

    [Fact]
    public void GetFriendlyName_Win62_ReturnsWindows8()
    {
        var os = Win32(6, 2, 9200);
        OperatingSystemNames.GetFriendlyName(os).Should().Be("Windows 8");
    }

    // ---------------------------------------------------------------------------
    // Windows 7 — Major=6, Minor=1
    // ---------------------------------------------------------------------------

    [Fact]
    public void GetFriendlyName_Win61_ReturnsWindows7()
    {
        var os = Win32(6, 1, 7601);
        OperatingSystemNames.GetFriendlyName(os).Should().Be("Windows 7");
    }

    // ---------------------------------------------------------------------------
    // Unknown Win32NT version — fallback to "Windows NT Major.Minor"
    // ---------------------------------------------------------------------------

    [Fact]
    public void GetFriendlyName_Win60_ReturnsWindowsNTFallback()
    {
        // Major=6, Minor=0 is Vista — has no dedicated branch
        var os = Win32(6, 0, 6002);
        OperatingSystemNames.GetFriendlyName(os).Should().Be("Windows NT 6.0");
    }

    [Fact]
    public void GetFriendlyName_Win5_ReturnsWindowsNTFallback()
    {
        var os = Win32(5, 1, 2600);
        OperatingSystemNames.GetFriendlyName(os).Should().Be("Windows NT 5.1");
    }

    // ---------------------------------------------------------------------------
    // Non-Win32NT platform — fallback to os.ToString()
    // ---------------------------------------------------------------------------

    [Fact]
    public void GetFriendlyName_UnixPlatform_ReturnsFallbackString()
    {
        var os = new System.OperatingSystem(System.PlatformID.Unix, new Version(5, 15, 0));
        var result = OperatingSystemNames.GetFriendlyName(os);
        result.Should().Be(os.ToString());
    }
}
