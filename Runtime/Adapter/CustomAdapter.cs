using System;
using UnityEngine;

namespace BuildManager {
    [Serializable]
    public abstract class CustomAdapter : ScriptableObject, IAdapter {
        public abstract string CreateBuildTimestamp();
        public abstract string CreateGitHash();
        public abstract string CreateVersionCode();
        public abstract void OnBeforeBuild();
    }
}