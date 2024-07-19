using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BuildManager {
    [Serializable]
    public class GeneralSettings {
        public enum ExcludeType {
            Disable = 0,
            AlwaysExclude = 1,
            ExcludeOnlyForSteamAndGOG = 2,
            ExcludeOnlyForMagenta = 3,
        }

        [Serializable]
        public class ExcludePath {
            public BuildTarget buildTarget;
            public ExcludeType excludeType = ExcludeType.AlwaysExclude;
            public string[] paths;
            public bool excludeCompleteFolder = false;
        }

        [Serializable]
        public class LanguageDepot {
            [Serializable]
            public class ContentPath {
                public bool copyContainingDirectory = false;
                public string path;
            }
            public string name = "English";
            public ContentPath[] contentPaths;
        }

        [Serializable]
        public class AdditionalFiles {
            [Tooltip("Target these Files are relevant for. Select 'NoTarget' to apply for all.")]
            public BuildTarget buildTarget;
            [Tooltip("Relativ to the Application DataPath")]
            public string[] files;
            [Tooltip("Relativ to the Executable")]
            public string destination;
        }

        [Serializable]
        public class Paths {
            [SerializeField]
            private string _buildsFolder = "../Builds/";
            [SerializeField]
            private string _addonsFolder = "../Builds/Addons/";
            public string BuildsFolder {
                get {
                    return Path.GetFullPath(Path.Combine(BuildManagerSettings.cachedDataPath,_buildsFolder));
                }
            }
            public string AddonsFolder {
                get {
                    return Path.GetFullPath(Path.Combine(BuildManagerSettings.cachedDataPath, _addonsFolder));
                }
            }
        }

        public Paths paths = new Paths();

        public string languageDepotsBasePath = "../DLC/Languages/";
        public List<LanguageDepot> languageDepots = new List<LanguageDepot>();
        public List<ExcludePath> excludePaths = new List<ExcludePath>();
        public List<ValidatorBase> validators = new List<ValidatorBase> ();
        [Tooltip("Files that should be added to the Build.")]
        public List<AdditionalFiles> additionalFiles = new List<AdditionalFiles>();
    }
}
