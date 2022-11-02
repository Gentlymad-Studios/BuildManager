using System;
using UnityEditor;

namespace BuildManager {
    [Serializable]
    public class SuccessfulBuildTarget {
        public BuildTarget buildTarget;
        public string targetPath;
    }
}
