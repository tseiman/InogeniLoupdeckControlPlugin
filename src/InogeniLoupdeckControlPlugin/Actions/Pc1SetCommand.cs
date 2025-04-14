namespace Loupedeck.InogeniLoupdeckControlPlugin
{
    using System;

    using Loupedeck.InogeniLoupdeckControlPlugin.Helpers;


    using static Loupedeck.InogeniLoupdeckControlPlugin.InogeniHandler;



    // This class implements an example adjustment that counts the rotation ticks of a dial.


          public class Pc1SetCommand : PluginMultistateDynamicCommand
    {

        private InogeniLoupdeckControlPlugin _plugin => (InogeniLoupdeckControlPlugin)this.Plugin;

        public String PC1Name { get; private set; } = "";
        public String UARTDevice { get; private set; } = "";

        private readonly InogeniHandler _inogeniHandler = InogeniLoupdeckControlPlugin.InogeniHandler;

        private const String DEVICENAME = "PC1 ";
        
       

        //     private String _currentState = "";

        private BitmapImage _image;

        // Initializes the command class.
        public Pc1SetCommand()
               : base(groupName: "Misc", displayName: "Toggle Preview Transition", description: "The transition can be previewed in ATEM preview screen")
        {
            this.IsWidget = true;


   //         this.AddState("IMPOSSIBLE", "IMPOSSIBLE", "IMPOSSIBLE");
            this.AddState("NOSERIAL", "USB Switch " + DEVICENAME + "No Serial", "USB Switch " + DEVICENAME + "No Serial");
            this.AddState("PCINACTIVE", "USB Switch " + DEVICENAME + "inactive", "USB Switch " + DEVICENAME + "inactive");
            this.AddState("ACTIVE", "USB Switch " + DEVICENAME + "active", "USB Switch " + DEVICENAME + "active");
            this.AddState("INACTIVE", "USB Switch " + DEVICENAME + "inactive", "USB Switch " + DEVICENAME + "inactive");

            this.MakeProfileAction("text;Enter PC name and serial device separated by a \";\":");



        }



        protected override Boolean OnLoad()
        {
            var x = base.OnLoad();
            this.SetCurrentState("", 0);


            if (this._plugin.TryGetPluginSetting("PC1Name", out var PC1Name))
            {
                PluginLog.Info($"[Pc1SetCommand] Loading config AtemURI: <{PC1Name}>");
                this.PC1Name = PC1Name;
            }
            else
            {
                PluginLog.Warning($"[Pc1SetCommand] NOT Loading config PC1Name");
            }

            if (this._plugin.TryGetPluginSetting("UARTDevice", out var UARTDevice))
            {
                PluginLog.Info($"[Pc1SetCommand] Loading config AtemURI: <{UARTDevice}>");
                this.UARTDevice = UARTDevice;
            }
            else
            {
                PluginLog.Warning($"[Pc1SetCommand] NOT Loading config PC1Name");
            }


            return x;
        }


        protected override Boolean OnUnload()
        {



            return base.OnUnload();
           
        }



        // This method is called when the user executes the command.
        protected override void RunCommand(String actionParameter)
        {
            PluginLog.Verbose($"[Pc1SetCommand] RunCommand {this.GetCurrentState(actionParameter).Name}//{actionParameter}");


            
            var actionParams = actionParameter.Split(';');

            if (actionParams.Length > 1)
            {
                if ((! this.PC1Name.Equals(actionParams[0]) ) || (!this.UARTDevice.Equals(actionParams[1])))
                {

                    PluginLog.Verbose($"[Pc1SetCommand] RunCommand  actionParams {actionParams[0]}//{actionParams[1]}");

                    if (!actionParams[0].Equals(""))
                    {
                        this._plugin.SetPluginSetting("PC1Name", actionParams[0], false);
                        PluginLog.Info($"[Pc1SetCommand] Storing config PC1Name: <{actionParams[0]}>");

                    }

                    if (!actionParams[1].Equals(""))
                    {
                        this._plugin.SetPluginSetting("UARTDevice", actionParams[1], false);
                        PluginLog.Info($"[Pc1SetCommand] Storing config UARTDevice: <{actionParams[1]}>");
                    }





                }


                this.PC1Name = actionParams[0];
                this.UARTDevice = actionParams[1];
                PluginLog.Verbose($"[Pc1SetCommand] RunCommand setting PC1Name: {this.PC1Name}// UARTDevice: {this.UARTDevice}");
            }



            this._inogeniHandler.setPC1State();
            this.SetCurrentState(actionParameter, Array.IndexOf(Enum.GetValues(typeof(States)), this._inogeniHandler.pc1state));


            this.ActionImageChanged();
        }


        protected override BitmapImage GetCommandImage(String actionParameter, Int32 stateIndex, PluginImageSize imageSize)
        {
            PluginLog.Verbose($"[Pc1SetCommand] GetCommandImage {actionParameter}//{this.GetCurrentState(actionParameter).Name}");

           
            using (var bitmapBuilder = new BitmapBuilder(imageSize))
            {


          
                switch (this._inogeniHandler.pc1state)
                {
                    case InogeniHandler.States.PcUnavailable:
                        bitmapBuilder.FillRectangle(0, 0, imageSize.GetWidth(), imageSize.GetHeight(), Colors.DARKGREY);
                        bitmapBuilder.DrawText($"{this.PC1Name} not availabe", Colors.GREY);
                        break;
                    case InogeniHandler.States.Inactive:
                        bitmapBuilder.FillRectangle(0, 0, imageSize.GetWidth(), imageSize.GetHeight(), Colors.LIGHTGREY);
                        bitmapBuilder.DrawText($"{this.PC1Name} inactive", BitmapColor.Black);
                        break;
                    case InogeniHandler.States.Active:
                        bitmapBuilder.FillRectangle(0, 0, imageSize.GetWidth(), imageSize.GetHeight(), BitmapColor.Blue);
                        bitmapBuilder.DrawText($"{this.PC1Name} active", BitmapColor.White);
                        break;
                    default:
                        bitmapBuilder.FillRectangle(0, 0, imageSize.GetWidth(), imageSize.GetHeight(), Colors.GREY);
                        bitmapBuilder.DrawLine(0, 0, imageSize.GetWidth(), imageSize.GetHeight(), BitmapColor.Red, 2);
                        bitmapBuilder.DrawLine(0, imageSize.GetWidth(), imageSize.GetHeight(), 0, BitmapColor.Red, 2);
                        bitmapBuilder.DrawText($"USB Switch not available", BitmapColor.Black);
                        break;
                }


                this._image = bitmapBuilder.ToImage();
                return this._image;

            }
        }


        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
//            PluginLog.Verbose($"[MacroPlayCommand] GetCommandDisplayName {this.GetCurrentState(actionParameter).Name},  {this.GetCurrentState(actionParameter).Description},  {this.GetCurrentState(actionParameter).DisplayName}");
            return  this.GetCurrentState(actionParameter).DisplayName;

            // return "aaaaa";

        }


    }




}

