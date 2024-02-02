using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using static EditorHelper.UI;
using System.Reflection;

namespace BuildManager {
    public class BuildTargetGroupHelper {
        public string name;
        public ToggableButton button = null;
        public BuildTargetGroup group;
        public Texture2D icon;
    }

    public class TargetGroupModule : IModuleBase {
        private static string[] searchBuiltInIconPatterns = new string[] { "d_BuildSettings.{0}.Small", "BuildSettings.{0}.Small", "d_BuildSettings.{0}", "BuildSettings.{0}" };
        private Dictionary<string, BuildTargetGroupHelper> targetGroupLookUp = new Dictionary<string, BuildTargetGroupHelper>();
        private string companyNameTmp, productNameTmp, productNameCache, activeBuildTargetGroupName = null;
        public bool targetGroupValidAndChanged = false;
        public BuildTargetGroupHelper activeTargetGroup = null;
        public string companyName, productName;
        private bool targetGroupfoldout;
        private ToggableEditorPrefsManagedItemList targetFoldoutArea = null;

        public TargetGroupModule() {
            companyName = PlayerSettings.companyName;
            productName = PlayerSettings.productName;
            targetFoldoutArea = new ToggableEditorPrefsManagedItemList("targetGroupModule.targetGroupfoldout");
            InitializeTargetGroupLookUp();
        }

        void InitializeTargetGroupLookUp() {
            foreach (BuildTargetGroup targetGroup in Enum.GetValues(typeof(BuildTargetGroup))) {
                if (targetGroup == BuildTargetGroup.Unknown) {
                    continue;
                }

                ObsoleteAttribute validTargetGroup = typeof(BuildTargetGroup).GetField(targetGroup.ToString()).GetCustomAttribute<ObsoleteAttribute>(false);
                if (validTargetGroup != null) {
                    continue;
                }

                string targetGroupName = Enum.GetName(typeof(BuildTargetGroup), targetGroup);
                if (!targetGroupLookUp.ContainsKey(targetGroupName)) {
                    BuildTargetGroupHelper targetGroupObject = new BuildTargetGroupHelper();
                    targetGroupObject.group = targetGroup;
                    targetGroupObject.name = targetGroupName;
                    foreach (string searchPattern in searchBuiltInIconPatterns) {
                        Texture2D icon = EditorGUIUtility.FindTexture(string.Format(searchPattern, targetGroup));
                        if (icon != null) {
                            targetGroupObject.icon = icon;
                            break;
                        }
                    }
                    targetGroupObject.button = new EditorHelper.UI.ToggableButton(targetGroupName, targetGroupObject.icon);
                    targetGroupLookUp.Add(targetGroupName, targetGroupObject);
                    if (EditorHelper.Utility.CurrentBuildTargetName.Contains(targetGroupName)) {
                        targetGroupObject.button.active = true;
                        ChangeActiveTargetGroup(targetGroupName);
                    }
                }
            }
        }

        void ManageBuildTargetSelection(BuildTargetGroupHelper targetGroup) {
            bool clicked = false;
            if (EditorHelper.Utility.CurrentBuildTargetName.Contains(targetGroup.name)) {
                clicked = targetGroup.button.Draw(Color.green, GUILayout.MaxWidth(80));
            } else {
                clicked = targetGroup.button.Draw(GUILayout.MaxWidth(80));
            }

            if (clicked && targetGroup.button.active) {
                ChangeActiveTargetGroup(targetGroup.name);
                foreach (var buildTargetGroup in targetGroupLookUp) {
                    if (targetGroup.name != buildTargetGroup.Key) {
                        buildTargetGroup.Value.button.active = false;
                    }
                }
            }
        }

        void ChangeActiveTargetGroup(string name) {
            activeBuildTargetGroupName = name;
            activeTargetGroup = targetGroupLookUp[activeBuildTargetGroupName];
        }

        public void Draw() {
            string oldActiveKey = activeBuildTargetGroupName;
            DrawInactiveLabelField("Current Build Target: " + EditorHelper.Utility.CurrentBuildTargetName);
            EditorGUILayout.Space();
            companyNameTmp = EditorGUILayout.TextField("Company", companyName);
            companyName = companyNameTmp != companyName ? companyNameTmp : companyName;
            productNameTmp = EditorGUILayout.TextField("App Name", productName);
            productName = productNameTmp != productName ? productNameTmp : productName;
            GUI.enabled = false;
            EditorGUILayout.LabelField("App Identifier", PlayerSettings.GetApplicationIdentifier(activeTargetGroup.group));
            GUI.enabled = true;
            if (GUILayout.Button("Save Changes to Identifiers")) {
                Save();
            }

            targetFoldoutArea.Draw<KeyValuePair<string, BuildTargetGroupHelper>>(
                targetGroupLookUp,
                "Build Target Groups",
                (_, i) => ManageBuildTargetSelection(_.Value)
            );

            // is the target group present and did it change?
            if (oldActiveKey != activeBuildTargetGroupName && targetGroupLookUp.ContainsKey(activeBuildTargetGroupName)) {
                targetGroupValidAndChanged = true;
            } else {
                targetGroupValidAndChanged = false;
            }
        }

        public void Save() {
            PlayerSettings.companyName = companyNameTmp;
            PlayerSettings.productName = productNameTmp;
            string bundleIdentifier = EditorHelper.Utility.CreateValidBundleIdentifier(companyName, productName);
            foreach (KeyValuePair<string, BuildTargetGroupHelper> buildTargetGroup in targetGroupLookUp) {
                PlayerSettings.SetApplicationIdentifier(buildTargetGroup.Value.group, bundleIdentifier);
            }
        }

        public void OverwriteProductName(string newName) {
            productNameCache = string.Empty;

            if (string.IsNullOrEmpty(newName)) {
                return;
            }

            productNameCache = productNameTmp;
            productNameTmp = newName;
            Save();
        }

        public void ResetProductName() {
            if (string.IsNullOrEmpty(productNameCache)) {
                return;
            }

            productNameTmp = productNameCache;
            Save();
        }
    }
}
