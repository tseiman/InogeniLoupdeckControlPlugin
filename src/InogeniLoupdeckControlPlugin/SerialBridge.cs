
namespace Loupedeck.InogeniLoupdeckControlPlugin
{
    using System;
    using System.Diagnostics;

    using Loupedeck.InogeniLoupdeckControlPlugin.Helpers;

    public class SerialBridge
    {

        private Process _process;
        private readonly String _binaryPath;
        private readonly String _port;
        private readonly Int32 _baudRate;

    
        private Action<String, Boolean> _handlerRxCallback;

        public SerialBridge(String binaryPath, String port, Int32 baudRate)
        {
            this._binaryPath = binaryPath;
            this._port = port;
            this._baudRate = baudRate;
        }



        public void Start()
        {
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

           

            Task.Run(() => this.ReadFromSerial());
        }

        private async void OnProcessExited(Object sender, EventArgs args)
        {

            PluginLog.Info("[SerialBridge] Serial service exited. Restarting...");
           
            this._handlerRxCallback?.Invoke("Connection closed", false);

            await Task.Delay(500); // avoid hot loop
            this.Start();
        }



        private void ReadFromSerial()
        {
            if (this._process == null || this._process.StandardOutput == null)
            {
                return;
            }

            var reader = this._process.StandardOutput;
            while (!this._process.HasExited)
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
                this._process.StandardInput.WriteLine(data);
                this._process.StandardInput.Flush();
            }
        }

        public Boolean IsOpen() => this._process == null || this._process.StandardOutput == null;
           

        public void Stop()
        {
            PluginLog.Verbose("[SerialBridge] Stop ");

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

                PluginLog.Verbose("Done Kill");
                this._handlerRxCallback?.Invoke("Connection closed", false);
            } else {
                PluginLog.Error("Not able to Kill");
            }
        }


        public void RegisterRXHandlerCallback(Action<String, Boolean> cb) => this._handlerRxCallback = cb;
    }
}

