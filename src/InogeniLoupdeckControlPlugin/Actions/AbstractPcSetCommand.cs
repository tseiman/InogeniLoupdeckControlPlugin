namespace Loupedeck.InogeniLoupdeckControlPlugin
{
    using System;

    using Loupedeck.InogeniLoupdeckControlPlugin.Helpers;


    using static Loupedeck.InogeniLoupdeckControlPlugin.InogeniHandler;



    // This class implements an example adjustment that counts the rotation ticks of a dial.


          public abstract class AbstractPcSetCommand : PluginMultistateDynamicCommand
    {

        protected InogeniLoupdeckControlPlugin _plugin => (InogeniLoupdeckControlPlugin)this.Plugin;

        public abstract String PCName { get;  set; }

        protected readonly InogeniHandler InogeniHandler = InogeniLoupdeckControlPlugin.InogeniHandler;
        protected const String DEVICENAME = "NOTSET ";

        protected BitmapImage _image;


        public AbstractPcSetCommand() {

            this.IsWidget = true;


            this.AddState("NOSERIAL", "USB Switch " + DEVICENAME + "No Serial", "USB Switch " + DEVICENAME + "No Serial");
            this.AddState("PCINACTIVE", "USB Switch " + DEVICENAME + "inactive", "USB Switch " + DEVICENAME + "inactive");
            this.AddState("ACTIVE", "USB Switch " + DEVICENAME + "active", "USB Switch " + DEVICENAME + "active");
            this.AddState("INACTIVE", "USB Switch " + DEVICENAME + "inactive", "USB Switch " + DEVICENAME + "inactive");

        }


        public abstract States GetInogeniHandlerState();

        protected override BitmapImage GetCommandImage(String actionParameter, Int32 stateIndex, PluginImageSize imageSize)
        {
         //   PluginLog.Verbose($"[{this.GetType().Name}] GetCommandImage {this.GetInogeniHandlerState()}//{this.GetCurrentState(actionParameter).Name}");

           
            using (var bitmapBuilder = new BitmapBuilder(imageSize))
            {


          
                switch (this.GetInogeniHandlerState())
                {
                    case InogeniHandler.States.PcUnavailable:
                        bitmapBuilder.FillRectangle(0, 0, imageSize.GetWidth(), imageSize.GetHeight(), Colors.DARKGREY);
                        bitmapBuilder.DrawText($"{this.PCName} not availabe", Colors.GREY);
                        break;
                    case InogeniHandler.States.Inactive:
                        bitmapBuilder.FillRectangle(0, 0, imageSize.GetWidth(), imageSize.GetHeight(), Colors.LIGHTGREY);
                        bitmapBuilder.DrawText($"{this.PCName}", BitmapColor.Black);
                        break;
                    case InogeniHandler.States.Active:
                        bitmapBuilder.FillRectangle(0, 0, imageSize.GetWidth(), imageSize.GetHeight(), BitmapColor.Blue);
                        bitmapBuilder.DrawText($"{this.PCName} active", BitmapColor.White);
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


        protected String LoadConfigData(String dataId) {

            if (this._plugin.TryGetPluginSetting(dataId, out var dataValue))
            {
                PluginLog.Info($"[{this.GetType().Name}] Loading config {dataId}: <{dataValue}>");
                return dataValue;
            }
            else
            {
                PluginLog.Warning($"[{this.GetType().Name}] NOT Loading config PC1Name");
                return "";
            }
        }

        protected Boolean SaveConfigData(String dataId, String oldDataToCompare, String newData) {

            var result = false;

            if (!oldDataToCompare.Equals(newData)) {

                if (!newData.Equals(""))
                {
                    this._plugin.SetPluginSetting(dataId, newData, false);
                    PluginLog.Info($"[{this.GetType().Name}] Storing config {dataId}: <{newData}>");
                    result = true;
                }
            }

            return result;
        }


        public abstract void HandleStateChange(States state);

        //   protected override String GetCommandDisplayName(String actionParameter, Int32 stateIndex, PluginImageSize imageSize) => this.GetCurrentState(actionParameter).DisplayName;


    }




}

