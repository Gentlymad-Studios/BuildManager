#if UNITY_EDITOR
using UnityEditor;
using EditorHelper;
using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace BuildManager {
    public class BuildManagerSettingsProvider : ScriptableSingletonProviderBase {
        private static readonly string[] tags = new string[] { "BuildManager", "BuildManagerSettings" };
        
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider() {
            return BuildManagerSettings.instance ? new BuildManagerSettingsProvider() : null;
        }

        public BuildManagerSettingsProvider(SettingsScope scope = SettingsScope.Project) : base(BuildManagerSettings.MENUITEMBASE + nameof(BuildManagerSettings), scope) {
            keywords = tags;
        }

        protected override EventCallback<SerializedPropertyChangeEvent> GetValueChangedCallback() {
            return ValueChanged;
        }

        /// <summary>
        /// Called when any value changed.
        /// </summary>
        /// <param name="evt"></param>
        private void ValueChanged(SerializedPropertyChangeEvent evt) {
            // notify all listeneres (ReactiveSettings)
            serializedObject.ApplyModifiedProperties();
            // call save on our singleton as it is a strange hybrid and not a full ScriptableObject
            BuildManagerSettings.instance.Save();
        }

        protected override string GetHeader() {
            return nameof(BuildManager);
        }

        public override Type GetDataType() {
            return typeof(BuildManagerSettings);
        }

        public override dynamic GetInstance() {
            //Force HideFlags
            BuildManagerSettings.instance.OnEnable();
            return BuildManagerSettings.instance;
        }
    }
}
#endif
