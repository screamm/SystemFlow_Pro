using System;
using System.Threading;
using System.Threading.Tasks;
using SystemMonitorApp.Models;

namespace SystemMonitorApp.Services
{
    /// <summary>
    /// Abstracts all hardware data sources (LibreHardwareMonitor, WMI, PerformanceCounter).
    /// Implementations must be thread-safe for concurrent CollectSnapshotAsync calls,
    /// though in practice the ViewModel serializes calls via a tick-gate.
    /// </summary>
    public interface IHardwareService : IDisposable
    {
        /// <summary>True once hardware enumeration has completed successfully.</summary>
        bool IsInitialized { get; }

        /// <summary>True if the current process has admin privileges.</summary>
        bool IsRunningAsAdmin { get; }

        /// <summary>Total physical RAM in GB, cached at init. 0 if WMI query failed.</summary>
        float TotalMemoryGB { get; }

        /// <summary>
        /// Initialize hardware monitoring. Must be called once before CollectSnapshotAsync.
        /// Performs LHM.Open(), first Accept(), and WMI probes — takes 1-3 seconds on first run.
        /// </summary>
        Task InitializeAsync(CancellationToken ct = default);

        /// <summary>
        /// Captures current hardware state as an immutable snapshot.
        /// Safe to call from any thread; hardware reads are serialized internally.
        /// </summary>
        Task<SystemSnapshot> CollectSnapshotAsync(CancellationToken ct = default);

        /// <summary>Cached hardware info text (CPU name, OS, user). Never changes after init.</summary>
        string HardwareInfoText { get; }

        /// <summary>
        /// Writes a hardware diagnostic report to disk on a background thread. Conditional:
        /// only runs when troubleshooting data would be useful. Safe to call after the
        /// main window is shown; does not block the UI.
        /// </summary>
        void WriteDiagnosticReportInBackground();
    }
}
