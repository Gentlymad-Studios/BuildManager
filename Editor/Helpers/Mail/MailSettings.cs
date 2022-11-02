using System;
using UnityEditor;

namespace BuildManager {
    [Serializable]
    public class MailSettings {
        public string buildToolsFolder;
        public string subjectText;
        public string mailBodyFilename;
        public string[] recipients;

        [Serializable]
        public class Credentials {
            public string displayName;
            public string username;
            public string mail;
            [PasswordField(toggable = true)]
            public string password;
            public string host;
        }
        public Credentials credentials;
    }
}
