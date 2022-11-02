using System;
using UnityEditor;

namespace BuildManager {
    [Serializable]
    public class MagentaSettings {
        public BuildTarget allowedBuildTarget = BuildTarget.StandaloneWindows64;
        public string preinstalledLanguage;
        public string[] languagesToExtract;
        public string localizationPath;
        public string buildToolsFolder;

        [Serializable]
        public class FTPCredentials {
            public string username;
            [PasswordField(toggable = true)]
            public string password;
            public string hostURL;
        }
        public FTPCredentials ftp;
    }
}
