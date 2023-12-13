using System;
using System.IO;
using UnityEngine;

namespace BuildManager {
    public class DefaultAdapter : IAdapter {
        public string CreateBuildTimestamp() {
            return System.DateTime.Now.ToShortDateString() + " | " + System.DateTime.Now.ToShortTimeString();
        }

        public string CreateGitHash() {
            string latestCommitHash = "";
            string path = BuildManagerRuntimeSettings.Instance.GitHeadPath;
            if (File.Exists(path)) {
                string[] lines = File.ReadAllLines(path);
                if (lines != null && lines.Length > 0) {
                    string latestLine = lines[lines.Length - 1];
                    string[] latestLineData = latestLine.Split(' ');
                    if (latestLineData != null && latestLineData.Length > 1) {
                        latestCommitHash = latestLineData[1].Substring(0, 9);
                    }
                }
            } else {
                Debug.LogWarning("Unable to extract GitHash at Path " + path);
            }
            return latestCommitHash;
        }

        public string CreateVersionCode() {
            return "0.0.0";
        }

        public void OnBeforeBuild() {}
    }
}