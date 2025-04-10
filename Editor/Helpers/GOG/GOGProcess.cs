using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using UnityEditor;
using static BuildManager.BuildManagerSettings;
using System;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace BuildManager {
    /// <summary>
    /// Runs GOGGalaxyPipelineBuilder.exe to execute GOG build upload command
    /// </summary>
    public class GOGProcess {
        private Process p;
        public bool processing = false;
        private GOGProfileBuilder profileBuilder = new GOGProfileBuilder();
        //private List<string> errorMessages = new List<string>();

        /// <summary>
        /// Method to start the GOGGalaxyPipelineBuilder.exe build upload process
        /// </summary>
        /// <param name="appConfig">The app config to use for uploading</param>
        /// <param name="versionCode"></param>
        /// <param name="productName"></param>
        /// <param name="targets"></param>
        public async void StartProcess(GOGGalaxySettings.GOGGalaxyAppConfig appConfig, string versionCode, string productName, SuccessfulBuildTargets targets = null) {
            if (!processing) {
                //errorMessages.Clear();
                // do stuff in another thread
                Task task = new Task(() => {
                    var buildTargets = targets.GetBuildTargets();
                    bool isUploadable = profileBuilder.CreateProfiles(appConfig, versionCode, productName, buildTargets);
                    //isUploadable = false;
                    if (isUploadable) {
                        var projectFiles = profileBuilder.GetFiles();
                        foreach (var projectFile in projectFiles) {
                            // create process
                            p = new Process();
                            // Path to executable
                            p.StartInfo.FileName = GOGGalaxy.paths.Executable;
                            //errorMessages.Add("[Process: filename] " + p.StartInfo.FileName);
                            p.StartInfo.Arguments = $"build-game {projectFile} --username=\"{GOGGalaxy.BuildAccountName}\" --password=\"{GOGGalaxy.BuildAccountPassword}\" --log_level=debug";
                            //errorMessages.Add("[Process: args] " + p.StartInfo.Arguments);
                            p.StartInfo.WorkingDirectory = Path.GetDirectoryName(GOGGalaxy.paths.Executable);
                            //errorMessages.Add("[Process: wd] " + p.StartInfo.WorkingDirectory);
                            p.StartInfo.UseShellExecute = true;
                            p.Start();
                            p.WaitForExit();
                        }
                        // remove .vdfs from disk
                        profileBuilder.RemoveProfilesFromDisk();
                    }
                });
                task.Start();

                await task;
                processing = false;
                /*
                foreach (var message in errorMessages) {
                    UnityEngine.Debug.Log(message);
                }
                */
            }
        }

        /// <summary>
        /// Method to start the GOGGalaxyPipelineBuilder.exe build upload process headless
        /// </summary>
        /// <param name="appConfig">The app config to use for uploading</param>
        /// <param name="branchName">The branch name to set live</param>
        /// <param name="buildMessage">The buildmessage to use</param>
        /// <param name="targets"></param>
        public void StartProcessHeadless(GOGGalaxySettings.GOGGalaxyAppConfig appConfig, string versionCode, string productName, SuccessfulBuildTargets targets = null) {
            if (!processing) {
                var buildTargets = targets.GetBuildTargets();
                bool isUploadable = profileBuilder.CreateProfiles(appConfig, versionCode, productName, buildTargets);
                if (isUploadable) {
                    var projectFiles = profileBuilder.GetFiles();
                    foreach (var projectFile in projectFiles) {
                        ClearLog();

                        // create process
                        p = new Process();
                        // Path to executable
                        p.StartInfo.FileName = GOGGalaxy.paths.Executable;
                        p.StartInfo.Arguments = $"build-game {projectFile} --username=\"{GOGGalaxy.BuildAccountName}\" --password=\"{GOGGalaxy.BuildAccountPassword}\" --log_level=debug";
                        p.StartInfo.WorkingDirectory = Path.GetDirectoryName(GOGGalaxy.paths.Executable);
                        p.StartInfo.CreateNoWindow = true;
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.RedirectStandardOutput = true;
                        p.StartInfo.RedirectStandardError = true;

                        p.OutputDataReceived += new DataReceivedEventHandler((s, e) => {
                            System.Console.WriteLine(e.Data);
                        });
                        p.ErrorDataReceived += new DataReceivedEventHandler((s, e) => {
                            System.Console.WriteLine(e.Data);
                        });

                        p.Start();
                        p.BeginOutputReadLine();
                        p.BeginErrorReadLine();
                        p.WaitForExit();

                        HeadlessBuild.WriteToProperties("BuildID", ExtractBuildID(versionCode));
                    }
                    // remove .vdfs from disk
                    profileBuilder.RemoveProfilesFromDisk();
                } else {
                    HeadlessBuild.WriteToProperties("Error", "40");
                    EditorApplication.Exit(40);
                }
            }
        }

        /// <summary>
        /// Return the path of the log file 
        /// </summary>
        /// <returns></returns>
        private string GetLogPath() {
            string dir = Path.GetDirectoryName(GOGGalaxy.paths.Executable);
            return Path.Join(dir, "output", "uploadLog.json");
        }

        /// <summary>
        /// Clear the GOGBuilder Log file
        /// </summary>
        private void ClearLog() {
            string logPath = GetLogPath();
            if (File.Exists(logPath)) {
                File.WriteAllText(logPath, string.Empty);
            }
        }

        /// <summary>
        /// Returns the buildID of the matching version
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        private string ExtractBuildID(string version) {
            string logPath = GetLogPath(); 
            if (File.Exists(logPath)) {
                string json = File.ReadAllText(logPath);

                JObject obj = JObject.Parse(json);
                JProperty entry = obj.Properties().FirstOrDefault();

                if (entry != null) {
                    if (version.Equals(entry.Value<string>("version"))) {
                        return entry.Value<string>("buildId");
                    }
                }
            }

            return string.Empty;
        }
    }
}
