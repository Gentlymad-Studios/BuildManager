using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace BuildManager {
    public class DefineModule : TargetGroupDependentModuleBase {
        private EditorHelper.UI.ListField<string> definesList = null;
        private EditorHelper.UI.EditorPrefsManagedFoldoutArea foldoutArea = null;

        List<string> defaultDefines = new List<string>() { "STEAM", "DISABLESTEAMWORKS", "GOGGALAXY" };

        public DefineModule(TargetGroupModule targetGroupModule) : base(targetGroupModule) {
            CreateDefinesList();
            foldoutArea = new EditorHelper.UI.EditorPrefsManagedFoldoutArea(OnFoldoutArea, "defineModule.targetFoldout");
        }

        private void OnFoldoutArea() {
            definesList.Draw();
            if (GUILayout.Button("Save Changes to Defines")) {
                Save();
            }
        }

        public override void Draw() {
            if (targetGroupModule.targetGroupValidAndChanged) {
                CreateDefinesList();
            } else if (definesList != null) {
                foldoutArea.Draw("Defines for [" + targetGroupModule.activeTargetGroup.name + "]");
            }
        }

        public override void Save() {
            string defineString = "";
            definesList.items.ForEach((_) => defineString += (!string.IsNullOrEmpty(_)) ? (_ + ";") : "");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroupModule.activeTargetGroup.group, defineString);
        }

        void CreateDefinesList() {
            string[] defines = EditorHelper.Utility.GetDefinesForTargetGroup(targetGroupModule.activeTargetGroup.group);
            List<string> defineList = defines.ToList();
            defineList.RemoveAll(_ => string.IsNullOrEmpty(_));
            definesList = new EditorHelper.UI.ListField<string>(defineList, "", true);

            definesList.OnAdd = (l) => {
                definesList.items.Add("");
            };

            definesList.OnDraw = (rect, index, isActive, isFocused) => {
                rect.y += 2;
                if (index < 0) { return; }
                definesList.DrawIndex(rect, index);

                string dataDefineName = definesList.items[index];
                bool dataActive = true;
                if (dataDefineName.StartsWith("*")) {
                    dataActive = false;
                    dataDefineName = dataDefineName.Substring(1);
                }

                string defineName = EditorGUI.TextField(new Rect(rect.x + 20, rect.y, rect.width - 60, EditorGUIUtility.singleLineHeight), dataDefineName);
                bool active = EditorGUI.Toggle(new Rect(rect.width + 10, rect.y - 2, 20, EditorGUIUtility.singleLineHeight), dataActive);

                if (defineName != dataDefineName || active != dataActive) {
                    definesList.items[index] = !active ? ("*" + defineName) : defineName;
                }
            };
        }

        List<string> AddDefaultDefines(List<string> defineList) {
            foreach (string define in defaultDefines) {
                if (!defineList.Contains(define) && !defineList.Contains("*"+define)) {
                    defineList.Add("*"+define);
                }
            }

            return defineList;
        }
    }
}
