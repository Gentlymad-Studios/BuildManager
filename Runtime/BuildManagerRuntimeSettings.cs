using Codice.CM.Common;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BuildManager {
    public class BuildManagerRuntimeSettings : ScriptableObject {
        [SerializeField]
        private string versionInfoPath = "VersionInfo.json";
        public string VersionInfoPath {
            get {
                return Path.Combine("Assets/Resources/", versionInfoPath);
            }
        }

        [SerializeField]
        private string steamAppIdPath = "SteamAppId.json";
        public string SteamAppIdPath {
            get {
                return Path.Combine("Assets/Resources/", steamAppIdPath);
            }
        }

        [SerializeField]
        private string gitHeadPath = "../../../.git/logs/HEAD";
        public string GitHeadPath {
            get {
                return gitHeadPath;
            }
        }


        private static BuildManagerRuntimeSettings instance;
        public static BuildManagerRuntimeSettings Instance {
            get {
                if (instance == null) {
                    instance = Resources.Load<BuildManagerRuntimeSettings>("Settings/BuildManagerRuntimeSettings");
                }

#if UNITY_EDITOR
                if (instance == null) {
                    BuildManagerRuntimeSettings asset = CreateInstance<BuildManagerRuntimeSettings>();

                    AssetDatabase.CreateAsset(asset, "Assets/Resources/Settings/BuildManagerRuntimeSettings.asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
#endif

                return instance;
            }
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        public static void Initialize() {
            instance = Instance;
        }
#endif
    }
}