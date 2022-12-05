using System.IO;
using UnityEditor;
using UnityEngine;

namespace BuildManager {
    //[CreateAssetMenu(fileName = nameof(BuildManagerSettings), menuName = nameof(BuildManagerSettings), order = 1)]
    public class BuildManagerSettings : ScriptableObject {
        /// <summary>
        /// thread safe access to data path
        /// </summary>
        public static string cachedDataPath = null;
        public static void CacheDataPath() {
            cachedDataPath = Application.dataPath;
            cachedDataPath = cachedDataPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// thread safe access to data path
        /// </summary>
        public static string cachedStreamingAssetsPath = null;
        public static void CacheStreamingAssetsPath() {
            cachedStreamingAssetsPath = Application.streamingAssetsPath;
            cachedStreamingAssetsPath = cachedStreamingAssetsPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        private static string buildInfoPath = null;
        public static string BuildInfoPath {
            get {
                if (buildInfoPath == null) {
                    buildInfoPath = Path.Combine(BuildPath, nameof(SuccessfulBuildTargets)+".json");
                }
                return buildInfoPath;
            }
        }

        public static SuccessfulBuildTargets GetSuccessfulBuildTargets(DistributionPlatform platform) {
            if (File.Exists(BuildManagerSettings.BuildInfoPath)) {
                SuccessfulBuildTargets succeededTargets = JsonUtility.FromJson<SuccessfulBuildTargets>(File.ReadAllText(BuildManagerSettings.BuildInfoPath));
                Debug.Log("[Existing Builds] " + succeededTargets != null ? string.Join(",", succeededTargets.GetBuildTargets()) : "NULL!");
                if (succeededTargets.distributionPlatform == platform) {
                    return succeededTargets;
                } else {
                    Debug.Log($"[Existing Builds] are not compatible with the selected platform! [{platform}]");
                    return null;
                }
            } else {
                Debug.Log("No build info found!");
                return null;
            }
        }

        private static string buildPath = null;
        public static string BuildPath {
            get {
                return General.paths.BuildsFolder;
            }
        }

        private static string addonsPath = null;
        public static string AddonsPath {
            get {
                return General.paths.AddonsFolder;
            }
        }

        /// <summary>
        /// General Settings
        /// </summary> 
        public GeneralSettings general = new GeneralSettings();
        public static GeneralSettings General {
            get {
                return Instance.general;
            }
        }

        /// <summary>
        /// Steam Settings
        /// </summary> 
        public SteamSettings steam = new SteamSettings();
        public static SteamSettings Steam {
            get {
                return Instance.steam;
            }
        }

        /// <summary>
        /// GOGGalaxy Settings
        /// </summary> 
        public GOGGalaxySettings gogGalaxy = new GOGGalaxySettings();
        public static GOGGalaxySettings GOGGalaxy {
            get {
                return Instance.gogGalaxy;
            }
        }

        /// <summary>
        /// Magenta Settings
        /// </summary> 
        public MagentaSettings magenta = new MagentaSettings();
        public static MagentaSettings Magenta {
            get {
                return Instance.magenta;
            }
        }

        /// <summary>
        /// Mail Settings
        /// </summary> 
        public MailSettings mail = new MailSettings();
        public static MailSettings Mail {
            get {
                return Instance.mail;
            }
        }

        /// <summary>
        /// Headless Settings
        /// </summary> 
        public HeadlessSettings headless = new HeadlessSettings();
        public static HeadlessSettings Headless {
            get {
                return Instance.headless;
            }
        }

        private static BuildManagerSettings _instance = null;
        public static BuildManagerSettings Instance {
            get {
                if (_instance == null) {
                    CacheDataPath();
                    CacheStreamingAssetsPath();
                    _instance = EditorHelper.Utility.CreateSettingsFile<BuildManagerSettings>(cachedDataPath);
                }
                return _instance;
            }
        }
    }
}

