using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Bluscream;

namespace UnityVRPatcher
{
    class Program {
        static FileInfo patcherFile;
        static FileInfo gameExe;
        static string gameName;
        static DirectoryInfo gameDir;
        static DirectoryInfo gameDataDir;
        static DirectoryInfo gamePluginsDir;
        static FileInfo gameManagersFile;
        static FileInfo gameManagersBackupFile;
        static FileInfo classDataFile;
        static void Main(string[] args)
        {
            try {
                patcherFile = new FileInfo(Process.GetCurrentProcess().MainModule.FileName);
                Console.WriteLine(patcherFile.ToString());
                if (args.Length < 1) {
                    Console.WriteLine("Usage: UnityVRPatcher.exe \"<game.exe>\"");
                    Console.WriteLine("Just drag and drop the game exe onto UnityVRPatcher.exe");
                    Console.ReadKey();
                    return;
                }
                gameExe = new FileInfo(string.Join(" ", args));
                Console.WriteLine("gameExe = "+gameExe.ToString());
                gameDir = gameExe.Directory;
                Console.WriteLine("gameDir = " + gameDir.ToString());
                gameName = Path.GetFileNameWithoutExtension(gameExe.Name);
                Console.WriteLine("gameName = " + gameName);
                gameDataDir = gameDir.Combine($"{gameName}_Data/");
                Console.WriteLine("gameDataDir = " + gameDataDir.ToString());  
                gamePluginsDir = gameDataDir.Combine("Plugins");
                Console.WriteLine("gamePluginsDir = " + gamePluginsDir.ToString());
                gameManagersFile = gameDataDir.CombineFile($"globalgamemanagers");
                Console.WriteLine("gameManagersFile = " + gameManagersFile.ToString());
                gameManagersBackupFile = CreateGameManagersBackup(gameManagersFile);
                Console.WriteLine("gameManagersBackupFile = " + gameManagersBackupFile.ToString());
                classDataFile = patcherFile.Directory.CombineFile("classdata.tpk");
                Console.WriteLine("classDataFile = " + classDataFile.ToString());

                ExtractPlugins(gamePluginsDir);
                PatchVR(gameManagersBackupFile, gameManagersFile, classDataFile);

                Console.WriteLine("Patched successfully, probably.");
                Console.ReadKey();
            }
            catch   (Exception ex) {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
            }
            finally
            {
                Console.WriteLine("Press any key to close this console.");
                Console.ReadKey();
            }
        }

        public static void CopyStream(Stream input, Stream output) {
            // Insert null checking here for production
            byte[] buffer = new byte[8192];

            int bytesRead;
            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0) {
                output.Write(buffer, 0, bytesRead);
            }
        }

        static void ExtractPlugins(DirectoryInfo gamePluginsDir)
        {
            Console.WriteLine($"Extracting embedded plugins to {gamePluginsDir}...");
            gamePluginsDir.CombineFile(nameof(re.openvr_api)+".dll").WriteAllBytes(re.openvr_api, overwrite: false);
            gamePluginsDir.CombineFile(nameof(re.OVRPlugin)+".dll").WriteAllBytes(re.OVRPlugin, overwrite: false);
        }

        static FileInfo CreateGameManagersBackup(FileInfo gameManagersFile)
        {
            Console.WriteLine($"Backing up '{gameManagersFile}'...");
            var backupPath = gameManagersFile.Backup(false);
            Console.WriteLine($"Created backup in '{backupPath}'");
            return backupPath;
        }

        static void PatchVR(FileInfo gameManagersBackupPath, FileInfo gameManagersPath, FileInfo classDataPath)
        {
            Console.WriteLine("Patching globalgamemanagers...");
            Console.WriteLine($"Using classData file from path '{classDataPath}'");

            AssetsManager am = new AssetsManager();
            am.LoadClassPackage(classDataPath.FullName);
            AssetsFileInstance ggm = am.LoadAssetsFile(gameManagersBackupPath.FullName, false);
            AssetsFile ggmFile = ggm.file;
            AssetsFileTable ggmTable = ggm.table;
            am.LoadClassDatabaseFromPackage(ggmFile.typeTree.unityVersion);

            List<AssetsReplacer> replacers = new List<AssetsReplacer>();

            AssetFileInfoEx buildSettings = ggmTable.GetAssetInfo(11);
            AssetTypeValueField buildSettingsBase = am.GetTypeInstance(ggmFile, buildSettings).GetBaseField();
            AssetTypeValueField enabledVRDevices = buildSettingsBase.Get("enabledVRDevices").Get("Array");
            AssetTypeTemplateField stringTemplate = enabledVRDevices.templateField.children[1];
            AssetTypeValueField[] vrDevicesList = new AssetTypeValueField[] { StringField("OpenVR", stringTemplate) };
            enabledVRDevices.SetChildrenList(vrDevicesList);

            replacers.Add(new AssetsReplacerFromMemory(0, buildSettings.index, (int)buildSettings.curFileType, 0xffff, buildSettingsBase.WriteToByteArray()));

            using (AssetsFileWriter writer = new AssetsFileWriter(gameManagersPath.OpenWrite()))
            {
                ggmFile.Write(writer, 0, replacers, 0);
            }
        }

        static AssetTypeValueField StringField(string str, AssetTypeTemplateField template)
        {
            return new AssetTypeValueField()
            {
                children = null,
                childrenCount = 0,
                templateField = template,
                value = new AssetTypeValue(EnumValueTypes.ValueType_String, str)
            };
        }
    }
}
