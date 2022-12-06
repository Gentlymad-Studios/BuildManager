using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BuildManager {
    [Serializable]
    public class HeadlessSettings {
        [Tooltip("Path to the Jenkins Properties File. Will be Combined with the Unity Data Path.")]
        [SerializeField]
        public string jenkinsPropertiesPath = "../../jenkins.properties";
        
        [SerializeField]
        public SteamHeadless steam = new SteamHeadless();

        [SerializeField]
        public GogHeadless gog = new GogHeadless();

        [Serializable]
        public class SteamHeadless {
            [Tooltip("Fill with Defines that should be forced enabled for Headless Builds. Other Defines will be disabled.")]
            public string[] enabledDefinesOverwrite = new string[] { "STEAM" };
        }

        [Serializable]
        public class GogHeadless {
            [Tooltip("Fill with Defines that should be forced enabled for Headless Builds. Other Defines will be disabled.")]
            public string[] enabledDefinesOverwrite = new string[] { "GOGGALAXY" };
        }
    }
}