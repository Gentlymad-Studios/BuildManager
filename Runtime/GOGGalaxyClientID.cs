namespace BuildManager {
#if UNITY_EDITOR || GOGGALAXY
    /// <summary>
    /// Class to manage the Galaxy Client ID of the project.
    /// This is relevant for editor tools like the build manager but also the steam backend service.
    /// </summary>
    public class GOGGalaxyClientIDAndSecret {
        /// <summary>
        /// The internally managed productID
        /// </summary>
        private static string productID = "1464526060";

        /// <summary>
        /// The internally managed clientID
        /// </summary>
        private static string clientID = "53031671814666115";

        /// <summary>
        /// The internally managed clientSecret
        /// </summary>
        private static string clientSecret = "6454bf48736bfc4325978c43d43493f9d935b60b6646b216610cf30a9ab24156";

#if UNITY_EDITOR
        private const string GUIDToThisFile = "0be915f8e82765549bb90dfc880f5753";
#endif
        /// <summary>
        /// Static ProductID setter/getter. Please keep in mind that setting this variable to another value,
        /// forces a rewrite of the GOGGalaxyClientIDAndSecret.cs file.
        /// </summary>
        public static string ProductID {
            get {
                return productID;
            }
#if UNITY_EDITOR
            set {
                SetStringVariable(nameof(productID), productID, value);
            }
#endif
        }

        /// <summary>
        /// Static ClientID setter/getter. Please keep in mind that setting this variable to another value,
        /// forces a rewrite of the GOGGalaxyClientIDAndSecret.cs file.
        /// </summary>
        public static string ClientID {
            get {
                return clientID;
            }
#if UNITY_EDITOR
            set {
                SetStringVariable(nameof(clientID), clientID, value);
            }
#endif
        }

        /// <summary>
        /// Static ClientSecret setter/getter. Please keep in mind that setting this variable to another value,
        /// forces a rewrite of the GOGGalaxyClientIDAndSecret.cs file.
        /// </summary>
        public static string ClientSecret {
            get {
                return clientSecret;
            }
#if UNITY_EDITOR
            set {
                SetStringVariable(nameof(clientSecret), clientSecret, value);
            }
#endif
        }

#if UNITY_EDITOR
        private static void SetStringVariable(string nameOfVariable, string currentValue, string targetValue) {
            if (currentValue != targetValue) {

                // convert GUID of SteamAppID.cs into asset path
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(GUIDToThisFile);

                // replace appID with
                string contents = System.IO.File.ReadAllText(assetPath);
                string appIDVariable = $"{nameOfVariable} = \"{currentValue}\"";
                string targetAppIDVariable = $"{nameOfVariable} = \"{targetValue}\"";
                contents = contents.Replace(appIDVariable, targetAppIDVariable);

                // write to disk
                System.IO.File.WriteAllText(assetPath, contents);

                // change
                // file to contain the correct app id
                UnityEditor.AssetDatabase.Refresh();
            }
        }

/*
        [UnityEditor.InitializeOnLoadMethod]
        private static void SetSteamAppidTxtFile() {
            ClientID = "50982309205119396";
            ClientSecret = "329dda218eab6254a042847b31d159c49a5243ad00f51bdb643649f10e5bd89f";
        }
*/
#endif

    }
#endif
}
