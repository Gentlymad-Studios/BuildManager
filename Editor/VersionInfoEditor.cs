using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace BuildManager {
    public class VersionInfoEditor : EditorHelper.UI.WindowBase {
        private GUIStyle centeredStyle;
        private int major, minor;
        private string customVersionName;

        [MenuItem("Tools/Version Manager")]
        public static void ShowWindow() {
            EditorHelper.UI.GetOrCreateWindowWithTitle<VersionInfoEditor>("Version Info");
        }

        private int DrawAndSetVersionInfoProperty(string label, int versionInfoProperty) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.padding = new RectOffset();
            int value;
            if (GUILayout.Button("+", style, GUILayout.Width(15), GUILayout.ExpandWidth(false))) {
                versionInfoProperty++;
            }
            if (GUILayout.Button("-", style, GUILayout.Width(15), GUILayout.ExpandWidth(false))) {
                versionInfoProperty--;
            }
            value = EditorGUILayout.IntField(versionInfoProperty);
            EditorGUILayout.EndHorizontal();

            if (value < 0) {
                value = 0;
            }

            if (value != versionInfoProperty) {
                versionInfoProperty = value;
            }

            return versionInfoProperty;
        }

        private void SyncValues() {
            major = VersionInfo.Version.Major;
            minor = VersionInfo.Version.Minor;
            customVersionName = VersionInfo.customVersionName;
        }

        private void ChangeAllPropertiesInVersionInfo() {
            ChangeScriptFile((contents) => {
                ChangeMajorAndMinor(ref contents, major, minor);
                ChangeCustomVersionName(ref contents, customVersionName);
                return contents;
            });
        }

        private static void ChangeScriptFile(Func<string, string> changeLogic) {
            // convert GUID of VersionInfo.cs into asset path
            string assetPath = AssetDatabase.GUIDToAssetPath("79f354a7e0154184d86d0903250f430a");
            string contents = File.ReadAllText(assetPath);
            File.WriteAllText(assetPath, changeLogic(contents));
            AssetDatabase.Refresh();
        }

        private static void ChangeMajorAndMinor(ref string contents, int major, int minor) {
            if (major != VersionInfo.Version.Major || minor != VersionInfo.Version.Minor) {
                string template = "AssemblyVersion(\"{0}.{1}.*\")";
                string oldVersion = string.Format(template, VersionInfo.Version.Major, VersionInfo.Version.Minor);
                string newVersion = string.Format(template, major, minor);
                contents = contents.Replace(oldVersion, newVersion);
            }
        }

        private static void ChangeCustomVersionName(ref string contents, string customVersionName) {
            if (customVersionName != VersionInfo.customVersionName) {
                string template = "string customVersionName = \"{0}\";";
                string oldName = string.Format(template, VersionInfo.customVersionName);
                string newName = string.Format(template, customVersionName);
                contents = contents.Replace(oldName, newName);
            }
        }

        protected override void OnGUI() {
            centeredStyle = AlignStyle(EditorStyles.label, TextAnchor.MiddleLeft);
            DrawHeader();
            EditorGUILayout.LabelField("Current Version: " + VersionInfo.VersionCode, centeredStyle);
            EditorGUILayout.Space();

            GUI.enabled = false;
            EditorGUILayout.TextField("Version Name", VersionInfo.VersionName);
            GUI.enabled = true;
            customVersionName = EditorGUILayout.TextField("Custom Version Name", customVersionName);
            major = DrawAndSetVersionInfoProperty("Major", major);
            minor = DrawAndSetVersionInfoProperty("Minor", minor);

            GUI.enabled = false;
            EditorGUILayout.IntField(new GUIContent("Build", "Automatically incremented"), VersionInfo.Version.Build);
            EditorGUILayout.IntField(new GUIContent("Revision", "Automatically incremented"), VersionInfo.Version.Revision);
            EditorGUILayout.IntField(new GUIContent("Build Counter", "Automatically incremented"), VersionInfo.BuildCounter);
            GUI.enabled = true;

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("Suggestion:\r\nMajor Version: Prototype = 0, Alpha = 0, Beta = 0, GM = 1" +
            "\r\nMinor Version: Milestone = 1..11" +
            "\r\nBuild Number: Auto incrementing on every build", MessageType.Info);

            if (major != VersionInfo.Version.Major || minor != VersionInfo.Version.Minor || customVersionName != VersionInfo.customVersionName) {
                if (GUILayout.Button("Save Changes")) {
                    ChangeAllPropertiesInVersionInfo();
                }
            }

        }

        protected override void OnEnable() {
            SyncValues();
        }
    }
}
