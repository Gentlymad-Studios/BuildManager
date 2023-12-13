using System;

namespace BuildManager {
    public interface IAdapter {
        string CreateBuildTimestamp();

        string CreateGitHash();

        string CreateVersionCode();

        void OnBeforeBuild();
    }
}