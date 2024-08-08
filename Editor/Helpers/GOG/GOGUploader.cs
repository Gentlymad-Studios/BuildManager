using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Settings = BuildManager.BuildManagerSettings;
using System.Linq;
using System;

namespace BuildManager {

    public class GOGUploader {

        private readonly string appIDKey, messageKey;
        private GOGGalaxySettings.GOGGalaxyAppConfig selectedAppConfig;
        private List<string> appConfigNames = new List<string>();
        private string productID;

        private bool initialized = false;
        private GUIStyle style;
        private EditorHelper.UI.EditorPrefsManagedFoldoutArea foldoutArea = null;

        private GOGProcess process = new GOGProcess();

        private void SetupStyles() {
            if (!initialized) {
                initialized = true;
                style = new GUIStyle(GUI.skin.label);
                style.wordWrap = true;
            }
        }

        public GOGUploader(string editorPrefsPrefix) {
            appIDKey = editorPrefsPrefix + ".GOGProductIDKey";
            foldoutArea = new EditorHelper.UI.EditorPrefsManagedFoldoutArea(OnFoldoutArea, editorPrefsPrefix + ".foldOutKey", true, "GOG Specific Options");
            UpdateSelectedAppConfig(true);
        }

        public void UpdateSelectedAppConfig(bool forceUpdate = false, int tempAppID = -1) {
            productID = tempAppID == -1 ? EditorPrefs.GetString(appIDKey, null) :  tempAppID.ToString();
            for (int i = 0; i < Settings.GOGGalaxy.appConfigs.Count; i++) {
                if (Settings.GOGGalaxy.appConfigs[i].productID == productID) {
                    selectedAppConfig = Settings.GOGGalaxy.appConfigs[i];
                    string oldID = GOGGalaxyClientIDAndSecret.ProductID;
                    GOGGalaxyClientIDAndSecret.ProductID = productID;
                    GOGGalaxyClientIDAndSecret.ClientID = selectedAppConfig.clientID;
                    GOGGalaxyClientIDAndSecret.ClientSecret = selectedAppConfig.clientSecret;
                    return;
                }
            }
            selectedAppConfig = null;
        }

        public void OnFoldoutArea() {
            bool guienabled = GUI.enabled;
            GUI.enabled = !process.processing;

            // extract and display all available app configs
            appConfigNames.Clear();
            int index = 0;
            for (int i = 0; i < Settings.GOGGalaxy.appConfigs.Count; i++) {
                appConfigNames.Add(Settings.GOGGalaxy.appConfigs[i].name);
                if (Settings.GOGGalaxy.appConfigs[i].productID == productID) {
                    index = i;
                }
            }
            index = EditorGUILayout.Popup("Application", index, appConfigNames.ToArray());
            EditorPrefs.SetString(appIDKey, Settings.GOGGalaxy.appConfigs[index].productID);
            UpdateSelectedAppConfig();
            GUI.enabled = guienabled;
        }

        public void Draw() {
            SetupStyles();
            foldoutArea.Draw();
        }

        /// <summary>
        /// Execute DLC ONLY upload
        /// </summary>
        public void UploadOnlyDLCs() {
            StartUploadProcess(null);
        }

        /// <summary>
        /// Execute of existing builds in builds folder
        /// </summary>
        public void UploadExisting() {
            StartUploadProcess(Settings.GetSuccessfulBuildTargets(DistributionPlatform.GOG));
        }

        /// <summary>
        /// Execute a regular Upload containing newly created builds
        /// </summary>
        /// <param name="targets"></param>
        public void UploadDefault(SuccessfulBuildTargets targets) {
            Debug.Log("[GOG: Upload Default] " + targets != null ? string.Join(",", targets) : "NULL!");
            if (targets != null && targets.Count > 0) {
                StartUploadProcess(targets);
            }
        }

        /// <summary>
        /// Retruns the selected AppConfig
        /// </summary>
        /// <returns></returns>
        public GOGGalaxySettings.GOGGalaxyAppConfig GetSelectedAppConfig() {
            return selectedAppConfig;
        }

        /// <summary>
        /// Base Method to manage uploading to Gog
        /// </summary>
        /// <param name="targets"></param>
        private void StartUploadProcess(SuccessfulBuildTargets targets = null) {
            UpdateSelectedAppConfig();
            if (selectedAppConfig == null) {
                Debug.Log("No application selected");
            } else {
                Settings.CacheDataPath();
                Settings.CacheStreamingAssetsPath();
                string version = BuildManagerRuntimeSettings.Instance.VersionCode;
                process.StartProcess(selectedAppConfig, version, PlayerSettings.productName, targets);
            }
        }

        /// <summary>
        /// Base Method to manage uploading to Gog headless
        /// </summary>
        /// <param name="targets"></param>
        public void UploadHeadless(SuccessfulBuildTargets targets, int appID) {
            System.Console.WriteLine("[GOG: Upload Default] " + targets != null ? string.Join(",", targets) : "NULL!");
            System.Console.WriteLine($"##### Start Upload: {DateTime.Now.ToString("HH:mm:ss")} #####");
            DateTime startTime = DateTime.Now;

            if (targets != null && targets.Count > 0) {
                UpdateSelectedAppConfig(tempAppID: appID);
                if (selectedAppConfig == null) {
                    Debug.Log("No application selected");
                    HeadlessBuild.WriteToProperties("Error", "30");
                    EditorApplication.Exit(30);
                } else {
                    Settings.CacheDataPath();
                    Settings.CacheStreamingAssetsPath();
                    string version = BuildManagerRuntimeSettings.Instance.VersionCode;
                    process.StartProcessHeadless(selectedAppConfig, version, PlayerSettings.productName, targets);
                }
            } else {
                HeadlessBuild.WriteToProperties("Error", "20");
                EditorApplication.Exit(20);
            }

            System.Console.WriteLine($"##### Finished Upload: {DateTime.Now.ToString("HH:mm:ss")} ##### Uploadtime: {DateTime.Now.Subtract(startTime)} #####");
            HeadlessBuild.WriteToProperties("UploadTime", DateTime.Now.Subtract(startTime).ToString(@"mm\:ss"));
        }
    }
}