
namespace Loupedeck.InogeniLoupdeckControlPlugin
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using Loupedeck.InogeniLoupdeckControlPlugin.Helpers;

    public class SerialBridge
    {

        private Process _process;
        private readonly String _binaryPath;
        private readonly String _port;
        private readonly Int32 _baudRate;
        private readonly Object _sendLock = new();
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



            this._handlerRxCallback?.Invoke("__SERIAL_OPEN__", true);
            Task.Run(() => this.ReadFromSerial(this._process));
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
                    PluginLog.Verbose($"[SerialBridge] {line}");
                    this._handlerRxCallback?.Invoke(line, true);
                }
            }
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

            if (this._process == null)
            {
                return;
            }

            this._process.Exited -= this.OnProcessExited;


            if (this._process != null && !this._process.HasExited)
            {

                PluginLog.Verbose("Kill 1");

                this._process.Kill();
                this._process.WaitForExit(10000);

            }

            if (this._process != null && !this._process.HasExited)
            {

                PluginLog.Verbose("Kill 2");


                var killProc = new ProcessStartInfo
                {
                    FileName = "/bin/kill",
                    Arguments = $"-2 {this._process.Id}",
                    UseShellExecute = false
                };
                Process.Start(killProc).WaitForExit(10000);
            }


            if (this._process != null && !this._process.HasExited)
            {

                PluginLog.Verbose("Kill 2");

                var killProc = new ProcessStartInfo
                {
                    FileName = "/usr/bin/killalll",
                    Arguments = $"serial_service",
                    UseShellExecute = false
                };

                Process.Start(killProc).WaitForExit(10000);
            }


            if (this._process != null && !this._process.HasExited)
            {

                PluginLog.Error("[SerialBridge] Not able to kill serial service");
                this._handlerRxCallback?.Invoke("Connection closed", false);
            } else {
                PluginLog.Verbose("[SerialBridge] Serial service stopped");
            }
        }


        public void RegisterRXHandlerCallback(Action<String, Boolean> cb) => this._handlerRxCallback = cb;
    }
}
