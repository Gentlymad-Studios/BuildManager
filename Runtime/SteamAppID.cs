namespace BuildManager { 
#if UNITY_EDITOR || STEAM
    /// <summary>
    /// Class to manage the Steam App ID of the project.
    /// The ID is managed in a file in Streaming Assets to be able to use it on runtime.
    /// This is relevant for editor tools like the build manager but also the steam backend service.
    /// </summary>
    public class SteamAppID {

        /// <summary>
        /// The internally managed appID
        /// </summary>
        private static int appID = 933820;

        /// <summary>
        /// Static AppID setter/getter. Please keep in mind that setting this variable to another value,
        /// forces a rewrite of the SteamAppID.cs file.
        /// </summary>
        public static int AppID {
            get {
                return appID;
            }
#if UNITY_EDITOR
            set {
                if (appID != value) {

                    // convert GUID of SteamAppID.cs into asset path
                    string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath("86d0eecdc263a50499f9bbb431e0a96b");

                    // replace appID with
                    string contents = System.IO.File.ReadAllText(assetPath);
                    string appIDVariable = nameof(appID) + " = ";
                    contents = contents.Replace(appIDVariable + appID, appIDVariable + value);

                    // write to disk
                    System.IO.File.WriteAllText(assetPath, contents);

                    // change steam_appid.txt file to contain the correct app id
                    SetSteamAppidTxtFile();
                    UnityEditor.AssetDatabase.Refresh();
                }
            }
#endif
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void SetSteamAppidTxtFile() {
            string pathToSteamAppidFile = System.IO.Path.Combine(UnityEngine.Application.dataPath, "../steam_appid.txt");
            if (System.IO.File.Exists(pathToSteamAppidFile)) {
                string appIDString = appID.ToString(); 
                if (System.IO.File.ReadAllText(pathToSteamAppidFile) != appIDString) {
                    System.IO.File.WriteAllText(pathToSteamAppidFile, appIDString);
                }
            } else {
                UnityEngine.Debug.Log("[Steam] steam_appid.txt could not be found at path: " + System.IO.Path.GetFullPath(pathToSteamAppidFile));
            }
        }
#endif
    }
#endif
}
