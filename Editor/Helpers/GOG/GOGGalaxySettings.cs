using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BuildManager {
    [Serializable]
    public class GOGGalaxySettings {
        public enum OSBitness { x64 = 64, x86 = 32 }

        [Serializable]
        public class GOGGalaxyAppConfig {
            public string name;
            public string productID = "";
            public string clientID = "";
            public string clientSecret = "";
            public string displayName = "";
            [Tooltip("If this is set, the Application Name will be overwrirten")]
            public string applicationName;

            public List<PlatformConfig> platformConfigs = new List<PlatformConfig> { };
            public List<LanguageDepotConfig> languageDepots = new List<LanguageDepotConfig> { };
        }

        [Serializable]
        public class PlatformConfig {
            public bool enabled = true;
            public enum Platform { windows, osx, linux };
            public Platform platform;
            public List<BuildDepotConfig> buildDepots = new List<BuildDepotConfig> { };
            public List<DLCDepotConfig> dlcDepots = new List<DLCDepotConfig>() { };

            public string GetPlatformString() {
                switch (platform) {
                    case Platform.linux:
                        return "gnu-linux";
                    default:
                        return platform.ToString();
                }
            }
        }

        [Serializable]
        public class DLCDepotConfig {
            public string name;
            public string relativePath;
            public int depotID;
            public OSBitness osBitness = OSBitness.x64;
            public bool enabled = true;
        }

        [Serializable]
        public class BuildDepotConfig {
            public BuildTarget buildTarget;
            public OSBitness osBitness = OSBitness.x64;
            public bool enabled = true;
        }

        [Serializable]
        public class LanguageDepotConfig {
            public string name;
            public string languageCode;
            public bool enabled = true;
        }
        [Serializable]
        public class Paths {

            [SerializeField]
            private string _baseSteamToolFolder = "";
            public string BaseSteamToolFolder {
                get {
                    return Path.GetFullPath(Path.Combine(BuildManagerSettings.cachedDataPath, _baseSteamToolFolder));
                }
            }
            [SerializeField]
            private string _temporaryScriptFolder = "";
            public string TemporaryScriptFolder {
                get {
                    return Path.GetFullPath(Path.Combine(BaseSteamToolFolder, _temporaryScriptFolder));
                }
            }

            [SerializeField]
            private string _executable = "";
            public string Executable {
                get {
                    return Path.GetFullPath(Path.Combine(BaseSteamToolFolder, _executable));
                }
            }
        }

        [PasswordField(toggable = true)]
        public string BuildAccountName = "<add build account here>";
        [PasswordField(toggable = true)]
        public string BuildAccountPassword = "<add build account here>";

        public Paths paths = new Paths();
        public List<GOGGalaxyAppConfig> appConfigs = new List<GOGGalaxyAppConfig> { };

    }
}

