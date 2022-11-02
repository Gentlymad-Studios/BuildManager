using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using static BuildManager.BuildManagerSettings;

namespace BuildManager {
    public class MailProcess {
        public bool processing = false;

        /// <summary>
        /// Execute of existing builds in builds folder
        /// </summary>
        public async void SendMail(SuccessfulBuildTargets targets) {
            if (!processing) {
                processing = true;

                // create filenames & paths
                string buildPath = BuildPath;
                string pathToTools = new DirectoryInfo(Path.Combine(buildPath, Mail.buildToolsFolder)).FullName;
                string mailBodyPath = new DirectoryInfo(Path.Combine(buildPath, Mail.buildToolsFolder, Mail.mailBodyFilename)).FullName;
                string pathToExecutable = Path.Combine(pathToTools, "blat.exe");

                // mail body
                string mailBody = "Hello,\r\n\r\n";
                mailBody += $"I'd like to inform you that a new build for {UnityEngine.Application.productName} is ready!\r\n\r\n";
                mailBody += "Here are the details:\r\n";
                mailBody += $"- Version: {targets.version}\r\n";
                mailBody += $"- Platform: {targets.distributionPlatform.ToString()}\r\n";
                mailBody += $"- Branch: {(string.IsNullOrWhiteSpace(targets.distributionBranch) ? "N/A" : targets.distributionBranch)}\r\n";
                string builds = "";
                foreach (var build in targets.builds) {
                    builds += build.buildTarget.ToString() + ",";
                }
                if (!string.IsNullOrWhiteSpace(builds)) {
                    builds = builds.Remove(builds.Length - 1); 
                }
                mailBody += $"- BuildTargets: {builds}\r\n\r\n";
                mailBody += "Have a nice day humans!\r\n\r\n";
                mailBody += "Sincerly,\r\n";
                mailBody += "B.Gently";
                File.WriteAllText(mailBodyPath, mailBody);

                // create batch file
                string batFile = "";
                batFile += $"{pathToExecutable} {mailBodyPath} -server {Mail.credentials.host} -to {string.Join(",", Mail.recipients)} -subject \"{Mail.subjectText} ({UnityEngine.Application.productName}, {targets.version})\" -f \"{Mail.credentials.displayName}<{Mail.credentials.mail}>\" -u {Mail.credentials.username} -pw {Mail.credentials.password}";
                string batchFileName = Path.Combine(pathToTools, "mail.bat");
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
                if (File.Exists(mailBodyPath)) {
                   File.Delete(mailBodyPath);
                }
                processing = false;
            }
        }
    }
}
