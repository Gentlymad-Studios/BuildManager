using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace BuildManager {
    public class SteamWebApiGetBranches {
        [DataContract]
        public class ApiResponse {
            [DataContract]
            public class AppBranches {
                [DataMember(Name = "betas")]
                public Dictionary<string, AppBranch> Betas { get; set; }
            }

            [DataContract]
            public class AppBranch {
                [DataMember(Name = "Description")]
                public string Description { get; set; }
            }

            [DataMember(Name = "response")]
            public AppBranches Response { get; set; }
        }

        public string GetSteamBetaBranchesURI = "https://partner.steam-api.com/ISteamApps/GetAppBetas/v1/";
        public string[] betaBranchNames = new string[] { defaultBetaBranchName };
        public static string defaultBetaBranchName = "None";
        public UnityWebRequest webRequest;
        public bool alreadyGettingBranches = false;
        public bool branchNamesInitialized = false;
        public int branchIndex = 0;
        public string branchKey = "steamWebApiGetBetas.branchIndex";
        public string[] DefaultBranchNameList = new string[] { defaultBetaBranchName };

        public int PrefsManagedPopup(string label, string[] options, string key, int defaultValue = 0) {
            int index = EditorPrefs.GetInt(key, defaultValue);
            index = (index < 0 || index >= options.Length) ? defaultValue : index;
            index = EditorGUILayout.Popup(label, index, options);
            EditorPrefs.SetInt(key, index);
            return index;
        }

        public void Draw() {
            bool guienabled = GUI.enabled;
            if (IsValidForDisplay()) {
                branchIndex = PrefsManagedPopup("Branch", betaBranchNames, branchKey);
            } else {
                GUI.enabled = false;
                EditorGUILayout.Popup("Branch", 0, DefaultBranchNameList);
                GUI.enabled = guienabled;
            }
        }

        public bool IsValidForDisplay() {
            return branchNamesInitialized && !alreadyGettingBranches;
        }

        public string GetSanitizedBranchName() {
            string branchName = branchIndex < betaBranchNames.Length && branchIndex > 0 ? betaBranchNames[branchIndex] : "";
            branchName = branchName == defaultBetaBranchName ? "" : branchName;
            return branchName;
        }

        public void UpdateBetaBranches(int appID) {
            branchIndex = 0;
            betaBranchNames = new string[] { defaultBetaBranchName };

            if (!alreadyGettingBranches) {
                alreadyGettingBranches = true;

                string urlParams = "?key=" + BuildManagerSettings.Steam.PublisherAPIKey;
                urlParams += "&appid=" + appID;
                webRequest = UnityWebRequest.Get(GetSteamBetaBranchesURI + urlParams);
                webRequest.SendWebRequest();

                void UpdateFunc() {
                    if (webRequest.isDone) {
                        if (!webRequest.isNetworkError) {
                            try {
                                ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(webRequest.downloadHandler.text);
                                if (response != null && response.Response != null && response.Response.Betas != null) {
                                    List<string> branchNames = new List<string>() { defaultBetaBranchName };
                                    branchNames.AddRange(response.Response.Betas.Keys.Where(_ => _ != "public"));
                                    betaBranchNames = branchNames.ToArray();
                                }
                            } catch {
                                branchNamesInitialized = false;
                                alreadyGettingBranches = false;
                                EditorApplication.update -= UpdateFunc;
                                Debug.Log($"Unable to request branches for Steam AppId {appID}.");
                                return;
                            }
                        } else {
                            Debug.Log("error" + webRequest.downloadHandler.text);
                        }
                        branchNamesInitialized = true;
                        alreadyGettingBranches = false;
                        EditorApplication.update -= UpdateFunc;
                    }
                }

                EditorApplication.update -= UpdateFunc;
                EditorApplication.update += UpdateFunc;
            }
        }
    }
}
