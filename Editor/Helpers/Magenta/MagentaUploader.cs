using static BuildManager.BuildManagerSettings;
using System.IO;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BuildManager {
    public class MagentaUploader {
        public bool processing = false;

        public MagentaUploader(string editorPrefsPrefix) {}

        /// <summary>
        /// Execute of existing builds in builds folder
        /// </summary>
        public void UploadExisting(SuccessfulBuildTargets targets) {
            StartUploadProcess(targets);
        }

        /// <summary>
        /// Execute a regular Upload containing newly created builds
        /// </summary>
        /// <param name="targets"></param>
        public void UploadDefault(SuccessfulBuildTargets targets) {
            UnityEngine.Debug.Log("[Magenta: Upload Default] " + targets != null ? string.Join(",", targets) : "NULL!");
            if (targets != null && targets.Count > 0) {
                StartUploadProcess(targets);
            }
        }

        /// <summary>
        /// Base Method to manage uploading to steam
        /// </summary>
        /// <param name="targets"></param>
        private async void StartUploadProcess(SuccessfulBuildTargets targets = null) {
            if (!processing) {
                processing = true;

                // create filenames & paths
                string buildPath = BuildPath;
                string pathToTools = new DirectoryInfo(Path.Combine(buildPath, Magenta.buildToolsFolder)).FullName;
                string zipName = $"build_{DateTime.Now.ToString("yyyyMMddHHmmss")}.zip";
                string zipPath = Path.Combine(pathToTools, zipName);

                // create batch file
                string batFile = "";
                batFile += $"\"{pathToTools}7zip/7z.exe\" a -mx3 -x!\"{BuildInfoPath}\" -tzip \"{zipPath}\" \"{buildPath}*\"\r\n";
                batFile += $"\"{pathToTools}winscp/winscp.com\" /command \"open ftp://{Magenta.ftp.username}:{Magenta.ftp.password}@{Magenta.ftp.hostURL}/\" \"put -delete {zipPath}\" \"exit\"";
                string batchFileName = Path.Combine(pathToTools, "upload.bat");
                File.WriteAllText(batchFileName, batFile);

                // start upload process
                Process p = new Process();
                // Path to executable
                p.StartInfo.FileName = batchFileName;
                // UnityEngine.Debug.Log("[Process: filename] "+ p.StartInfo.FileName);
                p.StartInfo.Arguments = "";
                // UnityEngine.Debug.Log("[Process: args] " + arguments);
                p.StartInfo.WorkingDirectory = pathToTools;
                // UnityEngine.Debug.Log("[Process: wd] " + p.StartInfo.WorkingDirectory);
                p.StartInfo.UseShellExecute = true;

                // do stuff in another thread
                Task task = new Task(() => {
                    // start process and keep it alive
                    p.Start();
                    p.WaitForExit();
                });
                task.Start();

                await task;
                // we are done with uploading
                if (File.Exists(batchFileName)) {
                    File.Delete(batchFileName);
                }
                processing = false;
            }
        }

    }
}
