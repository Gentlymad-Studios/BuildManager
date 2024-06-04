using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Settings = BuildManager.BuildManagerSettings;
using RSettings = BuildManager.BuildManagerRuntimeSettings;

namespace BuildManager {
    public class HeadlessBuild {
        private static BuildProcessModule buildProcessModule = null;
        private static TargetGroupModule targetGroupModule = null;

        private static string[] definesBackup;

        readonly static List<string> deleteDirectoryExceptions = new List<string>() { "Addon" };

        //Start a Build for Steam
        static void BuildForSteam() {
            BuildGeneral(DistributionPlatform.Steam);
        }

        //Start a Build for Gog
        static void BuildForGog() {
            BuildGeneral(DistributionPlatform.GOG);
        }

        //Start Build for given Platform
        static void BuildGeneral(DistributionPlatform distributionPlatform) {
            Console.WriteLine($"##### Unity startup done, Build started at: {DateTime.Now.ToString("HH:mm:ss")} #####");

            RSettings.Instance.Adapter.BeforeHeadlessBuild();

            ClearProperties();

            targetGroupModule = new TargetGroupModule();
            buildProcessModule = new BuildProcessModule(targetGroupModule);

            //get all given CommandLineArguments and pack them into a single string
            string[] args = Environment.GetCommandLineArgs();
            string arguments = string.Join("", args);

            //remove all stuff before our args
            arguments = arguments.Remove(0, arguments.IndexOf("--args") + 6);

            //extract devbuild arg
            bool isDevBuild;
            bool.TryParse(ExtractArg(arguments, "#isdevbuild"), out isDevBuild);

            //extract target arg
            BuildTarget buildtarget;
            Enum.TryParse(ExtractArg(arguments, "#target"), out buildtarget);
            buildtarget = !BuildTarget.IsDefined(typeof(BuildTarget), buildtarget) ? BuildTarget.StandaloneWindows64 : buildtarget;

            //extract appid arg
            int appID = int.Parse(ExtractArg(arguments, "#appId"));

            //extract upload arg
            bool upload;
            bool.TryParse(ExtractArg(arguments, "#upload"), out upload);

            bool customOverwriteDefines = false;
            string defines = ExtractArg(arguments, "#defines");
            string[] customDefines = null;
            if (!string.IsNullOrEmpty(defines)) {
                customOverwriteDefines = true;
                customDefines = defines.Split(',');
            }

            string distributionBranch = "";

            bool isDemo = Settings.Headless.demo.appIds.Contains(appID);

            //Set Defines
            switch (distributionPlatform) {
                case DistributionPlatform.Steam:
                    //extract steambranch arg
                    distributionBranch = ExtractArg(arguments, "#steambranch");
                    if (customOverwriteDefines) {
                        definesBackup = EditorHelper.Utility.GetDefinesForTargetGroup(targetGroupModule.activeTargetGroup.group);
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroupModule.activeTargetGroup.group, DefineArrayToString(customDefines));
                    } else {
                        OverwriteDefines(Settings.Headless.steam.enabledDefinesOverwrite.ToList(), isDemo);
                    }
                    break;

                case DistributionPlatform.GOG:
                    if (customOverwriteDefines) {
                        definesBackup = EditorHelper.Utility.GetDefinesForTargetGroup(targetGroupModule.activeTargetGroup.group);
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroupModule.activeTargetGroup.group, DefineArrayToString(customDefines));
                    } else {
                        OverwriteDefines(Settings.Headless.gog.enabledDefinesOverwrite.ToList(), isDemo);
                    }
                    break;
            }

            Debug.Log("===== Defines =====" + string.Join("\n", EditorHelper.Utility.GetDefinesForTargetGroup(targetGroupModule.activeTargetGroup.group)));

            DeleteFilesAndFoldersOfBuildsFolder();
            buildProcessModule.SaveHeadless(distributionPlatform, isDevBuild, buildtarget, distributionBranch, appID, upload);

            //Restore Defines to State before Build
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroupModule.activeTargetGroup.group, DefineArrayToString(definesBackup));

            WriteToProperties("Version", BuildManagerRuntimeSettings.Instance.VersionCode);
            WriteToProperties("Error", "None");

            RSettings.Instance.Adapter.AfterHeadlessBuild();

            Console.WriteLine($"##### Build Done: {DateTime.Now.ToString("HH:mm:ss")} #####");
        }

        //Overwrite Defines for given Platform
        static void OverwriteDefines(List<string> enabledDefines, bool isDemo) {
            string[] defines = EditorHelper.Utility.GetDefinesForTargetGroup(targetGroupModule.activeTargetGroup.group);
            definesBackup = EditorHelper.Utility.GetDefinesForTargetGroup(targetGroupModule.activeTargetGroup.group);

            //*DEFINE means disabled
            for (int i=0; i < defines.Length; i++) {
                //Force demo define
                if (isDemo && defines[i].Contains(Settings.Headless.demo.define)) {
                    defines[i] = Settings.Headless.demo.define;
                    continue;
                }

                if (defines[i][0] == '*') {
                    if (enabledDefines.Contains(defines[i].TrimStart('*'))) {
                        defines[i] = defines[i].TrimStart('*');
                    }
                } else {
                    if (!enabledDefines.Contains(defines[i])) {
                        defines[i] = '*' + defines[i];
                    }
                }
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroupModule.activeTargetGroup.group, DefineArrayToString(defines));
        }

        //Converts Define Array to a Single String
        static string DefineArrayToString(string[] defines) {
            EditorHelper.UI.ListField<string> definesList = null;
            List<string> defineList = defines.ToList();
            string defineString = "";

            defineList.RemoveAll(_ => string.IsNullOrEmpty(_));
            definesList = new EditorHelper.UI.ListField<string>(defineList, "", true);
            definesList.items.ForEach((_) => defineString += (!string.IsNullOrEmpty(_)) ? (_ + ";") : "");

            return defineString;
        }

        //Extracts an Argument from given CommandLineArgs
        static string ExtractArg(string allArgs, string arg) {
            if (allArgs.Contains(arg)) {
                //index of the argument with "-..."
                int startIndex = allArgs.IndexOf(arg) + arg.Length;
                //index of the nex argument
                int endIndex = allArgs.IndexOf('#', startIndex);
                //fallback for last argument
                if (endIndex == -1) {
                    endIndex = allArgs.Length;
                }
                return allArgs.Substring(startIndex, endIndex - startIndex);
            } else {
                WriteToProperties("Error", "10");
                EditorApplication.Exit(10);
                return "";
            }
        }

        static void DeleteFilesAndFoldersOfBuildsFolder() {
            DirectoryInfo di = new DirectoryInfo(Settings.General.paths.BuildsFolder);

            if (di.Exists) {
                foreach (FileInfo file in di.GetFiles()) {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories()) {
                    if (deleteDirectoryExceptions.Contains(dir.Name)) continue;
                    dir.Delete(true);
                }
            }
        }

        //Write Jenkins Properties
        public static void WriteToProperties(string key, string value, bool onlyIfExits = false) {
            string file = Path.Combine(Application.dataPath, Settings.Headless.jenkinsPropertiesPath);

            if (!onlyIfExits) {
                File.AppendAllText(file, $"{key}={value}{Environment.NewLine}");
            } else {
                if (File.Exists(file)) {
                    File.AppendAllText(file, $"{key}={value}{Environment.NewLine}");
                }
            }
        }

        //Clear Jenkins Properties
        static void ClearProperties() {
            string file = Path.Combine(Application.dataPath, Settings.Headless.jenkinsPropertiesPath);

            System.IO.File.WriteAllText(file, "");
        }
    }
}