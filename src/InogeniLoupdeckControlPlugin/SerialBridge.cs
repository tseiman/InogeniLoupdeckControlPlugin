
namespace Loupedeck.InogeniLoupdeckControlPlugin
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    using Loupedeck.InogeniLoupdeckControlPlugin.Helpers;

    public class SerialBridge
    {

        private Process _process;
        private readonly String _binaryPath;
        private readonly String _port;
        private readonly Int32 _baudRate;
        private readonly Object _sendLock = new();
        private readonly Object _processLock = new();
        private Boolean _stopRequested;


        private Action<String, Boolean> _handlerRxCallback;

        public SerialBridge(String binaryPath, String port, Int32 baudRate)
        {
            this._binaryPath = binaryPath;
            this._port = port;
            this._baudRate = baudRate;
        }



        public void Start()
        {
            lock (this._processLock)
            {
                if (this._process != null && !this._process.HasExited)
                {
                    PluginLog.Warning($"[SerialBridge] Start ignored because serial service is already running with pid {this._process.Id}");
                    return;
                }

                this.CleanupOrphanedBridgeProcesses();

                this._stopRequested = false;
                this._process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _binaryPath,
                        Arguments = $"-d {this._port} -b {this._baudRate}",
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    },
                    EnableRaisingEvents = true
                };

                this._process.Exited += this.OnProcessExited;
                this._process.Start();
                PluginLog.Verbose($"[SerialBridge] Serial service started with pid {this._process.Id}");

                this._handlerRxCallback?.Invoke("__SERIAL_OPEN__", true);
                Task.Run(() => this.ReadFromSerial(this._process));
            }

        }

        private async void OnProcessExited(Object sender, EventArgs args)
        {
            if (this._stopRequested)
            {
                return;
            }

            PluginLog.Info("[SerialBridge] Serial service exited. Restarting...");

            this._handlerRxCallback?.Invoke("Connection closed", false);

            await Task.Delay(500); // avoid hot loop
            this.Start();
        }



        private void ReadFromSerial(Process process)
        {
            if (process == null || process.StandardOutput == null)
            {
                return;
            }

            var reader = process.StandardOutput;
            while (!process.HasExited)
            {
                var line = reader.ReadLine();
                if (line != null)
                {
                    if (this.ShouldLogSerialLine(line))
                    {
                        PluginLog.Verbose($"[SerialBridge] {line}");
                    }

                    this._handlerRxCallback?.Invoke(line, true);
                }
            }
        }

        private Boolean ShouldLogSerialLine(String line)
        {
#if DEBUG
            return true;
#else
            var cleanLine = line?.Trim();
            if (String.IsNullOrEmpty(cleanLine))
            {
                return false;
            }

            if (cleanLine.Equals("ACK", StringComparison.OrdinalIgnoreCase)
                || cleanLine.Equals("GH", StringComparison.OrdinalIgnoreCase)
                || cleanLine.Equals("RST", StringComparison.OrdinalIgnoreCase)
                || cleanLine.Equals("0", StringComparison.OrdinalIgnoreCase)
                || cleanLine.Equals("1", StringComparison.OrdinalIgnoreCase)
                || cleanLine.Equals("2", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
#endif
        }

        public void Send(String data)
        {
            if (this._process?.StandardInput != null && !this._process.HasExited)
            {
                try
                {
                    lock (this._sendLock)
                    {
                        this._process.StandardInput.Write($"{data}\r");
                        this._process.StandardInput.Flush();
                    }
                }
                catch (Exception e)
                {
                    PluginLog.Warning($"[SerialBridge] Failed to send serial command: {e.Message}");
                    this._handlerRxCallback?.Invoke("Connection closed", false);
                }
            }
        }

        public Boolean IsOpen() => this._process != null && !this._process.HasExited && this._process.StandardInput != null;


        public void Stop()
        {
            PluginLog.Verbose("[SerialBridge] Stop ");
            this._stopRequested = true;

            Process process;
            lock (this._processLock)
            {
                process = this._process;
                this._process = null;
            }

            if (process == null)
            {
                this.CleanupOrphanedBridgeProcesses();
                return;
            }

            process.Exited -= this.OnProcessExited;
            this.KillProcess(process, "tracked serial service");
            process.Dispose();
            this.CleanupOrphanedBridgeProcesses();
        }

        private void KillProcess(Process process, String description)
        {
            if (process == null)
            {
                return;
            }

            try
            {
                if (process.HasExited)
                {
                    PluginLog.Verbose($"[SerialBridge] {description} already stopped");
                    return;
                }

                var pid = process.Id;
                PluginLog.Verbose($"[SerialBridge] Stopping {description} pid {pid}");

                try
                {
                    process.StandardInput?.Close();
                }
                catch (Exception)
                {
                    // Closing stdin is a best-effort hint before terminating the process.
                }

                this.SendSignal(pid, "-TERM");
                process.WaitForExit(2000);

                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5000);
                }

                if (process.HasExited)
                {
                    PluginLog.Verbose($"[SerialBridge] {description} stopped");
                }
                else
                {
                    PluginLog.Error($"[SerialBridge] Not able to kill {description} pid {pid}");
                    this._handlerRxCallback?.Invoke("Connection closed", false);
                }
            }
            catch (Exception e)
            {
                PluginLog.Error($"[SerialBridge] Failed to stop {description}: {e}");
                this._handlerRxCallback?.Invoke("Connection closed", false);
            }
        }

        private void SendSignal(Int32 pid, String signal)
        {
            try
            {
                var killProc = Process.Start(new ProcessStartInfo
                {
                    FileName = "/bin/kill",
                    Arguments = $"{signal} {pid}",
                    UseShellExecute = false
                });
                killProc?.WaitForExit(2000);
            }
            catch (Exception e)
            {
                PluginLog.Warning($"[SerialBridge] Failed to send {signal} to pid {pid}: {e.Message}");
            }
        }

        private void CleanupOrphanedBridgeProcesses()
        {
            if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux())
            {
                return;
            }

            try
            {
                var ps = Process.Start(new ProcessStartInfo
                {
                    FileName = "/bin/ps",
                    Arguments = "-axo pid=,ppid=,command=",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                });

                if (ps == null)
                {
                    return;
                }

                var output = ps.StandardOutput.ReadToEnd();
                ps.WaitForExit(2000);

                foreach (var rawLine in output.Split('\n'))
                {
                    var line = rawLine.Trim();
                    if (String.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    var parts = line.Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 3
                        || !Int32.TryParse(parts[0], out var pid)
                        || !Int32.TryParse(parts[1], out var parentPid))
                    {
                        continue;
                    }

                    var command = parts[2];
                    if (pid == Environment.ProcessId
                        || parentPid > 1
                        || !command.Contains(Path.GetFileName(this._binaryPath), StringComparison.Ordinal)
                        || !command.Contains(this._port, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    PluginLog.Warning($"[SerialBridge] Killing orphaned serial service pid {pid}: {command}");
                    var orphan = Process.GetProcessById(pid);
                    this.KillProcess(orphan, "orphaned serial service");
                    orphan.Dispose();
                }
            }
            catch (Exception e)
            {
                PluginLog.Warning($"[SerialBridge] Failed to cleanup orphaned serial services: {e.Message}");
            }
        }

        public void RegisterRXHandlerCallback(Action<String, Boolean> cb) => this._handlerRxCallback = cb;
    }
}
