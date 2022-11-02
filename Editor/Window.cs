using UnityEngine;
using UnityEditor;
using System.IO;
using Settings = BuildManager.BuildManagerSettings;
using System.Collections.Generic;

namespace BuildManager {
    public class Window : EditorHelper.UI.WindowBase {
        private EditorHelper.UI.ExpandedScrollView scrollView = null;
        private TargetGroupModule targetGroupModule = null;
        private DefineModule defineModule = null;
        private BuildProcessModule buildProcessModule = null;
        private GUIStyle btnStyle = null;
        private bool initialized = false;
        private static Window window;

        readonly List<string> deleteDirectoryExceptions = new List<string>(){ "Addon" };

        private void SetupStyles() {
            if (!initialized) {
                btnStyle = new GUIStyle(GUI.skin.button);
                btnStyle.padding = new RectOffset(0, 0, 0, 0);
                btnStyle.margin = new RectOffset(3, 3, 3, 3);

                initialized = true;
            }
        }
        public void DeleteFilesAndFoldersOfBuildsFolder(){
            DirectoryInfo di = new DirectoryInfo(Settings.General.paths.BuildsFolder);

            if (di.Exists) {
                foreach (FileInfo file in di.GetFiles()) {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories()) {
                    if (deleteDirectoryExceptions.Contains(dir.Name)) continue;
                    dir.Delete(true);
                }
            }
        }

        public static void RepaintWindow() {
            if (window == null) {
                window = GetWindow(typeof(Window)) as Window;
            }
            if (window != null) {
                window.Repaint();
            }
        }

        [MenuItem("Tools/Build Manager", priority = 20)]
        protected static void OnWindow(){
            window = EditorHelper.UI.GetOrCreateWindowWithTitle<Window>("Build Manager");
        }

        protected override void OnEnable(){
            scrollView = new EditorHelper.UI.ExpandedScrollView(OnScrollView);
            targetGroupModule = new TargetGroupModule();
            defineModule = new DefineModule(targetGroupModule);
            buildProcessModule = new BuildProcessModule(targetGroupModule);
        }

        void OnScrollView(){
            DrawHeader();

            targetGroupModule.Draw();
            defineModule.Draw();
            buildProcessModule.Draw();
        }


        protected override void OnGUI(){
            SetupStyles();
            scrollView.Update();
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(50));
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(120), GUILayout.Height(50));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Build", btnStyle, GUILayout.Height(40))) {
                defineModule.Save();
                DeleteFilesAndFoldersOfBuildsFolder();
                buildProcessModule.Save();
                if (Event.current != null)
                    GUIUtility.ExitGUI();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical(GUILayout.Height(50));
            GUILayout.FlexibleSpace();
            if (buildProcessModule.IsAnyPipeEnabled) {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button((position.width < 350 ? "" : "Upload ")+"Existing", btnStyle, GUILayout.Height(18))) {
                    buildProcessModule.UploadExisting(false);
                }
                if (GUILayout.Button((position.width < 350 ? "" : "Upload ")+"Only DLCs", btnStyle, GUILayout.Height(18))) {
                    buildProcessModule.UploadExisting(true);
                }
                EditorGUILayout.EndHorizontal();
            } else {
                if (buildProcessModule.IsMagentaEnabled) {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button((position.width < 350 ? "" : "Upload ") + "Existing", btnStyle, GUILayout.Height(18))) {
                        buildProcessModule.UploadExisting(false);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (GUILayout.Button("Clear Builds Folder", btnStyle, GUILayout.Height((buildProcessModule.IsAnyPipeEnabled || buildProcessModule.IsMagentaEnabled) ? 18 : 40) )) {
                DeleteFilesAndFoldersOfBuildsFolder();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
    }
}

