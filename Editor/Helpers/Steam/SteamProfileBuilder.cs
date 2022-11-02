using BuildManager.Templates.Steam;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using Settings = BuildManager.BuildManagerSettings;
using UnityEngine;

namespace BuildManager {
    /// <summary>
    /// Class dedicated for creating Steam .vdf build profiles
    /// </summary>
    public class SteamProfileBuilder {
        private Appbuild appBuildProfile;
        private SteamSettings.SteamAppConfig appConfig;
        private List<string> files = new List<string>();
        private List<string> errorMessages = new List<string>();
        private List<DepotBuildConfig> buildConfigs = new List<DepotBuildConfig>();

        /// <summary>
        /// Creates all relevant building profiles & writes them to disk for steamcmd.exe
        /// </summary>
        /// <param name="selectedAppConfig"></param>
        /// <param name="succeededTargets"></param>
        /// <returns></returns>
        public bool CreateProfiles(SteamSettings.SteamAppConfig selectedAppConfig, string setLive, string desc, SuccessfulBuildTargets succeededTargets = null) {
            files.Clear();
            errorMessages.Clear();
            buildConfigs.Clear();

            appBuildProfile = Appbuild.Create(selectedAppConfig.appID, setLive, desc);
            var buildTargets = succeededTargets.GetBuildTargets();

            if (succeededTargets != null) {
                foreach (var buildDepot in selectedAppConfig.buildDepots) {
                    if (buildDepot.enabled && buildTargets.Contains(buildDepot.buildTarget)) {
                        string identifier = buildDepot.buildTarget.ToString().ToLower();
                        string folder = Path.Combine(Settings.General.paths.BuildsFolder, identifier);
                        if (Directory.Exists(folder)) {
                            appBuildProfile.depots.Add(buildDepot.depotID, buildDepot.buildTarget.ToString() + ".vdf");
                            buildConfigs.Add(DepotBuildConfig.Create(buildDepot.depotID, folder, buildDepot.exclusions));
                        } else {
                            errorMessages.Add("[Path] " + folder + " for depot: " + buildDepot.depotID + " (" + identifier + ")" + " could not be found.");
                        }
                    }
                }
            }

            foreach (var langDepot in selectedAppConfig.languageDepots) {
                if (langDepot.enabled) {
                    GeneralSettings.LanguageDepot genericLangDepot = null;
                    for (int i=0; i<Settings.General.languageDepots.Count; i++) {
                        genericLangDepot = Settings.General.languageDepots[i];
                        if (genericLangDepot.name == langDepot.name) {
                            break;
                        }
                        genericLangDepot = null;
                    }
                    if (genericLangDepot != null) {
                        string folder = Path.GetFullPath(Path.Combine(Settings.cachedDataPath, Settings.General.languageDepotsBasePath, genericLangDepot.name));
                        if (Directory.Exists(folder)) {
                            appBuildProfile.depots.Add(langDepot.depotID, langDepot.depotID.ToString() + ".vdf");
                            buildConfigs.Add(DepotBuildConfig.Create(langDepot.depotID, folder, new string[] { }));
                        } else {
                            errorMessages.Add("[Path] " + folder + " for depot: " + langDepot.depotID + " could not be found.");
                        }
                    }
                }
            }

            foreach (var dlcDepot in selectedAppConfig.dlcDepots) {
                if (dlcDepot.enabled) {
                    string folder = Path.GetFullPath(Path.Combine(Settings.cachedDataPath, Settings.AddonsPath, dlcDepot.relativePath));
                    if (Directory.Exists(folder)) {
                        appBuildProfile.depots.Add(dlcDepot.depotID, dlcDepot.depotID.ToString() + ".vdf");
                        buildConfigs.Add(DepotBuildConfig.Create(dlcDepot.depotID, folder, dlcDepot.exclusions));

                    } else {
                        errorMessages.Add("[Path] " + folder + " for depot: " + dlcDepot.depotID + " could not be found.");
                    }
                }
            }

            bool isUploadable = appBuildProfile.depots.Count > 0;

            if (isUploadable) {
                WriteProfilesToDisk();
            }

            return isUploadable;
        }

        /// <summary>
        /// writes all profiles files to disk, so steamcmd.exe can read them.
        /// </summary>
        private void WriteProfilesToDisk() {
            if (!Directory.Exists(Settings.Steam.paths.TemporaryScriptFolder)) {
                Directory.CreateDirectory(Settings.Steam.paths.TemporaryScriptFolder);
            }
            string vdf = Appbuild.SerializeToVdf(appBuildProfile);
            files.Add(Path.Combine(Settings.Steam.paths.TemporaryScriptFolder, nameof(Appbuild) + ".vdf"));
            File.WriteAllText(files[files.Count - 1], vdf);

            foreach (var buildConfig in buildConfigs) {
                vdf = DepotBuildConfig.SerializeToVdf(buildConfig);
                files.Add(Path.Combine(Settings.Steam.paths.TemporaryScriptFolder, appBuildProfile.depots[buildConfig.DepotID]));
                File.WriteAllText(files[files.Count - 1], vdf);
            }
        }

        /// <summary>
        /// Removes all profiles that were written to disk
        /// </summary>
        public void RemoveProfilesFromDisk() {
            foreach (var file in files) {
                File.Delete(file);
            }
            files.Clear();
        }
    }
}
