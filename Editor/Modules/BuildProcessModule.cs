using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static EditorHelper.UI;
using UnityEditor.Build.Reporting;
using System.IO;

namespace BuildManager {

    public class BuildProcessModule : TargetGroupDependentModuleBase {

        private const string exclusionKey = "BuildManager_RunExclusion";
        private const string sendMailKey = "BuildManager_SendMail";
        private const string buildAddressableBundles = "BuildManager_BuildAddressableBundles";
        //private const string backupBundlesBeforeBuild = "BuildManager_BackupBundlesBeforeBuild";

        public static int[] deprecatedBuildTargets = new int[] { 3, 4, 6, 27 };

        private SortedDictionary<string, BuildTargetHelper> targetLookUp = new SortedDictionary<string, BuildTargetHelper>();
        private SteamUploader steamPipe;
        private GOGUploader gogPipe;
        private MagentaUploader magentaUpload;
        private MailProcess mailProcess;
        private ToggableEditorPrefsManagedItemList targetFoldoutArea = null;
        private EditorPrefsManagedFoldoutArea miscFoldoutArea = null;

        private GUIStyle radioButton = null;
        private SteamSettings.SteamAppConfig selectedSteamConfig = null;
        private GOGGalaxySettings.GOGGalaxyAppConfig selectedGOGConfig = null;
        private List<SteamSettings.SteamAppConfig> steamConfigs;
        private List<GOGGalaxySettings.GOGGalaxyAppConfig> gogConfigs;
        private GOGGalaxySettings.PlatformConfig gogPlatformConfig;

        public SuccessfulBuildTargets succeededBuildTargets = new SuccessfulBuildTargets();
        private string lastBuildTargetPath = null;

        enum BuildBundlesFilter {
            [Tooltip("No bundles will be built.")]
            None,
            [Tooltip("Bundles will be built for those selected platforms that no bundles can be found for.")]
            Missing,
            [Tooltip("Bundles for all selected platforms will be built.")]
            All,
        };

        const string messageBundlesFoundForAllPlatforms = "Addressable bundles were found for all selected platforms: {0}.\nIf you are sure these bundles are up-to-date, you can select 'None' or 'Missing' for 'Build Bundles' to not have them built again.";
        const string messageBundlesFoundForSomePlatforms = "Addressable bundles were found for some selected platforms: {0}. Missing: {1}.\nIf you are sure the existing bundles are up-to-date, select 'Missing' for the 'Build Bundles' to only build the missing bundles.";
        const string messageBundlesFoundForNoPlatforms = "No addressable bundles were found for the selected platforms.\nAll bundles need to be built.";

        public BuildProcessModule(TargetGroupModule targetGroupModule) : base(targetGroupModule) {
            steamPipe = new SteamUploader(TypeName);
            gogPipe = new GOGUploader(TypeName);
            magentaUpload = new MagentaUploader(TypeName);
            mailProcess = new MailProcess();
            CreateTargetLookUp(); 
            targetFoldoutArea = new ToggableEditorPrefsManagedItemList("buildProcessModule.targetFoldout");
            miscFoldoutArea = new EditorPrefsManagedFoldoutArea(OnMiscFoldout, "buildProcessModule.miscFoldout", false, "Misc Build Options");
        }

        private void OnMiscFoldout() {
            EditorUserBuildSettings.development = EditorGUILayout.Toggle("Development Build", EditorUserBuildSettings.development);
            EditorUserBuildSettings.allowDebugging = EditorGUILayout.Toggle("Allow script debugging", EditorUserBuildSettings.allowDebugging);
            if (!IsMagentaEnabled) {
                EditorPrefs.SetBool(exclusionKey, EditorGUILayout.Toggle("Exclude Unwanted Files", EditorPrefs.GetBool(exclusionKey, true)));
            }
            EditorPrefs.SetBool(sendMailKey, EditorGUILayout.Toggle("Send Mail", EditorPrefs.GetBool(sendMailKey, false)));
        }

        string BuildTargetListToString(List<BuildTarget> targets) {

            string result = "";
            int targetCount = targets.Count;
            if (targetCount > 0) {
                result = targets[0].ToString();

                for (int i = 1; i < targetCount; ++i) {

                    result += ", " + targets[i].ToString();
                }
            }
            return result;
        }

        void CreateTargetLookUp() {
            targetLookUp.Clear();
            BuildTarget[] buildTargets = Enum.GetValues(typeof(BuildTarget)) as BuildTarget[];
            buildTargets = buildTargets.OrderBy(x => x.ToString()).ToArray();
            for (int i = 0; i < buildTargets.Length; i++) {
                BuildTarget target = buildTargets[i];
                string name = target.ToString();
                if (!deprecatedBuildTargets.Contains((int)target) && name.IndexOf(targetGroupModule.activeTargetGroup.name) != -1 && !targetLookUp.ContainsKey(name)) {
                    string nicelyFormattedName = name.Replace(targetGroupModule.activeTargetGroup.name, "");
                    nicelyFormattedName = string.IsNullOrEmpty(nicelyFormattedName) ? name : nicelyFormattedName;

                    ToggableButton button = new ToggableButton(nicelyFormattedName);
                    targetLookUp.Add(name, new BuildTargetHelper {
                        button = button,
                        name = name,
                        target = target
                    });
                }
            }
        }

        public override void Draw() {
            if (radioButton == null) {
                radioButton = new GUIStyle(EditorStyles.radioButton);
                radioButton.fixedHeight = 8;
                radioButton.fixedWidth = 8;
            }

            steamConfigs = BuildManagerSettings.Steam.appConfigs.Where(_ => _.appID == SteamAppID.AppID).ToList();
            if (steamConfigs != null && steamConfigs.Count > 0) {
                selectedSteamConfig = steamConfigs[0];
            }

            gogConfigs = BuildManagerSettings.GOGGalaxy.appConfigs.Where(_ => _.productID == GOGGalaxyClientIDAndSecret.ProductID).ToList();
            if (gogConfigs != null && gogConfigs.Count > 0) {
                selectedGOGConfig = gogConfigs[0];
            }

            if (targetGroupModule.targetGroupValidAndChanged) {
                CreateTargetLookUp();
            } else if (targetLookUp != null) {
                if (!IsMagentaEnabled) {
                    targetFoldoutArea.Draw<KeyValuePair<string, BuildTargetHelper>>(
                        targetLookUp,
                        "Available Targets for [" + targetGroupModule.activeTargetGroup.name + "]",
                        (_, index) => ManageTargetSelection(_.Value),
                        4
                    );
                }

                if (IsAnyPipeEnabled) {
                    if (IsSteamEnabled) {
                        steamPipe.Draw();
                    } else {
                        gogPipe.Draw();
                    }
                } else {
                    /*
                    if (IsMagentaEnabled) {
                        magentaUpload.Draw();
                    }
                    */
                }
            }

            miscFoldoutArea.Draw();
        }


        private void ManageTargetSelection(BuildTargetHelper target) {
            string identifier = TypeName + "." + targetGroupModule.activeTargetGroup.name + "." + target.name;
            target.button.active = EditorPrefs.GetBool(identifier, false);
            target.button.Draw(GUILayout.MaxWidth(100));
            EditorPrefs.SetBool(identifier, target.button.active);
#if STEAM || GOGGALAXY
            if (IsAnyPipeEnabled) {
                Rect rect = GUILayoutUtility.GetLastRect();
                rect = new Rect(rect.x + rect.width - 7, rect.y+3.5f, 4, rect.height-6);
                if (IsSteamEnabled) {
                    if (selectedSteamConfig != null) {
                        if (selectedSteamConfig.buildDepots.Find(_ => _.buildTarget == target.target) != null) {
                            EditorGUI.ColorField(rect, GUIContent.none, Color.green, false, false, false);
                        } else {
                            EditorGUI.ColorField(rect, GUIContent.none, new Color(0.4f, 0.4f, 0.4f), false, false, false);
                        }
                    }
                } else {
                    if (selectedGOGConfig != null) {
                        gogPlatformConfig = selectedGOGConfig.platformConfigs.FirstOrDefault(_ => target.name.ToLower().Contains(_.platform.ToString()));

                        if (gogPlatformConfig != null && gogPlatformConfig.buildDepots.Find(_ => _.buildTarget == target.target) != null) {
                            EditorGUI.ColorField(rect, GUIContent.none, Color.green, false, false, false);
                        } else {
                            EditorGUI.ColorField(rect, GUIContent.none, new Color(0.4f, 0.4f, 0.4f), false, false, false);
                        }
                    }
                }
            }
#endif
        }

        private bool Validate() {
            foreach(var validator in BuildManagerSettings.General.validators) {
                if (!validator.Validate()) {
                    Debug.LogError($"[{validator.GetType()}] some assets are invalid! Build Aborted!");
                    return false;
                }
            }
            return true;
        }

        public void SaveHeadless(DistributionPlatform plattform,bool isDevBuild, BuildTarget buildtarget, string branch, int appID, bool upload = true) {
            BuildPlayerOptions options = new BuildPlayerOptions();

            List<BuildTarget> targets = new List<BuildTarget>();
            targets.Add(buildtarget);

            if (isDevBuild) {
                options.options |= BuildOptions.Development;
            }

            options.targetGroup = targetGroupModule.activeTargetGroup.group;
            options.scenes = (from scene in EditorBuildSettings.scenes where scene.enabled select scene.path).ToArray();

            // increment build version
            VersionInfo.BuildCounter++;

            // get newest git hash
            VersionInfo.UpdateGitHash();

            // update current timestamp
            VersionInfo.UpdateBuildTimestamp();

            // update version code
            VersionInfo.UpdateVersionCode();

            // refresh asset database
            AssetDatabase.Refresh();

            // Set version numbers for all platforms
            PlayerSettings.bundleVersion = VersionInfo.VersionCode;
            PlayerSettings.macOS.buildNumber = VersionInfo.BuildCounter.ToString();
            PlayerSettings.iOS.buildNumber = VersionInfo.BuildCounter.ToString();
            PlayerSettings.Android.bundleVersionCode = VersionInfo.BuildCounter;

            // clear build targets
            if (succeededBuildTargets == null) {
                succeededBuildTargets = new SuccessfulBuildTargets();
            } else if (succeededBuildTargets.builds == null) {
                succeededBuildTargets.builds = new List<SuccessfulBuildTarget>();
            } else if (succeededBuildTargets.builds.Count > 0) {
                succeededBuildTargets.builds.Clear();
            }
            succeededBuildTargets.version = VersionInfo.VersionCode;

            succeededBuildTargets.distributionPlatform = plattform;
            succeededBuildTargets.distributionBranch = branch;


            System.Console.WriteLine($"##### Start to build all targets: {DateTime.Now.ToString("HH:mm:ss")} #####");

            // check if the active build target is in the list of builds to create.
            // if yes, build this first as we don't have to re convert assets for the target platform & speed things up.
            for (int i = 0; i < targets.Count; i++) {
                if (EditorUserBuildSettings.activeBuildTarget == targets[i]) {
                    System.Console.WriteLine("[Build Manager] Building for " + targets[i] + " first!");
                    if (StartBuild(targets[i], BuildManagerSettings.BuildPath, options)) {
                        succeededBuildTargets.Add(targets[i], lastBuildTargetPath);
                    }
                    targets.RemoveAt(i);
                    break;
                }
            }

            // build all other selected targets
            foreach (var target in targets) {
                if (StartBuild(target, BuildManagerSettings.BuildPath, options)) {
                    succeededBuildTargets.Add(target, lastBuildTargetPath);
                }
            }

            System.Console.WriteLine($"##### Finished to build all targets: {DateTime.Now.ToString("HH:mm:ss")} #####");

            // update language depot files
            UpdateLanguageDepots();

            // write build info file
            if (File.Exists(BuildManagerSettings.BuildInfoPath)) {
                File.Delete(BuildManagerSettings.BuildInfoPath);
            }
            if (succeededBuildTargets.Count > 0) {
                string jsonBuildTargets = JsonUtility.ToJson(succeededBuildTargets);
                File.WriteAllText(BuildManagerSettings.BuildInfoPath, jsonBuildTargets);
            }

            if (upload) {
                // upload to specific plattform if valid
                switch (plattform) {
                    case DistributionPlatform.Steam:
                        steamPipe.UploadHeadless(succeededBuildTargets, appID);
                        break;

                    case DistributionPlatform.GOG:
                        gogPipe.UploadHeadless(succeededBuildTargets, appID);
                        break;

                    default:
                        HeadlessBuild.WriteToProperties("Error", "50");
                        EditorApplication.Exit(50);
                        break;
                }
            } else {
                HeadlessBuild.WriteToProperties("UploadTime", "00:00");
            }
        }

        public override void Save() {
            BuildPlayerOptions options = new BuildPlayerOptions();

            List<BuildTarget> targets;
            if (IsMagentaEnabled) {
                targets = new List<BuildTarget>() { BuildManagerSettings.Magenta.allowedBuildTarget };
            } else {
                targets = (from target in targetLookUp where target.Value.button.active select target.Value.target).ToList();
            }

            if (targets.Count > 0 && !Validate()) {
                return;
            }

            if (EditorUserBuildSettings.development) {
                options.options |= BuildOptions.Development;
            }

            if (EditorUserBuildSettings.allowDebugging) {
                options.options |= BuildOptions.AllowDebugging;
            }

            options.targetGroup = targetGroupModule.activeTargetGroup.group;
            options.scenes = (from scene in EditorBuildSettings.scenes where scene.enabled select scene.path).ToArray();

            // increment build version
            VersionInfo.BuildCounter++;

            // get newest git hash
            VersionInfo.UpdateGitHash();

            // update current timestamp
            VersionInfo.UpdateBuildTimestamp();

            // update version code
            VersionInfo.UpdateVersionCode();

            // refresh asset database
            AssetDatabase.Refresh();

            // Set version numbers for all platforms
            PlayerSettings.bundleVersion = VersionInfo.VersionCode;
            PlayerSettings.macOS.buildNumber = VersionInfo.BuildCounter.ToString();
            PlayerSettings.iOS.buildNumber = VersionInfo.BuildCounter.ToString();
            PlayerSettings.Android.bundleVersionCode = VersionInfo.BuildCounter;

            // clear build targets
            if (succeededBuildTargets == null) {
                succeededBuildTargets = new SuccessfulBuildTargets();
            } else if(succeededBuildTargets.builds == null) {
                succeededBuildTargets.builds = new List<SuccessfulBuildTarget>();
            } else if (succeededBuildTargets.builds.Count > 0) {
                succeededBuildTargets.builds.Clear();
            }
            succeededBuildTargets.version = VersionInfo.VersionCode;
            if (IsSteamEnabled) {
                succeededBuildTargets.distributionPlatform = DistributionPlatform.Steam;
                succeededBuildTargets.distributionBranch = steamPipe.GetBranchName();
            } else if (IsGOGEnabled) {
                succeededBuildTargets.distributionPlatform = DistributionPlatform.GOG;
                succeededBuildTargets.distributionBranch = "";
            } else if (IsMagentaEnabled) {
                succeededBuildTargets.distributionPlatform = DistributionPlatform.Magenta;
                succeededBuildTargets.distributionBranch = "";
            } else {
                succeededBuildTargets.distributionPlatform = DistributionPlatform.Other;
                succeededBuildTargets.distributionBranch = "";
            }
            // check if the active build target is in the list of builds to create.
            // if yes, build this first as we don't have to re convert assets for the target platform & speed things up.
            for (int i = 0; i < targets.Count; i++) {
                if (EditorUserBuildSettings.activeBuildTarget == targets[i]) {
                    Debug.Log("[Build Manager] Building for " + targets[i] + " first!");
                    if(StartBuild(targets[i], BuildManagerSettings.BuildPath, options)) {
                        succeededBuildTargets.Add(targets[i], lastBuildTargetPath);
                    }
                    targets.RemoveAt(i);
                    break;
                }
            }

            // build all other selected targets
            foreach (var target in targets) {
                if(StartBuild(target, BuildManagerSettings.BuildPath, options)) {
                    succeededBuildTargets.Add(target, lastBuildTargetPath);
                }
            }

            // update language depot files
            if (IsMagentaEnabled) {
                PrepareLocalizations();
            } else {
                UpdateLanguageDepots();
            }

            // write build info file
            if (File.Exists(BuildManagerSettings.BuildInfoPath)) {
                File.Delete(BuildManagerSettings.BuildInfoPath);
            }
            if (succeededBuildTargets.Count > 0) {
                string jsonBuildTargets = JsonUtility.ToJson(succeededBuildTargets);
                File.WriteAllText(BuildManagerSettings.BuildInfoPath, jsonBuildTargets);
            }

            // upload to steam if valid
            Upload(succeededBuildTargets);
            // send a mail notification
            SendMail(succeededBuildTargets);

            if (Event.current != null)
                GUIUtility.ExitGUI();
        }

        /// <summary>
        /// Delete all files and folders
        /// </summary>
        /// <param name="path"></param>
        void DeleteAllFilesAndFoldersAtPath(string path, bool removeWholeFolder = false) {
            if (Directory.Exists(path)) {
                DirectoryInfo di = new DirectoryInfo(path);
                foreach (FileInfo file in di.EnumerateFiles()) {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.EnumerateDirectories()) {
                    dir.Delete(true);
                }
                if (removeWholeFolder) {
                    di.Delete();
                }
            } else if(File.Exists(path)) {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Update language depots
        /// </summary>
        void UpdateLanguageDepots() {
            string langDepotPathBase = Path.Combine(BuildManagerSettings.cachedDataPath, BuildManagerSettings.General.languageDepotsBasePath);

            // remove all files and directories, since language files should always be updated.
            if (!Directory.Exists(langDepotPathBase)) {
                Directory.CreateDirectory(langDepotPathBase);
            } else {
                DeleteAllFilesAndFoldersAtPath(langDepotPathBase);
            }

            foreach (var langDepot in BuildManagerSettings.General.languageDepots) {
                string langDepotPath = Path.Combine(langDepotPathBase, langDepot.name, "Localizations");

                if (!Directory.Exists(langDepotPath)) {
                    Directory.CreateDirectory(langDepotPath);
                }

                // remove all files and directories, since language files should always be updated.
                if (!Directory.Exists(langDepotPath)) {
                    Directory.CreateDirectory(langDepotPath);
                } else {
                    DeleteAllFilesAndFoldersAtPath(langDepotPath);
                }

                foreach (GeneralSettings.LanguageDepot.ContentPath path in langDepot.contentPaths) {
                    string contentPath = Path.Combine(BuildManagerSettings.cachedDataPath, path.path);
                    contentPath = contentPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

                    if (File.Exists(contentPath) || Directory.Exists(contentPath)) {
                        // get the file attributes for file or directory
                        FileAttributes attr = File.GetAttributes(contentPath);
                        if (attr.HasFlag(FileAttributes.Directory)) {
                            if (path.copyContainingDirectory) {
                                string relativeFolders = path.path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                                relativeFolders = relativeFolders.Replace($"StreamingAssets{Path.DirectorySeparatorChar}Localizations{Path.DirectorySeparatorChar}", "");
                                relativeFolders = Path.Combine(langDepotPath, relativeFolders);
                                Copy(contentPath, relativeFolders);
                            } else {
                                Copy(contentPath, langDepotPath);
                            }
                        } else {
                            if (path.copyContainingDirectory) {
                                string dir = Path.GetDirectoryName(path.path);
                                if (dir != null) {
                                    dir = Path.GetFileName(dir);
                                    Directory.CreateDirectory(Path.Combine(langDepotPath, dir));
                                    File.Copy(contentPath, Path.Combine(langDepotPath, dir, Path.GetFileName(path.path)));
                                }
                            } else {
                                File.Copy(contentPath, Path.Combine(langDepotPath, Path.GetFileName(path.path)));
                            }

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update language depots
        /// </summary>
        void PrepareLocalizations() {
            /*
            foreach (var langDepot in BuildManagerSettings.General.languageDepots) {
                string langDepotPath = Path.Combine(BuildManagerSettings.BuildPath, langDepot.name + "LanguagePack");

                if (!BuildManagerSettings.Magenta.languagesToExtract.Contains(langDepot.name) && langDepot.name != BuildManagerSettings.Magenta.preinstalledLanguage) {
                    if (Directory.Exists(langDepotPath)) {
                        Directory.Delete(langDepotPath);
                    }
                    continue;
                }

                string langPath = langDepotPath;
                if (langDepot.name == BuildManagerSettings.Magenta.preinstalledLanguage) {
                    langPath = Path.Combine(Path.GetDirectoryName(succeededBuildTargets.builds[0].targetPath), BuildManagerSettings.Magenta.localizationPath);
                }

                if (!Directory.Exists(langPath)) {
                    Directory.CreateDirectory(langPath);
                }

                // remove all files and directories, since language files should always be updated.
                if (!Directory.Exists(langPath)) {
                    Directory.CreateDirectory(langPath);
                } else {
                    DeleteAllFilesAndFoldersAtPath(langPath);
                }

                foreach (GeneralSettings.LanguageDepot.ContentPath path in langDepot.contentPaths) {
                    string contentPath = Path.Combine(BuildManagerSettings.cachedDataPath, path.path);
                    contentPath = contentPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

                    if (File.Exists(contentPath) || Directory.Exists(contentPath)) {
                        // get the file attributes for file or directory
                        FileAttributes attr = File.GetAttributes(contentPath);
                        if (attr.HasFlag(FileAttributes.Directory)) {
                            if (path.copyContainingDirectory) {
                                string relativeFolders = path.path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                                relativeFolders = relativeFolders.Replace($"StreamingAssets{Path.DirectorySeparatorChar}Localizations{Path.DirectorySeparatorChar}", "");
                                relativeFolders = Path.Combine(langPath, relativeFolders);
                                Copy(contentPath, relativeFolders);
                            } else {
                                Copy(contentPath, langPath);
                            }
                        } else {
                            if (path.copyContainingDirectory) {
                                string dir = Path.GetDirectoryName(path.path);
                                if (dir != null) {
                                    dir = Path.GetFileName(dir);
                                    Directory.CreateDirectory(Path.Combine(langPath, dir));
                                    File.Copy(contentPath, Path.Combine(langPath, dir, Path.GetFileName(path.path)));
                                }
                            } else {
                                File.Copy(contentPath, Path.Combine(langPath, Path.GetFileName(path.path)));
                            }

                        }
                    }
                }

            }
            */
        }

        public static void Copy(string sourceDirectory, string targetDirectory) {
            var diSource = new DirectoryInfo(sourceDirectory);
            var diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target) {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles()) {
                //Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories()) {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        void DeleteExcludedFilesAndFoldersFromBuild(BuildTarget target, string targetPath) {
            if (EditorPrefs.GetBool(exclusionKey, true) || IsMagentaEnabled) {
                GeneralSettings.ExcludePath exclude = null;
                for (int i = 0; i < BuildManagerSettings.General.excludePaths.Count; i++) {
                    exclude = BuildManagerSettings.General.excludePaths[i];
                    
                    bool shouldExclude = exclude.excludeType == GeneralSettings.ExcludeType.Disable ? false : true;
                    if (exclude.excludeType != GeneralSettings.ExcludeType.AlwaysExclude) {
                        if ((IsSteamEnabled || IsGOGEnabled) && exclude.excludeType == GeneralSettings.ExcludeType.ExcludeOnlyForSteamAndGOG) {
                            shouldExclude = true;
                        } else {
                            if (IsMagentaEnabled && exclude.excludeType == GeneralSettings.ExcludeType.ExcludeOnlyForMagenta) {
                                shouldExclude = true;
                            } else {
                                shouldExclude = false;
                            }
                        }
                    }
                    
                    if (exclude.buildTarget == target && shouldExclude) {
                        foreach (var excludePath in exclude.paths) {
                            if (!string.IsNullOrWhiteSpace(excludePath)) {
                                DeleteAllFilesAndFoldersAtPath(Path.Combine(Path.GetDirectoryName(targetPath), excludePath), exclude.excludeCompleteFolder);
                            }
                        }
                    }
                }
            }
        }

        bool StartBuild(BuildTarget target, string path, BuildPlayerOptions options) {

            bool succeeded = false;
            string tName = target.ToString().ToLower();
            string targetPath = path + tName + "/";

            if (!System.IO.Directory.Exists(targetPath)) {
                System.IO.Directory.CreateDirectory(targetPath);
            }

            targetPath += targetGroupModule.productName;
            if (tName.Contains("windows") && tName.Contains("standalone")) {
                targetPath += ".exe";
            }
            options.locationPathName = targetPath;
            options.target = target;

            System.Console.WriteLine($"##### Start Buildingprocess: {DateTime.Now.ToString("HH:mm:ss")} #####");
            DateTime startTime = DateTime.Now;
            var report = BuildPipeline.BuildPlayer(options);
            System.Console.WriteLine($"##### Finished Buildingprocess: {DateTime.Now.ToString("HH:mm:ss")}  ##### Buildtime: {DateTime.Now.Subtract(startTime)}#####");
            HeadlessBuild.WriteToProperties("BuildTime", DateTime.Now.Subtract(startTime).ToString(@"mm\:ss"), true);

            if (report != null && report.summary.result == BuildResult.Succeeded) {
                succeeded = true;
                lastBuildTargetPath = targetPath;
                DeleteExcludedFilesAndFoldersFromBuild(target, targetPath);
            }

            return succeeded;
        }

        public void UploadExisting(bool onlyDLCs) {
            if (IsAnyPipeEnabled) {
                if (onlyDLCs) {
                    UpdateLanguageDepots();
                    if (IsSteamEnabled) {
                        steamPipe.UploadOnlyDLCs();
                    } else {
                        gogPipe.UploadOnlyDLCs();
                    }
                }
                else {
                    if (IsSteamEnabled) {
                        UploadExistingBuilds(DistributionPlatform.Steam, (info) => steamPipe.UploadExisting(), steamPipe.GetBranchName());
                    } else {
                        UploadExistingBuilds(DistributionPlatform.GOG, (info) => gogPipe.UploadExisting());
                    }
                }
            } else if (IsMagentaEnabled) {
                UploadExistingBuilds(DistributionPlatform.Magenta, (info) => {
                    PrepareLocalizations();
                    magentaUpload.UploadExisting(info);
                });
            }
        }

        private void UploadExistingBuilds(DistributionPlatform distributionPlatform, Action<SuccessfulBuildTargets> uploadAction, string branchName = "") {
            succeededBuildTargets = BuildManagerSettings.GetSuccessfulBuildTargets(distributionPlatform);
            foreach (var build in succeededBuildTargets.builds) {
                DeleteExcludedFilesAndFoldersFromBuild(build.buildTarget, build.targetPath);
            }
            if (succeededBuildTargets != null && succeededBuildTargets.Count > 0) {
                succeededBuildTargets.distributionBranch = branchName;
                uploadAction(succeededBuildTargets);
                SendMail(succeededBuildTargets);
                string jsonBuildTargets = JsonUtility.ToJson(succeededBuildTargets);
                File.WriteAllText(BuildManagerSettings.BuildInfoPath, jsonBuildTargets);
            }
        }

        private void SendMail(SuccessfulBuildTargets successfulBuildTargets) {
            if (EditorPrefs.GetBool(sendMailKey, false)) {
                mailProcess.SendMail(successfulBuildTargets);
            }
        }

        public void Upload(SuccessfulBuildTargets successfulBuildTargets) {
            if (IsAnyPipeEnabled) {
                if (IsSteamEnabled) {
                    steamPipe.UploadDefault(successfulBuildTargets);
                } else {
                    gogPipe.UploadDefault(successfulBuildTargets);
                }
            } else if(IsMagentaEnabled) {
                magentaUpload.UploadDefault(successfulBuildTargets);
            }
        }

        public bool IsSteamEnabled {
            get {
#if STEAM
                return true;
#else
                return false;
#endif
            }
        }

        public bool IsGOGEnabled {
            get {
#if GOGGALAXY
                return true;
#else
                return false;
#endif
            }
        }

        public bool IsMagentaEnabled {
            get {
#if MAGENTA
                return true;
#else
                return false;
#endif
            }
        }

        public bool IsAnyPipeEnabled {
            get {
#if STEAM || GOGGALAXY
                if (!targetGroupModule.activeTargetGroup.name.Contains("Standalone")) { return false; }
                return true;
#else
                return false;
#endif
            }
        }
    }
}

