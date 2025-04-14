namespace Loupedeck.InogeniLoupdeckControlPlugin
{
    using System;

    using Loupedeck.InogeniLoupdeckControlPlugin.Helpers;

    using static Loupedeck.InogeniLoupdeckControlPlugin.InogeniHandler;


    public class Pc1SetCommand : AbstractPcSetCommand
    {
        private new const String DEVICENAME = "PC1 ";
        public String UARTDevice { get; private set; } = "";
        public override String PCName { get; set; } = "";


        // Initializes the command class.
        public Pc1SetCommand()
            : base()
        {
            this.GroupName = "";
            this.DisplayName = "USB switch select PC1";
            this.Description = "USB Switch to PC1 (use this first as it configures the serial port)";
        
            this.MakeProfileAction("text;Enter PC name and serial device separated by a \";\":");
        }



        protected override Boolean OnLoad()
        {
            var result = base.OnLoad();
            this.SetCurrentState("", 0);

            this.PCName =  this.LoadConfigData("PC1Name");
            this.UARTDevice = this.LoadConfigData("UARTDevice");

            return result;
        }


        protected override Boolean OnUnload() => base.OnUnload();



        // This method is called when the user executes the command.
        protected override void RunCommand(String actionParameter)
        {
            PluginLog.Verbose($"[Pc1SetCommand] RunCommand {this.GetCurrentState(actionParameter).Name}//{actionParameter}");

            var actionParams = actionParameter.Split(';');

            if (actionParams.Length > 1)
            {
                this.SaveConfigData("PC1Name", this.PCName, actionParams[0]);
                this.SaveConfigData("UARTDevice", this.UARTDevice, actionParams[1]);

                this.PCName = actionParams[0];
                this.UARTDevice = actionParams[1];
                PluginLog.Verbose($"[Pc1SetCommand] RunCommand setting PC1Name: {this.PCName}// UARTDevice: {this.UARTDevice}");
            }

            this.InogeniHandler.setPC1State();
            this.SetCurrentState(actionParameter, Array.IndexOf(Enum.GetValues(typeof(States)), this.InogeniHandler.pc1state));

            this.ActionImageChanged();
        }

        public override States GetInogeniHandlerState() => this.InogeniHandler.pc1state;
    }

}

