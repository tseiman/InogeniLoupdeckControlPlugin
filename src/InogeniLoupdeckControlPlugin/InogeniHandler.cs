

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
        private readonly Object _connectionLock = new();
        private String _serialBridgeExecutable;
        private String _serialDevice;
        private DateTime _lastValidStatusUtc = DateTime.MinValue;
        private DateTime _lastStatusRequestUtc = DateTime.MinValue;
        private DateTime _nextReconnectAllowedUtc = DateTime.MinValue;
        private Int32 _invalidResponseCount;
        private Int32 _statusRequestsWithoutValidResponse;
        private Int32 _consecutiveReconnects;
        private Int32 _connectionGeneration;
        private Int32 _selectedPc = -1;
        private Boolean _reconnectInProgress;
        private Boolean _serialInitializationInProgress;
        private Boolean _resetOnNextSerialOpen;
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
            this._serialBridgeExecutable = executeableSerialBridge;
            this._serialDevice = tty;
            this._lastValidStatusUtc = DateTime.MinValue;
            this._lastStatusRequestUtc = DateTime.MinValue;
            this._nextReconnectAllowedUtc = DateTime.MinValue;
            this._invalidResponseCount = 0;
            this._statusRequestsWithoutValidResponse = 0;
            this._consecutiveReconnects = 0;
            this._selectedPc = -1;
            this._serialInitializationInProgress = false;
            this._resetOnNextSerialOpen = false;
            this._connectionGeneration++;
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
            this._invalidResponseCount = 0;
            this._statusRequestsWithoutValidResponse = 0;
            this._consecutiveReconnects = 0;
            this._nextReconnectAllowedUtc = DateTime.MinValue;
            this._selectedPc = -1;
            this._serialInitializationInProgress = false;
            this._resetOnNextSerialOpen = false;
            this._connectionGeneration++;
            this.serialBridge?.Stop();
            this.serialBridge = null;
        }


        public void initCommands()
        {
            var sendReset = this._resetOnNextSerialOpen;
            this._resetOnNextSerialOpen = false;
            this.InitializeSerialConsole(this._connectionGeneration, sendReset);
        }


        private void SendMessage(String msg)
        {
            this.serialBridge?.Send(msg);

        }

        private void StartPolling()
        {
            this._pollTimer?.Dispose();
            this._pollTimer = new Timer(_ => this.PollStatusAndCheckHealth(), null, TimeSpan.FromMilliseconds(1000), TimeSpan.FromSeconds(2));
        }

        private void RequestStatus()
        {
            if (this.serialBridge?.IsOpen() == true)
            {
                var now = DateTime.UtcNow;
                if (now - this._lastStatusRequestUtc < TimeSpan.FromMilliseconds(500))
                {
                    return;
                }

                this._lastStatusRequestUtc = now;
                this._statusRequestsWithoutValidResponse++;
                this.SendMessage("GH");
            }
        }

        private async void RequestStatusDelayed(Int32 delayMs = 250)
        {
            await Task.Delay(delayMs);
            this.RequestStatus();
        }

        private async void InitializeSerialConsole(Int32 connectionGeneration, Boolean sendReset)
        {
            if (this._isStopping)
            {
                return;
            }

            this._lastStatusRequestUtc = DateTime.MinValue;
            this._invalidResponseCount = 0;
            this._statusRequestsWithoutValidResponse = 0;
            this._serialInitializationInProgress = true;

            if (sendReset)
            {
                PluginLog.Info("[InogeniHandler] Initializing INOGENI serial console with RST");
                this.SendMessage("RST");
            }
            else
            {
                PluginLog.Info("[InogeniHandler] Initializing INOGENI serial console");
            }

            await Task.Delay(sendReset ? 5000 : 1000);

            if (this._isStopping || connectionGeneration != this._connectionGeneration || this.serialBridge?.IsOpen() != true)
            {
                if (connectionGeneration == this._connectionGeneration)
                {
                    this._serialInitializationInProgress = false;
                }

                return;
            }

            this.SendMessage("SEV 1");
            await Task.Delay(800);

            if (this._isStopping || connectionGeneration != this._connectionGeneration || this.serialBridge?.IsOpen() != true)
            {
                if (connectionGeneration == this._connectionGeneration)
                {
                    this._serialInitializationInProgress = false;
                }

                return;
            }

            this._serialInitializationInProgress = false;
            this.RequestStatus();
        }

        private void PollStatusAndCheckHealth()
        {
            if (this._isStopping || this._reconnectInProgress || this._serialInitializationInProgress)
            {
                return;
            }

            if (this.serialBridge?.IsOpen() != true)
            {
                return;
            }

            if (this.ShouldReconnectBridge())
            {
                this.RestartSerialBridge("no valid INOGENI status after invalid serial responses");
                return;
            }

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

            if (this.ShouldLogSerialMessage(cleanMsg))
            {
                PluginLog.Verbose($"[InogeniHandler] is open = {isOpen}, got message {cleanMsg}");
            }

            if (cleanMsg.Equals("__SERIAL_OPEN__", StringComparison.OrdinalIgnoreCase))
            {
                this.initCommands();
                return;
            }

            if (this.IsCommandEcho(cleanMsg))
            {
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
            if (!selectedPcMatch.Success)
            {
                selectedPcMatch = Regex.Match(cleanMsg, @"^\s*G(?<pc>[012])\s*$", RegexOptions.IgnoreCase);
            }

            if (selectedPcMatch.Success)
            {
                this.UpdateSelectedPcState(Int32.Parse(selectedPcMatch.Groups["pc"].Value));
                return;
            }

            PluginLog.Verbose($"[InogeniHandler] ignoring serial response: {cleanMsg}");
            if (this._serialInitializationInProgress)
            {
                return;
            }

            this.RecordInvalidResponse(cleanMsg);
        }

        private Boolean IsCommandEcho(String msg) =>
            msg.Equals("GH", StringComparison.OrdinalIgnoreCase)
            || msg.Equals("RST", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(msg, @"^\s*SEV\s+[01]\s*$", RegexOptions.IgnoreCase)
            || Regex.IsMatch(msg, @"^\s*SH\s+[12]\s*$", RegexOptions.IgnoreCase);

        private Boolean ShouldLogSerialMessage(String msg)
        {
#if DEBUG
            return true;
#else
            return msg.Equals("__SERIAL_OPEN__", StringComparison.OrdinalIgnoreCase);
#endif
        }

        private void UpdateSelectedPcState(Int32 selectedPc)
        {
            var selectedPcChanged = this._selectedPc != selectedPc;
            this._selectedPc = selectedPc;
            this._lastValidStatusUtc = DateTime.UtcNow;
            this._invalidResponseCount = 0;
            this._statusRequestsWithoutValidResponse = 0;
            this._consecutiveReconnects = 0;
            this._nextReconnectAllowedUtc = DateTime.MinValue;
            this._serialInitializationInProgress = false;
            this._resetOnNextSerialOpen = false;
            this.IsConnected = true;

            if (selectedPcChanged)
            {
                PluginLog.Verbose($"[InogeniHandler] selected PC status changed to {selectedPc}");
            }

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

        private void RecordInvalidResponse(String response)
        {
            this._invalidResponseCount++;

            if (this.ShouldReconnectBridge())
            {
                this.RestartSerialBridge($"invalid serial response '{response}'");
            }
        }

        private Boolean ShouldReconnectBridge()
        {
            if (DateTime.UtcNow < this._nextReconnectAllowedUtc)
            {
                return false;
            }

            if (this._invalidResponseCount < 2 && this._statusRequestsWithoutValidResponse < 3)
            {
                return false;
            }

            if (this._lastValidStatusUtc == DateTime.MinValue)
            {
                return true;
            }

            return DateTime.UtcNow - this._lastValidStatusUtc > TimeSpan.FromSeconds(10);
        }

        private async void RestartSerialBridge(String reason)
        {
            lock (this._connectionLock)
            {
                if (this._reconnectInProgress || this._isStopping)
                {
                    return;
                }

                this._reconnectInProgress = true;
            }

            this._consecutiveReconnects++;
            var reconnectDelay = this.GetReconnectDelay(this._consecutiveReconnects);
            this._nextReconnectAllowedUtc = DateTime.UtcNow + reconnectDelay;
            this._resetOnNextSerialOpen = this._consecutiveReconnects == 2;

            PluginLog.Warning($"[InogeniHandler] Restarting serial bridge ({this._consecutiveReconnects}) in {reconnectDelay.TotalSeconds:0}s: {reason}");

            try
            {
                this.SetStates(States.NoSerial, States.NoSerial);
                this.serialBridge?.Stop();
                this.serialBridge = null;

                await Task.Delay(reconnectDelay);

                if (this._isStopping || String.IsNullOrEmpty(this._serialBridgeExecutable) || String.IsNullOrEmpty(this._serialDevice))
                {
                    return;
                }

                this._invalidResponseCount = 0;
                this._statusRequestsWithoutValidResponse = 0;
                this._lastStatusRequestUtc = DateTime.MinValue;
                this._lastValidStatusUtc = DateTime.MinValue;
                this._connectionGeneration++;
                this.serialBridge = new(this._serialBridgeExecutable, this._serialDevice, 9600);
                this.serialBridge.RegisterRXHandlerCallback(this.OnMessageReceive);
                this.serialBridge.Start();
            }
            catch (Exception e)
            {
                PluginLog.Error($"[InogeniHandler] Failed to restart serial bridge: {e}");
            }
            finally
            {
                this._reconnectInProgress = false;
            }
        }

        private TimeSpan GetReconnectDelay(Int32 reconnectAttempt)
        {
            switch (reconnectAttempt)
            {
                case 1:
                    return TimeSpan.FromSeconds(1);
                case 2:
                    return TimeSpan.FromSeconds(10);
                case 3:
                    return TimeSpan.FromSeconds(15);
                default:
                    return TimeSpan.FromMinutes(5);
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
