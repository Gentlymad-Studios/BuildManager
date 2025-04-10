using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using UnityEditor;
using BuildManager.Templates.Steam;
using static BuildManager.BuildManagerSettings;
using UnityEngine;
using System.Text.RegularExpressions;
using System;
using UnityEngine.Windows;

namespace BuildManager {
    /// <summary>
    /// Runs steamcmd.exe to execute Steams build upload command (run_app_build_http)
    /// </summary>
    public class SteamProcess {
        private Process p;
        private string arguments = "";
        public bool processing = false;
        private SteamProfileBuilder profileBuilder = new SteamProfileBuilder();

        /// <summary>
        /// Build command arguments for steamcmd.exe build upload command
        /// </summary>
        private void BuildArguments() {
            string pathToAppbuildProfile = Path.Combine(Steam.paths.TemporaryScriptFolder, nameof(Appbuild) + ".vdf");
            string quitOption = Steam.quitProcess ? "+quit" : "";
            arguments = $"+login {Steam.BuildAccountName} {Steam.BuildAccountPassword} +run_app_build_http \"{pathToAppbuildProfile}\" {quitOption}";
#if UNITY_EDITOR_OSX
            //need to call quit twice on mac
            arguments += " "+quitOption;
#endif
        }

        /// <summary>
        /// Method to start the steamcmd.exe build upload process
        /// </summary>
        /// <param name="appConfig">The app config to use for uploading</param>
        /// <param name="branchName">The branch name to set live</param>
        /// <param name="buildMessage">The buildmessage to use</param>
        /// <param name="targets"></param>
        public async void StartProcess(SteamSettings.SteamAppConfig appConfig, string branchName, string buildMessage, SuccessfulBuildTargets targets = null) {
            if (!processing) {
                // create arguments
                BuildArguments();
                // create process
                p = new Process();
                // Path to executable
#if !UNITY_EDITOR_OSX
                p.StartInfo.FileName = Steam.paths.ValidExecutable;
                p.StartInfo.Arguments = arguments;
#else
                // create a bash file script that contains all necessary commands
                string bashPath = Path.Combine(Path.GetFullPath(Path.GetDirectoryName(Steam.paths.ValidExecutable)), "run.sh");
                string cmd = Path.GetFullPath(Steam.paths.ValidExecutable) + " " + arguments;
                cmd += $"\r\nrm -- {bashPath}";
                File.WriteAllText(bashPath, cmd);
                // run the bash file in an explicit terminal window
                p.StartInfo.FileName = "osascript";
                p.StartInfo.Arguments = $"-e 'tell application \"Terminal\" to do script \"{bashPath}\"'";
#endif
                p.StartInfo.WorkingDirectory = Path.GetDirectoryName(Steam.paths.ValidExecutable);
                p.StartInfo.UseShellExecute = true;

                // do stuff in another thread
                Task task = new Task(() => {
                    // create .vdfs
                    if(profileBuilder.CreateProfiles(appConfig, branchName, buildMessage, targets)) {
                        // start process and keep it alive
                        p.Start();
                        p.WaitForExit();
                    }
#if !UNITY_EDITOR_OSX
                    // remove .vdfs from disk
                    profileBuilder.RemoveProfilesFromDisk();
#endif
                });
                task.Start();

                await task;
                processing = false;
            }
        }

        /// <summary>
        /// Method to start the steamcmd.exe build upload process headless
        /// </summary>
        /// <param name="appConfig">The app config to use for uploading</param>
        /// <param name="branchName">The branch name to set live</param>
        /// <param name="buildMessage">The buildmessage to use</param>
        /// <param name="targets"></param>
        public void StartProcessHeadless(SteamSettings.SteamAppConfig appConfig, string branchName, string buildMessage, SuccessfulBuildTargets targets = null) {
            if (!processing) {
                // create arguments
                BuildArguments();
                // create process
                p = new Process();
                // Path to executable
#if !UNITY_EDITOR_OSX
                p.StartInfo.FileName = Steam.paths.ValidExecutable;
                p.StartInfo.Arguments = arguments;
#endif
                p.StartInfo.WorkingDirectory = Path.GetDirectoryName(Steam.paths.ValidExecutable);

                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;

                p.OutputDataReceived += new DataReceivedEventHandler((s, e) =>
                {
                    Match match = Regex.Match(e.Data, @"BuildID (\d+)");
                    if (match.Success) {
                        HeadlessBuild.WriteToProperties("BuildID", match.Groups[1].Value);
                    }

                    System.Console.WriteLine(e.Data);
                });
                p.ErrorDataReceived += new DataReceivedEventHandler((s, e) => 
                {
                    System.Console.WriteLine(e.Data);
                });


                // create .vdfs
                if (profileBuilder.CreateProfiles(appConfig, branchName, buildMessage, targets)) {
                    // start process and keep it alive
                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    p.WaitForExit();
                } else {
                    HeadlessBuild.WriteToProperties("Error", "40");
                    EditorApplication.Exit(40);
                }


#if !UNITY_EDITOR_OSX
                // remove .vdfs from disk
                profileBuilder.RemoveProfilesFromDisk();
#endif
                processing = false;
            }
        }

    }
}
