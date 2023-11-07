using System.IO;
using UnityEditor;
using UnityEngine;

namespace BuildManager {
    [CreateAssetMenu(fileName = nameof(BuildManagerRuntimeSettings), menuName = EXPECTED_PATH, order = 0)]
    public class BuildManagerRuntimeSettings : ScriptableObject {
        public const string EXPECTED_PATH = "Settings/" + nameof(BuildManagerRuntimeSettings);

        [SerializeField]
        private string versionInfoPath;
        public string VersionInfoPath {
            get {
                return Path.Combine("Assets/Resources/", versionInfoPath);
            }
        }
        public string VersionInfoResourcePath {
            get {
                return versionInfoPath.Remove(versionInfoPath.LastIndexOf('.'));
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

        [SerializeField]
        private string gitHeadPath;
        public string GitHeadPath {
            get {
                return gitHeadPath;
            }
        }


        private static BuildManagerRuntimeSettings instance;
        public static BuildManagerRuntimeSettings Instance {
            get {
                if (instance == null) {
                    instance = Resources.Load<BuildManagerRuntimeSettings>(EXPECTED_PATH);
                }

#if UNITY_EDITOR
                if (instance == null) {
                    BuildManagerRuntimeSettings asset = CreateInstance<BuildManagerRuntimeSettings>();

                    AssetDatabase.CreateAsset(asset, EXPECTED_PATH + ".asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
#endif

                return instance;
            }
        }
    }
}