using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BuildManager {
    [CreateAssetMenu(fileName = nameof(BuildManagerRuntimeSettings), menuName = EXPECTED_PATH, order = 0)]
    public class BuildManagerRuntimeSettings : ScriptableObject {
        public const string EXPECTED_PATH = "Settings/" + nameof(BuildManagerRuntimeSettings);

        [Header("Adapter")]
        [SerializeReference]
        public CustomAdapter customAdapter = null;

        [NonSerialized]
        public IAdapter adapter;
        public IAdapter Adapter {
            get {
                if (adapter == null) {
                    if (customAdapter == null) {
                        adapter = new DefaultAdapter();
                    } else {
                        adapter = customAdapter;
                    }
                }
                return adapter;
            }
        }

        [SerializeField]
        private string steamAppIdPath;
        public string SteamAppIdPath {
            get {
                return Path.Combine("Assets/Resources/", steamAppIdPath);
            }
        }
        public string SteamAppIdResourcePath {
            get {
                return steamAppIdPath.Remove(steamAppIdPath.LastIndexOf('.'));
            }
        }

        #region Version
        private Version version = null;
        public Version Version {
            get {
#if UNITY_EDITOR
                if (Application.isPlaying) {
                    UpdateVersionCode();
                }
#endif
                if (version == null) {
                    version = new Version(versionCode);
                }

                return version;
            }
        }

        /// <summary>
        /// Stringified Version
        /// </summary>
        [HideInInspector]
        [SerializeField]
        public string versionCode = null;
        public string VersionCode {
            get => versionCode;
        }

        /// <summary>
        /// The Automated Version Name
        /// </summary>
        public string VersionName {
            get {
                if (Version == null) {
                    Debug.LogWarning($"Version [{VersionCode}] is in wrong format. Should be x.x.x or x.x.x.x");
                    return "Unknow";
                }

                if (Version.Minor > 19 || Version.Major > 0) {
                    return "Release";
                } else {
                    if (Version.Minor < 14) {
                        return "Technical Test";
                    } else if (Version.Minor < 15) {
                        return "Prototype";
                    } else if (Version.Minor < 16) {
                        return "Alpha";
                    } else if (Version.Minor < 17) {
                        return "Beta";
                    } else if (Version.Minor == 19) {
                        return "Goldmaster";
                    } else {
                        return "Unknown";
                    }
                }
            }
        }

        public void UpdateVersionCode() {
            versionCode = Adapter.CreateVersionCode();
        }
        #endregion

        #region GitHash
        [SerializeField]
        private string gitHeadPath;
        public string GitHeadPath {
            get {
                return gitHeadPath;
            }
        }

        [HideInInspector]
        [SerializeField]
        private string gitHash = null;
        public string GitHash {
            get => gitHash;
            set => gitHash = value;
        }

        public void UpdateGitHash() {
            GitHash = Adapter.CreateGitHash();
        }
        #endregion

        #region Timestamp
        [HideInInspector]
        [SerializeField]
        private string buildTimestamp = null;
        public string BuildTimestamp {
            get => buildTimestamp;
            set => buildTimestamp = value;
        }

        public void UpdateBuildTimestamp() {
            BuildTimestamp = Adapter.CreateBuildTimestamp();
        }
        #endregion

        #region BuildCounter
        [HideInInspector]
        [SerializeField]
        private int buildCounter = -1;
        public int BuildCounter {
            get => buildCounter;
            set => buildCounter = value;
        }
        #endregion

        private static BuildManagerRuntimeSettings instance;
        public static BuildManagerRuntimeSettings Instance {
            get {
                if (instance == null) {
                    instance = Resources.Load<BuildManagerRuntimeSettings>(EXPECTED_PATH);
                }

#if UNITY_EDITOR
                if (instance == null) {
                    BuildManagerRuntimeSettings asset = CreateInstance<BuildManagerRuntimeSettings>();

                    string directory = Path.GetDirectoryName(Path.GetFullPath("Assets/Resources/" + EXPECTED_PATH + ".asset"));
                    if (!Directory.Exists(directory)) {
                        Directory.CreateDirectory(directory);
                    }

                    string filename = Path.GetFileName(EXPECTED_PATH + ".asset");

                    AssetDatabase.CreateAsset(asset, $"Assets/Resources/{EXPECTED_PATH}.asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
#endif

                return instance;
            }
        }

        /// <summary>
        /// Make sure we print the VersionCode into the log on startup.
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        private static void OnRuntimeMethodLoad() {
            CurrentVersionInfoData.Log();
        }

        private static VersionInfoData _currentVersionInfoData = null;

        public static VersionInfoData CurrentVersionInfoData {
            get {
                if (_currentVersionInfoData == null) {
                    _currentVersionInfoData = new VersionInfoData();
#if UNITY_EDITOR
                    _currentVersionInfoData.SetVersion("N/A");
                    _currentVersionInfoData.SetAppType(VersionInfoData.AppType.Editor);
                    _currentVersionInfoData.SetGitHash(Instance.Adapter.CreateGitHash());
                    _currentVersionInfoData.SetBuildTimestamp(Instance.Adapter.CreateBuildTimestamp());
#else
                    _currentVersionInfoData.SetVersion(Instance.VersionCode);
                    _currentVersionInfoData.SetAppType(Debug.isDebugBuild ? VersionInfoData.AppType.DebugBuild : VersionInfoData.AppType.Build);
                    _currentVersionInfoData.SetGitHash(Instance.GitHash);
                    _currentVersionInfoData.SetBuildTimestamp(Instance.BuildTimestamp);
#endif
                }
                return _currentVersionInfoData;
            }
        }

        [Serializable]
        public class VersionInfoData {
            public enum AppType {
                Editor, DebugBuild, Build
            }
            public const string versionTag = " [Version]";
            public const string buildTimestampTag = " [Date&Time]";
            public const string gitHashTag = " [GitHash]";
            public const string appTypeTag = " [AppType]";

            public string appTypeOutput;
            public string versionOutput;
            public string buildTimestampOutput;
            public string gitHashOutput;

            public AppType appType;
            public string version;
            public string buildTimestamp;
            public string gitHash;

            public void SetAppType(AppType appType) {
                this.appType = appType;
                appTypeOutput = appType.ToString() + appTypeTag;
            }

            public void SetVersion(string version) {
                this.version = version;
                versionOutput = version + versionTag;
            }

            public void SetBuildTimestamp(string timestamp) {
                this.buildTimestamp = timestamp;
                buildTimestampOutput = timestamp + buildTimestampTag;
            }

            public void SetGitHash(string gitHash) {
                this.gitHash = gitHash;
                gitHashOutput = gitHash + gitHashTag;
            }

            public void Log() {
                Debug.LogWarning($"Current Date:{DateTime.Now}");
                Debug.LogWarning("--------------- [VersionInfo] ---------------");
                Debug.LogWarning(appTypeOutput);
                Debug.LogWarning(versionOutput);
                Debug.LogWarning(buildTimestampOutput);
                Debug.LogWarning(gitHashOutput);
                Debug.LogWarning($"Memory: {SystemInfo.systemMemorySize}MB");
                Debug.LogWarning("---------------------------------------------");
            }
        }
    }
}