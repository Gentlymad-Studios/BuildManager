using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BuildManager {
    [Serializable]
    public class SteamSettings {

        [Serializable]
        public class SteamAppConfig {
            public string name;
            public int appID;
            [Tooltip("If this is set, the Application Name will be overwrirten")]
            public string applicationName;
            public List<BuildDepotConfig> buildDepots = new List<BuildDepotConfig> { };
            public List<DLCDepotConfig> dlcDepots = new List<DLCDepotConfig> { };
            public List<LanguageDepotConfig> languageDepots = new List<LanguageDepotConfig> { };
        }

        [Serializable]
        public class LanguageDepotConfig {
            public string name;
            public int depotID;
            public bool enabled = true;
        }

        [Serializable]
        public class DLCDepotConfig {
            public string relativePath;
            public int depotID;
            public bool enabled = true;
            public string[] exclusions = new string[] { };
        }

        [Serializable]
        public class BuildDepotConfig {
            public BuildTarget buildTarget;
            public int depotID;
            public bool enabled = true;
            public string[] exclusions = new string[] { };
        }

        [Serializable]
        public class Paths {

            [SerializeField]
            private string _baseSteamToolFolder = "../BuildTools/Steam/";
            public string BaseSteamToolFolder {
                get {
                    return Path.GetFullPath(Path.Combine(BuildManagerSettings.cachedDataPath, _baseSteamToolFolder));
                }
            }
            [SerializeField]
            private string _temporaryScriptFolder = "Scripts/";
            public string TemporaryScriptFolder {
                get {
                    return Path.GetFullPath(Path.Combine(BaseSteamToolFolder, _temporaryScriptFolder));
                }
            }

            [SerializeField]
            private string _logOutputFolder = "Output/";
            public string LogOutputFolder {
                get {
                    return Path.GetFullPath(Path.Combine(BaseSteamToolFolder, _logOutputFolder));
                }
            }

            [SerializeField]
            private string _executable = "Builder/steamcmd.exe";
            public string Executable {
                get {
                    return Path.GetFullPath(Path.Combine(BaseSteamToolFolder, _executable));
                }
            }

            [SerializeField]
            private string _executableOSX = "Builder/steamcmd.exe";
            public string ExecutableOSX {
                get {
                    return Path.GetFullPath(Path.Combine(BaseSteamToolFolder, _executableOSX));
                }
            }

            public string ValidExecutable{
                get {
#if UNITY_EDITOR_OSX
                    return ExecutableOSX;
#else
                    return Executable;
#endif
                }
            }
        }

        [PasswordField(toggable = true)]
        public string PublisherAPIKey = "<add key here>";
        [PasswordField(toggable = true)]
        public string BuildAccountName = "<add build account here>";
        [PasswordField(toggable = true)]
        public string BuildAccountPassword = "<add build account here>";
        public bool quitProcess = true;

        public Paths paths = new Paths();

        public List<SteamAppConfig> appConfigs = new List<SteamAppConfig> { };
    }

}
