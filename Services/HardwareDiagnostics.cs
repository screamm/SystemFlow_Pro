using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LibreHardwareMonitor.Hardware;

namespace SystemMonitorApp.Services
{
    /// <summary>
    /// Generates a full hardware report to help diagnose cases where fans or other sensors
    /// aren't detected. Writes to %APPDATA%\SystemFlow Pro\logs\hardware-report-{timestamp}.txt
    /// so users can send it for troubleshooting without us needing their machine.
    /// </summary>
    public static class HardwareDiagnostics
    {
        public static string? LastReportPath { get; private set; }

        /// <summary>
        /// Dump the full LHM hardware tree + native GetReport() to a timestamped file.
        /// Called automatically on startup (via HardwareService) so every run leaves a
        /// fresh diagnostic trail. Returns the path written to, or null on failure.
        /// </summary>
        public static string? DumpReport(Computer computer)
        {
            if (computer == null) return null;

            try
            {
                var logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SystemFlow Pro",
                    "logs");
                Directory.CreateDirectory(logDir);

                var path = Path.Combine(logDir,
                    $"hardware-report-{DateTime.Now:yyyy-MM-dd-HHmmss}.txt");

                var sb = new StringBuilder();
                sb.AppendLine("SystemFlow Pro — Hardware Diagnostic Report");
                sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"OS:        {Environment.OSVersion}");
                sb.AppendLine($".NET:      {Environment.Version}");
                sb.AppendLine($"User:      {Environment.UserName}");
                sb.AppendLine($"Admin:     {IsAdmin()}");
                sb.AppendLine(new string('=', 72));
                sb.AppendLine();

                // Section 1: Our custom hierarchical dump — easier to read
                sb.AppendLine("==== Hardware tree (SystemFlow Pro view) ====");
                sb.AppendLine();
                int totalHw = 0;
                int totalSensors = 0;
                int totalFanSensors = 0;
                int totalControlSensors = 0;

                foreach (var hardware in computer.Hardware)
                {
                    totalHw++;
                    sb.AppendLine($"[Hardware] {hardware.HardwareType} :: {hardware.Name}");
                    sb.AppendLine($"  Identifier: {hardware.Identifier}");
                    int hwFanCount = 0;
                    foreach (var sensor in hardware.Sensors)
                    {
                        totalSensors++;
                        if (sensor.SensorType == SensorType.Fan) { totalFanSensors++; hwFanCount++; }
                        if (sensor.SensorType == SensorType.Control) totalControlSensors++;

                        var val = sensor.Value.HasValue ? sensor.Value.Value.ToString("F2") : "null";
                        sb.AppendLine($"    [{sensor.SensorType}] {sensor.Name} = {val}  (idx={sensor.Index}, id={sensor.Identifier})");
                    }

                    foreach (var subHw in hardware.SubHardware)
                    {
                        totalHw++;
                        sb.AppendLine($"  [SubHardware] {subHw.HardwareType} :: {subHw.Name}");
                        sb.AppendLine($"    Identifier: {subHw.Identifier}");
                        foreach (var sensor in subHw.Sensors)
                        {
                            totalSensors++;
                            if (sensor.SensorType == SensorType.Fan) { totalFanSensors++; hwFanCount++; }
                            if (sensor.SensorType == SensorType.Control) totalControlSensors++;

                            var val = sensor.Value.HasValue ? sensor.Value.Value.ToString("F2") : "null";
                            sb.AppendLine($"      [{sensor.SensorType}] {sensor.Name} = {val}  (idx={sensor.Index}, id={sensor.Identifier})");
                        }
                    }

                    sb.AppendLine();
                }

                sb.AppendLine(new string('-', 72));
                sb.AppendLine($"Summary: {totalHw} hardware nodes, {totalSensors} sensors");
                sb.AppendLine($"  SensorType.Fan:     {totalFanSensors}");
                sb.AppendLine($"  SensorType.Control: {totalControlSensors}");

                bool motherboardFound = false;
                foreach (var hardware in computer.Hardware)
                    if (hardware.HardwareType == HardwareType.Motherboard) { motherboardFound = true; break; }
                sb.AppendLine($"  Motherboard hardware detected: {motherboardFound}");

                if (!motherboardFound)
                {
                    sb.AppendLine();
                    sb.AppendLine("WARNING: LibreHardwareMonitor did not detect any Motherboard hardware.");
                    sb.AppendLine("This usually means one of:");
                    sb.AppendLine("  1. Motherboard model is not in LHM's database.");
                    sb.AppendLine("  2. SuperIO chip is known but the exact board variant is not mapped.");
                    sb.AppendLine("  3. WinRing0 driver failed to load (check Windows Event Viewer).");
                    sb.AppendLine("Workaround: upgrade LibreHardwareMonitorLib to a newer version,");
                    sb.AppendLine("or file an issue at github.com/LibreHardwareMonitor/LibreHardwareMonitor");
                    sb.AppendLine("attaching this report.");
                }

                sb.AppendLine();
                sb.AppendLine();

                // Section 2: LHM's native report (rich detail — SuperIO registers, BIOS info)
                sb.AppendLine("==== LibreHardwareMonitor native report ====");
                sb.AppendLine();
                try { sb.Append(computer.GetReport()); }
                catch (Exception ex) { sb.AppendLine($"  GetReport() failed: {ex}"); }

                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                LastReportPath = path;
                Logger.Info($"Hardware diagnostic report written: {path}");
                return path;
            }
            catch (Exception ex)
            {
                Logger.Error("HardwareDiagnostics.DumpReport failed", ex);
                return null;
            }
        }

        private static bool IsAdmin()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }
    }
}
