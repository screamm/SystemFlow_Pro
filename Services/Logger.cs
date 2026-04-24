using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SystemMonitorApp.Services
{
    /// <summary>
    /// Simple file-based logger that writes to %APPDATA%\SystemFlow Pro\logs.
    /// Thread-safe via background queue. Rotates when file exceeds 5 MB, keeps last 5 files.
    /// </summary>
    public static class Logger
    {
        private static readonly BlockingCollection<string> _queue = new(new ConcurrentQueue<string>());
        private static readonly string _logDirectory;
        private static readonly object _rotationLock = new();
        private static readonly CancellationTokenSource _cts = new();
        private static volatile bool _initialized;

        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        private const int MaxRotatedFiles = 5;

        static Logger()
        {
            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SystemFlow Pro",
                "logs");

            try
            {
                Directory.CreateDirectory(_logDirectory);
                Task.Factory.StartNew(ProcessQueue, _cts.Token,
                    TaskCreationOptions.LongRunning, TaskScheduler.Default);
                _initialized = true;
            }
            catch
            {
                // If logger fails to init, swallow — never crash the host app over logging.
                _initialized = false;
            }
        }

        public static void Info(string message) => Enqueue("INFO", message, null);
        public static void Warn(string message, Exception? ex = null) => Enqueue("WARN", message, ex);
        public static void Error(string message, Exception? ex = null) => Enqueue("ERROR", message, ex);

        public static void Flush()
        {
            // Allow queue to drain — best effort.
            for (int i = 0; i < 50 && _queue.Count > 0; i++)
                Thread.Sleep(20);
        }

        public static void Shutdown()
        {
            _queue.CompleteAdding();
            _cts.Cancel();
        }

        private static void Enqueue(string level, string message, Exception? ex)
        {
            if (!_initialized) return;
            try
            {
                var sb = new StringBuilder(256);
                sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                sb.Append(" [").Append(level).Append("] ");
                sb.Append(message);
                if (ex != null)
                {
                    sb.AppendLine();
                    sb.Append("  ").Append(ex.GetType().Name).Append(": ").Append(ex.Message);
                    if (ex.StackTrace != null)
                    {
                        sb.AppendLine();
                        sb.Append(ex.StackTrace);
                    }
                }
                _queue.Add(sb.ToString());
            }
            catch
            {
                // Queue closed or other — swallow.
            }
        }

        private static void ProcessQueue()
        {
            foreach (var line in _queue.GetConsumingEnumerable(_cts.Token))
            {
                try
                {
                    var path = GetCurrentLogPath();
                    RotateIfNeeded(path);
                    File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // Never throw from logger thread.
                }
            }
        }

        private static string GetCurrentLogPath()
            => Path.Combine(_logDirectory, $"app-{DateTime.Now:yyyy-MM-dd}.log");

        private static void RotateIfNeeded(string path)
        {
            if (!File.Exists(path)) return;
            var info = new FileInfo(path);
            if (info.Length < MaxFileSizeBytes) return;

            lock (_rotationLock)
            {
                info.Refresh();
                if (info.Length < MaxFileSizeBytes) return;

                var timestamp = DateTime.Now.ToString("HHmmss");
                var rotatedPath = Path.Combine(_logDirectory,
                    $"app-{DateTime.Now:yyyy-MM-dd}-{timestamp}.log");

                try
                {
                    File.Move(path, rotatedPath);
                    TrimOldFiles();
                }
                catch
                {
                    // If rotate fails, keep appending — full file is better than crash.
                }
            }
        }

        private static void TrimOldFiles()
        {
            try
            {
                var files = new DirectoryInfo(_logDirectory).GetFiles("app-*.log");
                if (files.Length <= MaxRotatedFiles) return;

                Array.Sort(files, (a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
                for (int i = MaxRotatedFiles; i < files.Length; i++)
                {
                    try { files[i].Delete(); } catch { /* ignore */ }
                }
            }
            catch
            {
                // Never throw.
            }
        }
    }
}
