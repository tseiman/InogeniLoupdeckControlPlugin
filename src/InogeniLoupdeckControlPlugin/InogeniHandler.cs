

namespace Loupedeck.InogeniLoupdeckControlPlugin
{
    using System;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    using Loupedeck.InogeniLoupdeckControlPlugin.Helpers;


    public class InogeniHandler
    {
        public enum States {
            NoSerial,
            PcUnavailable,
            Inactive,
            Active
        }


        public String SerialDevice { set; get; }

        public States pc1state { get; set; } = States.NoSerial;
        public States pc2state { get; set; } = States.NoSerial;

        public Boolean IsConnected { get; private set; }

        private Action<States> _pc1callback;
        private Action<States> _pc2callback;
        private SerialBridge serialBridge;
        private Timer _pollTimer;
        private readonly Object _stateLock = new();
        private Boolean _isStopping;

        //       private SerialBridge serialBridge;

        public InogeniHandler() {


        }


        /*    private void OnConnectionStatusChanged(Object sender, ConnectionStatusChangedEventArgs args)
            {
                PluginLog.Verbose($"[InogeniHandler] Connected = {args.Connected}");
                this.IsConnected = args.Connected;
            }
        */







        public void Connect(String tty) {
            var executeableSerialBridge = Path.Combine(InogeniLoupdeckControlPlugin.PluginPath, "serial_service");
            if (! File.Exists(executeableSerialBridge))
            {
                PluginLog.Error($"[InogeniHandler] executable for serial bridge not found after all: {executeableSerialBridge}");
                return;
            }
            this.Disconnect();
            this._isStopping = false;
            this.SetStates(States.NoSerial, States.NoSerial);
            this.serialBridge = new(executeableSerialBridge, tty, 9600);
            this.serialBridge.RegisterRXHandlerCallback(this.OnMessageReceive);
            this.serialBridge.Start();
            this.StartPolling();

        }


        public void Disconnect()
        {
            PluginLog.Verbose("[InogeniHandler] Disconnect ");
            this._isStopping = true;
            this._pollTimer?.Dispose();
            this._pollTimer = null;
            this.serialBridge?.Stop();
            this.serialBridge = null;
        }


        public void initCommands()
        {
            this.SendMessage("SEV 1");
            this.RequestStatus();
        }


        private void SendMessage(String msg)
        {
            this.serialBridge?.Send(msg);

        }

        private void StartPolling()
        {
            this._pollTimer?.Dispose();
            this._pollTimer = new Timer(_ => this.RequestStatus(), null, TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2));
        }

        private void RequestStatus()
        {
            if (this.serialBridge?.IsOpen() == true)
            {
                this.SendMessage("GH");
            }
        }

        private async void RequestStatusDelayed()
        {
            await Task.Delay(250);
            this.RequestStatus();
        }




        public void RegisterPC1EventCallback(Action<States> cb) => this._pc1callback = cb;
        public void RegisterPC2EventCallback(Action<States> cb) => this._pc2callback = cb;



        public void OnMessageReceive(String msg, Boolean isOpen)
        {
            if (this._isStopping)
            {
                return;
            }

            if (!isOpen)
            {
                this.SetStates(States.NoSerial, States.NoSerial);
                return;
            }

            if (msg == null)
            {
                return;
            }

            var cleanMsg = msg.Trim();

            if(cleanMsg.Equals(""))
            {
                return;
            }

            PluginLog.Verbose($"[InogeniHandler] is open = {isOpen}, got message {cleanMsg}");

            if (cleanMsg.Equals("__SERIAL_OPEN__", StringComparison.OrdinalIgnoreCase))
            {
                this.initCommands();
                return;
            }

            if (cleanMsg.Equals("ACK", StringComparison.OrdinalIgnoreCase) || cleanMsg.EndsWith(" ACK", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (cleanMsg.Equals("NACK", StringComparison.OrdinalIgnoreCase) || cleanMsg.EndsWith(" NACK", StringComparison.OrdinalIgnoreCase))
            {
                PluginLog.Warning($"[InogeniHandler] INOGENI rejected command: {cleanMsg}");
                return;
            }

            if (Regex.IsMatch(cleanMsg, @"^\s*EVT:", RegexOptions.IgnoreCase))
            {
                this.RequestStatusDelayed();
                return;
            }

            var selectedPcMatch = Regex.Match(cleanMsg, @"^\s*GH\s*(?<pc>[012])\s*$", RegexOptions.IgnoreCase);
            if (!selectedPcMatch.Success)
            {
                selectedPcMatch = Regex.Match(cleanMsg, @"^\s*(?<pc>[012])\s*$");
            }

            if (selectedPcMatch.Success)
            {
                this.UpdateSelectedPcState(Int32.Parse(selectedPcMatch.Groups["pc"].Value));
                return;
            }

            PluginLog.Verbose($"[InogeniHandler] ignoring serial response: {cleanMsg}");
        }

        private void UpdateSelectedPcState(Int32 selectedPc)
        {
            switch (selectedPc)
            {
                case 0:
                    this.SetStates(States.Inactive, States.Inactive);
                    break;
                case 1:
                    this.SetStates(States.Active, States.Inactive);
                    break;
                case 2:
                    this.SetStates(States.Inactive, States.Active);
                    break;
                default:
                    this.SetStates(States.PcUnavailable, States.PcUnavailable);
                    break;
            }
        }

        private void SetStates(States pc1State, States pc2State)
        {
            var changed = false;
            lock (this._stateLock)
            {
                changed = this.pc1state != pc1State || this.pc2state != pc2State;
                this.pc1state = pc1State;
                this.pc2state = pc2State;
            }

            if (changed)
            {
                this.InformStateChange();
            }
        }

        public void InformStateChange() {
            this._pc1callback?.Invoke(this.pc1state);
            this._pc2callback?.Invoke(this.pc2state);

        }

        public void TrySetPC1() {


            PluginLog.Verbose($"[InogeniHandler] try to set PC1");
            this.SendMessage("SH 1");
            this.RequestStatusDelayed();

        }

        public void TrySetPC2()
        {
            PluginLog.Verbose($"[InogeniHandler] try to set PC2");

            this.SendMessage("SH 2");
            this.RequestStatusDelayed();

        }




        /*
        private void OnSerialPortOpenChange(Boolean newValue)
        {
            PluginLog.Verbose($"[InogeniHandler] OnSerialPortOpenChange {newValue}");

            if (newValue)
            {
                this.initCommands();
            }
        }
        */







    }
}

