using Newtonsoft.Json.Linq;
using System.IO;

namespace BuildManager { 
#if UNITY_EDITOR || STEAM
    /// <summary>
    /// Class to manage the Steam App ID of the project.
    /// The ID is managed in a file in Streaming Assets to be able to use it on runtime.
    /// This is relevant for editor tools like the build manager but also the steam backend service.
    /// </summary>
    public class SteamAppID {
        /// <summary>
        /// Static AppID setter/getter
        /// </summary>
        public static int AppID {
            get {
                return GetSteamAppIdValue<int>("steamAppId");
            }
            set {
                SetSteamAppIdValue("steamAppId", value.ToString());
            }
        }

        #region JSON
        /// <summary>
        /// Try to Load VersionInfo JSON File, otherwise it creates a new one
        /// </summary>
        /// <returns>T</returns>
        private static T GetSteamAppIdValue<T>(string key) {
            if (!File.Exists(BuildManagerRuntimeSettings.Instance.SteamAppIdPath)) {
                //Create Default JSON File
                CreateDefaultSteamAppIdFile();
            }

            JObject json = JObject.Parse(File.ReadAllText(BuildManagerRuntimeSettings.Instance.SteamAppIdPath));
            return json.Value<T>(key);
        }

        /// <summary>
        /// Set JSON value for given key
        /// </summary>
        /// <param name="key">Json Key</param>
        /// <param name="value">Json Value</param>
        private static void SetSteamAppIdValue(string key, string value) {
            JObject json = JObject.Parse(File.ReadAllText(BuildManagerRuntimeSettings.Instance.SteamAppIdPath));
            json[key] = value;
            File.WriteAllText(BuildManagerRuntimeSettings.Instance.SteamAppIdPath, json.ToString());
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        /// <summary>
        /// Create Default JSON File
        /// </summary>
        private static void CreateDefaultSteamAppIdFile() {
            JObject json = new JObject();
            json["steamAppId"] = "0";
            File.WriteAllText(BuildManagerRuntimeSettings.Instance.SteamAppIdPath, json.ToString());
        }
        #endregion
    }
#endif
}
