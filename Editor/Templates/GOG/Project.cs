using System;
using System.Collections.Generic;

namespace BuildManager.Templates.GOG {
    [Serializable]
    public class Project {
        public string baseProductId;
        public string clientId;
        public string clientSecret;
        public string version;
        public string installDirectory;
        public string name;
        public string platform;
        public List<string> tags = new List<string>();
        public string languageMode = "separate";
        public List<Product> products = new List<Product>();
        public bool scriptInterpreter = true;
    }
}
