namespace Loupedeck.InogeniLoupdeckControlPlugin
{
    using System;

    using Loupedeck.InogeniLoupdeckControlPlugin.Helpers;

    using static Loupedeck.InogeniLoupdeckControlPlugin.InogeniHandler;


    public class Pc2SetCommand : AbstractPcSetCommand
    {
        private new const String DEVICENAME = "PC2 ";
        public override String PCName { get; set; } = "";


        // Initializes the command class.
        public Pc2SetCommand()
            : base()
        {
            this.GroupName = "";
            this.DisplayName = "USB switch select PC2";
            this.Description = "USB Switch to PC2";

            this.MakeProfileAction("text;Enter PC name:");
        }



        protected override Boolean OnLoad()
        {
            var result = base.OnLoad();
            this.SetCurrentState("", 0);

            this.PCName =  this.LoadConfigData("PC2Name");

            return result;
        }


        protected override Boolean OnUnload() => base.OnUnload();



        // This method is called when the user executes the command.
        protected override void RunCommand(String actionParameter)
        {
            PluginLog.Verbose($"[Pc2SetCommand] RunCommand {this.GetCurrentState(actionParameter).Name}//{actionParameter}");

          //  var actionParams = actionParameter.Split(';');

            this.SaveConfigData("PC2Name", this.PCName, actionParameter);

            this.PCName = actionParameter;
            PluginLog.Verbose($"[Pc2SetCommand] RunCommand setting PC2Name: {this.PCName}");

            this.InogeniHandler.setPC2State();
            this.SetCurrentState(actionParameter, Array.IndexOf(Enum.GetValues(typeof(States)), this.InogeniHandler.pc2state));

            this.ActionImageChanged();
        }

        public override States GetInogeniHandlerState() => this.InogeniHandler.pc2state;
    }

}

