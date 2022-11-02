using BuildManager.Templates.GOG;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using Settings = BuildManager.BuildManagerSettings;
using Newtonsoft.Json;

namespace BuildManager {
    /// <summary>
    /// Class dedicated for creating Steam .vdf build profiles
    /// </summary>
    public class GOGProfileBuilder {

        private List<string> files = new List<string>();
        private List<ProjectContainer> projectContainers = new List<ProjectContainer>();
        private List<Depot> languageDepots = new List<Depot>();
        private List<string> languageCodes = new List<string>();
        private List<GOGGalaxySettings.BuildDepotConfig> validBuildDepots = new List<GOGGalaxySettings.BuildDepotConfig>();
        private List<string> osBitnesses = new List<string>();

        /// <summary>
        /// Creates all relevant building profiles & writes them to disk for steamcmd.exe
        /// </summary>
        /// <param name="selectedAppConfig"></param>
        /// <param name="succeededTargets"></param>
        /// <returns></returns>
        public bool CreateProfiles(GOGGalaxySettings.GOGGalaxyAppConfig selectedAppConfig, string versionCode, string productName, List<BuildTarget> succeededTargets = null) {
            files.Clear();
            projectContainers.Clear();

            if (succeededTargets != null) {
                // get all valid language depots and language codes for this app configuration
                ExtractLanguageDepotsAndCodes(selectedAppConfig);

                foreach (var platformConfig in selectedAppConfig.platformConfigs) {
                    if (platformConfig.enabled) {
                        // get all valid build depots for this platform
                        ExtractValidBuildDepots(platformConfig, succeededTargets);

                        if (validBuildDepots.Count > 0) {
                            // create project container
                            ProjectContainer projectContainer = new ProjectContainer();

                            // create & setup project
                            Project project = projectContainer.project;
                            project.baseProductId = selectedAppConfig.productID;
                            project.clientId = selectedAppConfig.clientID;
                            project.clientSecret = selectedAppConfig.clientSecret;
                            project.installDirectory = productName;
                            project.name = selectedAppConfig.displayName;
                            project.version = versionCode;
                            project.platform = platformConfig.GetPlatformString();

                            // create & setup product
                            Product product = new Product();
                            product.productId = project.baseProductId;
                            product.name = project.name;
                            project.products.Add(product);

                            // create & setup exec task
                            Task mainTask = new Task();
                            mainTask.name = project.name;
                            mainTask.languages = languageCodes;
                            mainTask.osBitness = osBitnesses;
                            mainTask.path = GetExecutablePathByPlatform(platformConfig.platform, productName);
                            product.tasks.Add(mainTask);

                            // create & setup build depots
                            foreach (var validDepot in validBuildDepots) {
                                string identifier = validDepot.buildTarget.ToString().ToLower();
                                string folder = Path.Combine(Settings.General.paths.BuildsFolder, identifier);
                                folder = folder.Replace('\\', '/');

                                if (Directory.Exists(folder)) {
                                    Depot depot = new Depot();
                                    depot.osBitness.Add(((int)validDepot.osBitness).ToString());
                                    depot.name = identifier;
                                    depot.folder = folder;
                                    depot.languages = languageCodes;
                                    product.depots.Add(depot);
                                } else {
                                    UnityEngine.Debug.LogWarning("[Path] " + folder + " for depot: " + identifier + " could not be found.");
                                }
                            }

                            // create & setup language depots
                            foreach (var langDepot in languageDepots) {
                                Depot depot = new Depot();
                                depot.folder = langDepot.folder;
                                depot.languages = langDepot.languages;
                                depot.name = langDepot.name;
                                depot.osBitness = osBitnesses;
                                product.depots.Add(depot);
                            }

                            foreach (var dlcDepotData in platformConfig.dlcDepots) {

                                if (dlcDepotData.enabled) {
                                    string folder = Path.GetFullPath(Path.Combine(Settings.cachedDataPath, Settings.AddonsPath, dlcDepotData.relativePath));
                                    folder = folder.Replace('\\', '/');

                                    if (Directory.Exists(folder)) {
                                        Product dlcProduct = new Product();
                                        dlcProduct.name = dlcDepotData.name;
                                        dlcProduct.productId = dlcDepotData.depotID.ToString();

                                        Depot dlcDepot = new Depot();
                                        dlcDepot.languages = languageCodes;
                                        dlcDepot.folder = folder;
                                        dlcDepot.name = Path.GetFileName(folder).ToLower();
                                        dlcDepot.osBitness.Add(dlcDepotData.osBitness == GOGGalaxySettings.OSBitness.x64 ? "64" : "32");
                                        dlcProduct.depots.Add(dlcDepot);

                                        project.products.Add(dlcProduct);
                                    } else {
                                        UnityEngine.Debug.LogWarning("[Path] " + folder + " for depot: " + dlcDepotData.depotID + " could not be found.");
                                    }
                                }
                            }

                            // add created container to list of containers
                            projectContainers.Add(projectContainer);
                        }
                    }
                }
            }

            bool isUploadable = projectContainers.Count > 0;

            if (isUploadable) {
                WriteProfilesToDisk();
            }

            return isUploadable;
        }

        private void ExtractLanguageDepotsAndCodes(GOGGalaxySettings.GOGGalaxyAppConfig selectedAppConfig) {
            languageDepots.Clear();
            languageCodes.Clear();

            foreach (var langDepot in selectedAppConfig.languageDepots) {
                if (langDepot.enabled) {
                    GeneralSettings.LanguageDepot genericLangDepot = null;
                    for (int i = 0; i < Settings.General.languageDepots.Count; i++) {
                        genericLangDepot = Settings.General.languageDepots[i];
                        if (genericLangDepot.name == langDepot.name) {
                            break;
                        }
                        genericLangDepot = null;
                    }
                    if (genericLangDepot != null) {
                        string folder = Path.GetFullPath(Path.Combine(Settings.cachedDataPath, Settings.General.languageDepotsBasePath, genericLangDepot.name));
                        folder = folder.Replace('\\', '/');
                        if (Directory.Exists(folder)) {
                            Depot depot = new Depot();
                            depot.languages.Add(langDepot.languageCode);
                            if (!languageCodes.Contains(langDepot.languageCode)) {
                                languageCodes.Add(langDepot.languageCode);
                            }
                            depot.folder = folder;
                            depot.name = langDepot.name;
                            languageDepots.Add(depot);
                        } else {
                            UnityEngine.Debug.LogWarning("[Path] " + folder + " for depot: " + langDepot.name + " could not be found.");
                        }
                    }
                }
            }
        }


        private void ExtractValidBuildDepots(GOGGalaxySettings.PlatformConfig platformConfig, List<BuildTarget> succeededTargets) {
            validBuildDepots.Clear();
            osBitnesses.Clear();

            foreach (var buildDepot in platformConfig.buildDepots) {
                if (buildDepot.enabled && succeededTargets.Contains(buildDepot.buildTarget)) {
                    validBuildDepots.Add(buildDepot);
                    string osBitnessInDepot = ((int)buildDepot.osBitness).ToString();
                    if (!osBitnesses.Contains(osBitnessInDepot)) {
                        osBitnesses.Add(osBitnessInDepot);
                    }
                }
            }
        }

        private string GetExecutablePathByPlatform(GOGGalaxySettings.PlatformConfig.Platform platform, string executableName) {
            switch (platform) {
                case GOGGalaxySettings.PlatformConfig.Platform.windows:
                    return executableName + ".exe";
                case GOGGalaxySettings.PlatformConfig.Platform.osx:
                    return executableName + ".app";
                case GOGGalaxySettings.PlatformConfig.Platform.linux:
                    return executableName + ".sh";
            }
            return null;
        }

        /// <summary>
        /// writes all profiles files to disk, so steamcmd.exe can read them.
        /// </summary>
        private void WriteProfilesToDisk() {
            //Maybe need to be checked for correct settings
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.PreserveReferencesHandling = PreserveReferencesHandling.None;
            settings.TypeNameHandling = TypeNameHandling.Auto;
            settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;

            if (!Directory.Exists(Settings.GOGGalaxy.paths.TemporaryScriptFolder)) {
                Directory.CreateDirectory(Settings.GOGGalaxy.paths.TemporaryScriptFolder);
            }

            foreach (var projectContainer in projectContainers) {
                string json = JsonConvert.SerializeObject(projectContainer, Formatting.Indented, settings);
                files.Add(Path.Combine(Settings.GOGGalaxy.paths.TemporaryScriptFolder, $"{projectContainer.project.baseProductId}_{projectContainer.project.platform}.json"));
                File.WriteAllText(files[files.Count - 1], json);
            }
        }

        public List<string> GetFiles() {
            return files;
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

