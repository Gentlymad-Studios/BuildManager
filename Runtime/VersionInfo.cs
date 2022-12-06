using System.Reflection;
using System;
using UnityEngine;
using System.IO;

[assembly: AssemblyVersion("1.2.*")]
namespace BuildManager {
    public class VersionInfo {
        /// <summary>
        /// A specialized, custom version name
        /// </summary>
        public const string customVersionName = "";
        private static string versionInfoPath = Path.Combine(Path.Combine(Application.dataPath, "Resources"), "VersionInfo.json");

        #region BuildCounter
        /// <summary>
        /// Get the BuildCounterFrom the file
        /// </summary>
        /// <returns></returns>
        private static int GetBuildCounterFromFile() {
            VersionInfoJson versionInfoJson = LoadVersionInfoJson();
            if (versionInfoJson != null) {
                return versionInfoJson.BuildCounter;
            }
            return 0;
        }

        /// <summary>
        /// A build counter, auto incremented on build
        /// </summary>
        private static int _buildCounter = -1;
        public static int BuildCounter {
            get {
#if !UNITY_EDITOR
                if (_buildCounter < 0) {
                    _buildCounter = GetBuildCounterFromFile();
                }
#else
                if (Application.isPlaying) {
                    if (_buildCounter < 0) {
                        _buildCounter = GetBuildCounterFromFile();
                    }
                } else {
                    _buildCounter = GetBuildCounterFromFile();
                }
#endif
                return _buildCounter;
            }
#if UNITY_EDITOR
            set {
                _buildCounter = value;

                VersionInfoJson versionInfoJson = LoadVersionInfoJson();
                versionInfoJson.BuildCounter = _buildCounter;
            }
#endif
        }
        #endregion

        #region GitHash
        /// <summary>
        /// Load githash from resources.
        /// </summary>
        /// <returns></returns>
        private static string LoadGitHashFromFile() {
            VersionInfoJson versionInfoJson = LoadVersionInfoJson();
            if (versionInfoJson != null && !string.IsNullOrWhiteSpace(versionInfoJson.GitHash)) {
                return versionInfoJson.GitHash;
            }
            return "";
        }

        /// <summary>
        /// The saved/ corresponding git Hash of this build
        /// </summary>
        private static string _gitHash = null;
        public static string GitHash {
            get {
#if !UNITY_EDITOR
                if (_gitHash == null) {
                    _gitHash = LoadGitHashFromFile();
                }
#else
                if (Application.isPlaying) {
                    if (_gitHash == null) {
                        _gitHash = LoadGitHashFromFile();
                    }
                } else {
                    _gitHash = LoadGitHashFromFile();
                }
#endif
                return _gitHash;
            }
        }

#if UNITY_EDITOR
        //[UnityEditor.InitializeOnLoadMethod]
        [UnityEditor.MenuItem("Tools/" + nameof(UpdateGitHash))]
        public static void UpdateGitHash() {
            VersionInfoJson versionInfoJson = LoadVersionInfoJson();
            versionInfoJson.Version = CreateGitHash();
            UnityEditor.AssetDatabase.Refresh();
        }

        public static string CreateGitHash() {
            string latestCommitHash = "";
            string path = System.IO.Path.Combine(Application.dataPath, "../../../.git/logs/HEAD");
            if (System.IO.File.Exists(path)) {
                string[] lines = System.IO.File.ReadAllLines(path);
                if (lines != null && lines.Length > 0) {
                    string latestLine = lines[lines.Length - 1];
                    string[] latestLineData = latestLine.Split(' ');
                    if (latestLineData != null && latestLineData.Length > 1) {
                        latestCommitHash = latestLineData[1].Substring(0, 9);
                    }
                }
            }
            return latestCommitHash;
        }
#endif
        #endregion

        #region Timestamp
        /// <summary>
        /// Load the timestamp from file
        /// </summary>
        /// <returns></returns>
        private static string LoadTimestampFromFile() {
            VersionInfoJson versionInfoJson = LoadVersionInfoJson();
            if (versionInfoJson != null && !string.IsNullOrWhiteSpace(versionInfoJson.BuildTimestamp)) {
                return versionInfoJson.BuildTimestamp;
            }
            return "";
        }

        /// <summary>
        /// The current/ corresponding Timestamp
        /// </summary>
        private static string _buildTimestamp = null;
        public static string BuildTimestamp {
            get {
#if !UNITY_EDITOR
                if (_buildTimestamp == null) {
                    _buildTimestamp = LoadTimestampFromFile();
                }
#else
                if (Application.isPlaying) {
                    if (_buildTimestamp == null) {
                        _buildTimestamp = LoadTimestampFromFile();
                    }
                } else {
                    _buildTimestamp = LoadTimestampFromFile();
                }
#endif
                return _buildTimestamp;
            }
        }

#if UNITY_EDITOR
        //[UnityEditor.InitializeOnLoadMethod]
        public static void UpdateBuildTimestamp() {
            string dateAndTime = CreateBuildTimestamp();
            VersionInfoJson versionInfoJson = LoadVersionInfoJson();
            versionInfoJson.BuildTimestamp = dateAndTime;
        }

        public static string CreateBuildTimestamp() {
            return System.DateTime.Now.ToShortDateString() + " | " + System.DateTime.Now.ToShortTimeString();
        }
#endif
        #endregion

        #region Version
#if UNITY_EDITOR
        public static void UpdateVersionCode() {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            VersionInfoJson versionInfoJson = LoadVersionInfoJson();
            versionInfoJson.Version = version;
        }
#endif
        private static string LoadVersionCodeFromFile() {
            VersionInfoJson versionInfoJson = LoadVersionInfoJson();
            if (versionInfoJson != null && !string.IsNullOrWhiteSpace(versionInfoJson.Version)) {
                return versionInfoJson.Version;
            }
            return "";
        }

        /// <summary>
        /// The Version object
        /// </summary>
        public static Version Version {
            get {

#if UNITY_EDITOR
                if (Application.isPlaying) {
                    _Version = Assembly.GetExecutingAssembly().GetName().Version;
                } else {
                    _Version = new Version(LoadVersionCodeFromFile());
                }
#else
                if (_Version == null) {
                    _Version = new Version(LoadVersionCodeFromFile());
                }
#endif
                return _Version;
            }
        }
        private static Version _Version = null;

        /// <summary>
        /// Stringified Version
        /// </summary>
        public static string VersionCode {
            get {
#if UNITY_EDITOR
                _VersionCode = Version.ToString();
#else
                if (_VersionCode == null) {
                    _VersionCode = Version.ToString();
                }
#endif
                return _VersionCode;
            }
        }
        private static string _VersionCode = null;

        /// <summary>
        /// The Automated Version Name
        /// </summary>
        public static string VersionName {
            get {
                if (string.IsNullOrWhiteSpace(customVersionName)) {
                    if (_versionName == null) {
                        if (Version.Minor > 19 || Version.Major > 0) {
                            _versionName = "Release";
                        } else {
                            if (Version.Minor < 14) {
                                _versionName = "Technical Test";
                            } else if (Version.Minor < 15) {
                                _versionName = "Prototype";
                            } else if (Version.Minor < 16) {
                                _versionName = "Alpha";
                            } else if (Version.Minor < 16) {
                                _versionName = "Beta";
                            } else if (Version.Minor == 19) {
                                _versionName = "Goldmaster";
                            }
                        }
                    }
                    return _versionName;
                }
                return customVersionName;
            }
        }
        private static string _versionName = null;
        #endregion

        /// <summary>
        /// Try to Load VersionInfo JSON File, otherwise it creates a new one
        /// </summary>
        /// <returns>VersionInfoJson</returns>
        private static VersionInfoJson LoadVersionInfoJson() {
            if (!File.Exists(versionInfoPath)) {
                string resDir = Path.GetDirectoryName(versionInfoPath);
                if (!Directory.Exists(resDir)) {
                    Directory.CreateDirectory(resDir);
                }

                VersionInfoJson newVersionInfo = new VersionInfoJson();
                newVersionInfo.SaveToFile();
                return newVersionInfo;
            }

            string json = File.ReadAllText(versionInfoPath);
            return JsonUtility.FromJson<VersionInfoJson>(json);
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
                    _currentVersionInfoData.SetGitHash(CreateGitHash());
                    _currentVersionInfoData.SetBuildTimestamp(CreateBuildTimestamp());
#else
                    _currentVersionInfoData.SetVersion(VersionCode);
                    _currentVersionInfoData.SetAppType(Debug.isDebugBuild ? VersionInfoData.AppType.DebugBuild : VersionInfoData.AppType.Build);
                    _currentVersionInfoData.SetGitHash(GitHash);
                    _currentVersionInfoData.SetBuildTimestamp(BuildTimestamp);
#endif
                }
                return _currentVersionInfoData;
            }
        }

        public class VersionInfoData {
            public enum AppType { Editor, DebugBuild, Build }
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

        public class VersionInfoJson {
            //Version
            public string _version = "0.0.0.0";
            public string Version {
                get {
                    return _version;
                }
                set {
                    _version = value;
                    SaveToFile();
                }
            }

            //Build Counter
            public int _buildCounter = 0;
            public int BuildCounter {
                get {
                    return _buildCounter;
                }
                set {
                    _buildCounter = value;
                    SaveToFile();
                }
            }

            //Timestamp
            public string _buildTimestamp = "";
            public string BuildTimestamp {
                get {
                    return _buildTimestamp;
                }
                set {
                    _buildTimestamp = value;
                    SaveToFile();
                }
            }

            //GitHash
            public string _gitHash = "";
            public string GitHash {
                get {
                    return _gitHash;
                }
                set {
                    _gitHash = value;
                    SaveToFile();
                }
            }

            public void SaveToFile() {
                string json = JsonUtility.ToJson(this);
                File.WriteAllText(versionInfoPath, json);
#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
#endif
            }
        }
    }
}

