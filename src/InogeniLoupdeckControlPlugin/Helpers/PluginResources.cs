namespace Loupedeck.InogeniLoupdeckControlPlugin.Helpers
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;

    // A helper class for managing plugin resources.
    // Note that the resource files handled by this class must be embedded in the plugin assembly at compile time.
    // That is, the Build Action of the files must be "Embedded Resource" in the plugin project.

    internal static class PluginResources
    {
        private static Assembly _assembly;

        public static void Init(Assembly assembly)
        {
            assembly.CheckNullArgument(nameof(assembly));
            PluginResources._assembly = assembly;
        }





        // Retrieves the names of all the resource files in the specified folder.
        // The parameter `folderName` must be specified as a full path, for example, `Loupedeck.LoupedeckAtemControlerPlugin.Resources`.
        // Returns the full names of the resource files, for example, `Loupedeck.LoupedeckAtemControlerPlugin.Resources.Resource.txt`.
        public static String[] GetFilesInFolder(String folderName) => PluginResources._assembly.GetFilesInFolder(folderName);



        // Finds the first resource file with the specified file name.
        // Returns the full name of the found resource file.
        // Throws `FileNotFoundException` if the resource file is not found.
        public static String FindFile(String fileName) => PluginResources._assembly.FindFileOrThrow(fileName);

        // Finds all the resource files that match the specified regular expression pattern.
        // Returns the full names of the found resource files.
        // Example:
        //     `PluginResources.FindFiles(@"\w+\.txt$")` returns all the resource files with the extension `.txt`.
        public static String[] FindFiles(String regexPattern) => PluginResources._assembly.FindFiles(regexPattern);

        // Finds the first resource file with the specified file name, and returns the file as a stream.
        // Throws `FileNotFoundException` if the resource file is not found.
        public static Stream GetStream(String resourceName) => PluginResources._assembly.GetStream(PluginResources.FindFile(resourceName));

        // Reads content of the specified text file, and returns the file content as a string.
        // Throws `FileNotFoundException` if the resource file is not found.
        public static String ReadTextFile(String resourceName) => PluginResources._assembly.ReadTextFile(PluginResources.FindFile(resourceName));

        // Reads content of the specified binary file, and returns the file content as bytes.
        // Throws `FileNotFoundException` if the resource file is not found.
        public static Byte[] ReadBinaryFile(String resourceName) => PluginResources._assembly.ReadBinaryFile(PluginResources.FindFile(resourceName));

        // Reads content of the specified image file, and returns the file content as a bitmap image.
        // Throws `FileNotFoundException` if the resource file is not found.
        public static BitmapImage ReadImage(String resourceName) => PluginResources._assembly.ReadImage(PluginResources.FindFile(resourceName));

        // Extracts the specified resource file to the given file path in the file system.
        // Throws `FileNotFoundException` if the resource file is not found, or a system exception if the output file cannot be written.
        public static void ExtractFile(String resourceName, String filePathName)
            => PluginResources._assembly.ExtractFile(PluginResources.FindFile(resourceName), filePathName);



        public static String GetPluginFolder(Plugin plugin) {
            var finalPluginPath = "";


            var dir = new DirectoryInfo(plugin.GetPluginDataDirectory());
            var newPath = Path.Combine(dir.Parent?.Parent?.FullName, "Plugins");
            var possibleLinkFileName = Path.Combine(dir.Parent?.Parent?.FullName, "Plugins", "InogeniLoupdeckControlPlugin.link");

            var pluginBase = "";
            var packageInfoFile = "";

            if (File.Exists(possibleLinkFileName))
            {
                PluginLog.Verbose($"[PluginResources] Plugin Development link file existent {possibleLinkFileName}");
                pluginBase = File.ReadLines(possibleLinkFileName).FirstOrDefault()?.Trim();
                PluginLog.Verbose($"[PluginResources] Plugin is here {pluginBase}");
                packageInfoFile = Path.Combine(pluginBase, "metadata", "LoupedeckPackage.yaml");

            }
            else
            {
                pluginBase = Path.Combine(dir.Parent?.Parent?.FullName, "Plugins", "InogeniLoupdeckControlPlugin");
                PluginLog.Verbose($"[PluginResources] Plugin is here {pluginBase}");
                packageInfoFile = Path.Combine(pluginBase, "metadata", "LoupedeckPackage.yaml");
            }

            var pluginSubFolderKeyword = "pluginFolderMac";
            var pluginSubFolder = "";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                pluginSubFolderKeyword = "pluginFolderWin";
            }



            if (File.Exists(packageInfoFile))
            {
                pluginSubFolder = File.ReadLines(packageInfoFile).FirstOrDefault(line => line.TrimStart().StartsWith(pluginSubFolderKeyword, StringComparison.OrdinalIgnoreCase));
                var parts = pluginSubFolder.Split(new[] { ':' }, 2); // limit to 2 parts
                if (parts.Length == 2)
                {
                    pluginSubFolder = parts[1].Trim();
                }


                PluginLog.Verbose($"[PluginResources] Plugin subfolder is {pluginSubFolder}");

            }
            else
            {
                PluginLog.Error($"[PluginResources] ERROR cannot find any LoupedeckPackage.yaml file  {packageInfoFile}");
            }

            finalPluginPath = Path.Combine(pluginBase, pluginSubFolder);

            if (!Directory.Exists(finalPluginPath))
            {
                PluginLog.Error($"[PluginResources] ERROR cannot find the plugin directory  {finalPluginPath}");
            }



            PluginLog.Verbose($"[PluginResources] final after all plugin directory  {finalPluginPath}");

            return finalPluginPath;
        }

    }
}
