#if UNITY_EDITOR
using UnityEditor;
using EditorHelper;
using System;

namespace BuildManager {
    public class BuildManagerSettingsProvider : SettingsProviderBase {

        private const string path = basePath + nameof(BuildManagerSettings);
        private static readonly string[] tags = new string[] { "BuildManager", "BuildManagerSettings", "BuildManager" };

        public BuildManagerSettingsProvider(SettingsScope scope = SettingsScope.Project)
            : base(path, scope) {
            keywords = tags;
        }

        // Register the SettingsProvider
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider() {
            return BuildManagerSettings.Instance ? new BuildManagerSettingsProvider() : null;
        }

        public override Type GetDataType() {
            return typeof(BuildManagerSettings);
        }

        public override dynamic GetInstance() {
            return BuildManagerSettings.Instance;
        }

        protected override void OnChange() {

        }
    }
}
#endif
