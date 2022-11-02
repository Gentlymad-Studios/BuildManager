using System;
using System.Collections.Generic;

namespace BuildManager.Templates.GOG {
    [Serializable]
    public class Depot {
        public string name;
        public string folder;
        public List<string> languages = new List<string>();
        public List<string> osBitness = new List<string>();
    }
}
