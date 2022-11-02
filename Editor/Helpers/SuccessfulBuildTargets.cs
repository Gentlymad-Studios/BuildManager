using System;
using System.Collections.Generic;
using UnityEditor;

namespace BuildManager {
    public enum DistributionPlatform {
        Steam = 0,
        GOG = 1,
        Magenta = 2,
        Other = 3
    }

    [Serializable]
    public class SuccessfulBuildTargets {
        public string version;
        public string distributionBranch = "";
        public DistributionPlatform distributionPlatform;

        public List<SuccessfulBuildTarget> builds = new List<SuccessfulBuildTarget>();
        private List<BuildTarget> buildTargets = new List<BuildTarget>();

        public int Count {
            get {
                if (builds == null) {
                    return 0;
                }
                return builds.Count;
            }
        }

        public List<BuildTarget> GetBuildTargets() {
            buildTargets.Clear();
            foreach (var build in builds) {
                buildTargets.Add(build.buildTarget);
            }
            return buildTargets;
        }

        public void Add(BuildTarget buildTarget, string targetPath) {
            builds.Add(new SuccessfulBuildTarget() { buildTarget = buildTarget, targetPath = targetPath });
        }
    }
}
