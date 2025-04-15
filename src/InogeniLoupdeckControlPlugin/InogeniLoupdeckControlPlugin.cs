namespace Loupedeck.InogeniLoupdeckControlPlugin
{
    using System;

    using Loupedeck.InogeniLoupdeckControlPlugin.Helpers;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class InogeniLoupdeckControlPlugin : Plugin
    {
        // Gets a value indicating whether this is an Universal plugin or an Application plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean HasNoApplication => true;

        public static InogeniHandler InogeniHandler { get; } = new();
        public static String PluginPath { get; private set; }


        public static event Action PluginReady;


        public InogeniLoupdeckControlPlugin() {
            PluginLog.Init(this.Log);
          
            PluginResources.Init(this.Assembly);
            PluginPath = PluginResources.GetPluginFolder(this);

        }

        // This method is called when the plugin is loaded during the Loupedeck service start-up.
        public override void Load()
        {
            



            PluginReady?.Invoke();
        }

        // This method is called when the plugin is unloaded during the Loupedeck service shutdown.
        public override void Unload()
        {
            PluginLog.Verbose("[InogeniLoupdeckControlPlugin] Unload ");
            InogeniHandler.Disconnect();
            base.Unload();
        }


    }
}
