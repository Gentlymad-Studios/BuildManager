using System;
using System.Collections.Generic;

namespace BuildManager.Templates.GOG {
    [Serializable]
    public class Task {
        public string type = "FileTask";
        public string name;
        public List<string> languages = new List<string>();
        public string category = "game";
        public string path;
        public bool isPrimary = true;
        public List<string> osBitness = new List<string>();
        public string arguments;
    }
}
