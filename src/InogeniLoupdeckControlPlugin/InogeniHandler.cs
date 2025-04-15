

namespace Loupedeck.InogeniLoupdeckControlPlugin
{
    using System;
    using System.ComponentModel;
    using System.IO.Ports;
    using System.Text.RegularExpressions;

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
            this.serialBridge = new(executeableSerialBridge, tty, 9600);
            this.serialBridge.RegisterRXHandlerCallback(this.OnMessageReceive);
            this.serialBridge.Start();
           
        }


        public void Disconnect()
        {
            PluginLog.Verbose("[InogeniHandler] Disconnect ");
            this.serialBridge.Stop();
        }


        public void initCommands()
        {
            this.SendMessage("RST");

        }


        private void SendMessage(String msg)
        {
            this.serialBridge.Send(msg);

        }




        public void RegisterPC1EventCallback(Action<States> cb) => this._pc1callback = cb;
        public void RegisterPC2EventCallback(Action<States> cb) => this._pc2callback = cb;



        public void OnMessageReceive(String msg, Boolean isOpen)
        {

            if (msg == null)
            {
                return;
            }

            if(msg.Equals(""))
            {
                return;
            }

            PluginLog.Verbose($"[InogeniHandler] is open = {isOpen}, got message {msg}");

            if (isOpen)
            {
                if (Regex.IsMatch(msg, @"^\s*EVT:\s*HOST_1.*", RegexOptions.IgnoreCase))
                {
                    this.pc1state = States.Active;
                    this.pc2state = States.Inactive; //  FIXME need to check if it is connected


                }
                else if(Regex.IsMatch(msg, @"^\s*EVT:\s*HOST_2.*", RegexOptions.IgnoreCase))
                {
                    this.pc1state = States.Inactive; //  FIXME need to check if it is connected
                    this.pc2state = States.Active; 
                }
                else
                {
                    this.pc1state = States.PcUnavailable;
                    this.pc2state = States.PcUnavailable;
                }

            }
            else
            {
                this.pc1state = States.NoSerial;
                this.pc2state = States.NoSerial;
            }
            this.InformStateChange();
        }  

        public void InformStateChange() {
            this._pc1callback?.Invoke(this.pc1state);
            this._pc2callback?.Invoke(this.pc2state);

        }

        public void TrySetPC1() {


            PluginLog.Verbose($"[InogeniHandler] try to set PC1");
                        this.SendMessage("SH 1");
          
        }

        public void TrySetPC2()
        {
            PluginLog.Verbose($"[InogeniHandler] try to set PC2");

            this.SendMessage("SH 2");

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

