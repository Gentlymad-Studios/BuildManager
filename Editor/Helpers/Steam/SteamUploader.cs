using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Settings = BuildManager.BuildManagerSettings;
using System;

namespace BuildManager{

    public class SteamUploader {

        private readonly string appIDKey, messageKey;
        private SteamSettings.SteamAppConfig selectedAppConfig;
        private List<string> appConfigNames = new List<string>();
        private int appID;
        private string branchName;

        private bool initialized = false;
        private GUIStyle style;
        private EditorHelper.UI.EditorPrefsManagedFoldoutArea foldoutArea = null;

        private SteamWebApiGetBranches getBranches = new SteamWebApiGetBranches();
        private SteamProcess process = new SteamProcess();

        const string messageAppConfigMissing = "No Steam app config were found.\nPlease set up the BuildManagerSettings correctly.";
        const string messageCredentialsMissing = "No Steam Credentials was given.\nPlease set up the BuildManagerSettings correctly.";

        private void SetupStyles() {
            if (!initialized) {
                initialized = true;
                style = new GUIStyle(GUI.skin.label);
                style.wordWrap = true;
            }
        }

        public SteamUploader(string editorPrefsPrefix){
            appIDKey = editorPrefsPrefix + ".appIDKey";
                        messageKey = editorPrefsPrefix + ".buildmessage";
            foldoutArea = new EditorHelper.UI.EditorPrefsManagedFoldoutArea(OnFoldoutArea, editorPrefsPrefix + ".foldOutKey", true,  "Steam Specific Options");
            UpdateSelectedAppConfig(true);
        }

        private void UpdateSelectedAppConfig(bool forceUpdate = false, int tempAppID = -1) {
            appID = tempAppID < 0 ? EditorPrefs.GetInt(appIDKey, 0) : tempAppID;
            for (int i = 0; i < Settings.Steam.appConfigs.Count; i++) {
                if (Settings.Steam.appConfigs[i].appID == appID) {
                    selectedAppConfig = Settings.Steam.appConfigs[i];
                    int oldSteamID = SteamAppID.AppID; 
                    SteamAppID.AppID = appID;
                    if (forceUpdate || oldSteamID != appID) {
                        if (!string.IsNullOrEmpty(Settings.Steam.PublisherAPIKey) && !Settings.Steam.PublisherAPIKey.StartsWith("<add")) {
                            getBranches.UpdateBetaBranches(appID);
                        }
                    }
                    return;
                }
            }
            selectedAppConfig = null;
        }

        public void OnFoldoutArea() {
            bool guienabled = GUI.enabled;
            GUI.enabled = !process.processing;

            if (string.IsNullOrEmpty(Settings.Steam.PublisherAPIKey) || Settings.Steam.PublisherAPIKey.StartsWith("<add") ||
                string.IsNullOrEmpty(Settings.Steam.BuildAccountName) || Settings.Steam.BuildAccountName.StartsWith("<add") ||
                string.IsNullOrEmpty(Settings.Steam.BuildAccountPassword) || Settings.Steam.BuildAccountPassword.StartsWith("<add")) {
                EditorGUILayout.HelpBox(messageCredentialsMissing, MessageType.Error, true);
                return;
            }

            if (Settings.Steam.appConfigs.Count < 1) {
                EditorGUILayout.HelpBox(messageAppConfigMissing, MessageType.Error, true);
                return;
            }

            // extract and display all available app configs
            appConfigNames.Clear();
            int index = 0;
            for (int i=0; i<Settings.Steam.appConfigs.Count; i++) {
                appConfigNames.Add(Settings.Steam.appConfigs[i].name);
                if (Settings.Steam.appConfigs[i].appID == appID) {
                    index = i;
                }
            }
            index = EditorGUILayout.Popup("Application", index, appConfigNames.ToArray());

            EditorPrefs.SetInt(appIDKey, Settings.Steam.appConfigs[index].appID);
            UpdateSelectedAppConfig();

            // draw beta branches
            getBranches.Draw();

            // steam message dialog
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Steam Build Message", EditorHelper.UI.boldLabelStyle);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            bool guistate = GUI.enabled;
            GUI.enabled = false;
            EditorGUILayout.TextArea(CreateStandardBuildMessage(""), style, GUILayout.MaxWidth(350));
            GUI.enabled = guistate;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Custom Message");
            string steamBuildMessage = EditorGUILayout.TextArea(EditorPrefs.GetString(messageKey, ""), GUILayout.Height(60));
            EditorPrefs.SetString(messageKey, steamBuildMessage);
            EditorGUILayout.EndVertical();
            GUI.enabled = guienabled;
            EditorGUILayout.EndVertical();
        }

        public void Draw(){
            SetupStyles();
            foldoutArea.Draw();
        }

        public string CreateStandardBuildMessage(string buildMessage) {
            string message = "";
            // Add relevant build information
            message += "[ ";
            message += "DateAndTime : " + VersionInfo.BuildTimestamp + ", ";
            message += "Version: " + VersionInfo.VersionCode + ", ";
            message += "Tag: " + VersionInfo.VersionName + ", ";
            message += "AppID: " + SteamAppID.AppID + ", ";
            message += "GitHash: " + VersionInfo.GitHash + ", ";
            message += "BuildTargets: {" + buildMessage+ "}";
            message += " ]";
            return message;
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
            StartUploadProcess(Settings.GetSuccessfulBuildTargets(DistributionPlatform.Steam));
        }

        /// <summary>
        /// Execute a regular Upload containing newly created builds
        /// </summary>
        /// <param name="targets"></param>
        public void UploadDefault(SuccessfulBuildTargets targets) {
            Debug.Log("[Steam: Upload Default] "+ ((targets != null && targets.builds != null) ? string.Join(",", targets.GetBuildTargets()) : "NULL!"));
            if (targets != null && targets.Count > 0) {
                StartUploadProcess(targets);
            }
        }

        /// <summary>
        /// Base Method to manage uploading to steam headless
        /// </summary>
        /// <param name="targets"></param>
        /// <param name="appID"></param>
        public void UploadHeadless(SuccessfulBuildTargets targets, int appID) {
            foreach (var build in targets.builds) {
                System.Console.WriteLine(build.buildTarget);
            }

            System.Console.WriteLine("[Steam: Upload Default] " + ((targets != null && targets.builds != null) ? string.Join(",", targets.GetBuildTargets()) : "NULL!"));
            System.Console.WriteLine($"##### Start Upload: {DateTime.Now.ToString("HH:mm:ss")} #####");
            DateTime startTime = DateTime.Now;

            if (targets != null && targets.Count > 0) {

                System.Console.WriteLine("targets != null && targets.Count > 0");

                UpdateSelectedAppConfig(tempAppID: appID);
                if (selectedAppConfig == null) {
                    Debug.Log("No application selected");
                    HeadlessBuild.WriteToProperties("Error", "30");
                    EditorApplication.Exit(30);
                } else {

                    System.Console.WriteLine("AppConfig: " + selectedAppConfig);

                    Settings.CacheDataPath();
                    Settings.CacheStreamingAssetsPath();
                    var buildMessage = CreateStandardBuildMessage(targets == null ? "DLCUpload" : string.Join(",", targets));
                    process.StartProcessHeadless(selectedAppConfig, targets.distributionBranch, buildMessage, targets);
                }
            } else {
                HeadlessBuild.WriteToProperties("Error", "20");
                EditorApplication.Exit(20);
            }

            System.Console.WriteLine($"##### Finished Upload: {DateTime.Now.ToString("HH:mm:ss")} ##### Uploadtime: {DateTime.Now.Subtract(startTime)} #####");
            HeadlessBuild.WriteToProperties("UploadTime", DateTime.Now.Subtract(startTime).ToString(@"mm\:ss"));
        }

        /// <summary>
        /// Base Method to manage uploading to steam
        /// </summary>
        /// <param name="targets"></param>
        private void StartUploadProcess(SuccessfulBuildTargets targets = null) {
            if (!getBranches.alreadyGettingBranches) {
                UpdateSelectedAppConfig();
                if (selectedAppConfig == null) {
                    Debug.Log("No application selected");
                } else {
                    Settings.CacheDataPath();
                    Settings.CacheStreamingAssetsPath();
                    GetBranchName();
                    var buildMessage = CreateStandardBuildMessage(targets == null ? "DLCUpload" : string.Join(",", targets));
                    process.StartProcess(selectedAppConfig, branchName, buildMessage, targets);
                }
            }
        }

        public string GetBranchName() {
            branchName = getBranches.GetSanitizedBranchName();
            return branchName;
        }
    }
}
