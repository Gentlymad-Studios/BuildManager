using System;
using System.Collections.Generic;

namespace BuildManager.Templates.GOG {
    [Serializable]
    public class Product {
        public string name;
        public string productId;
        public List<Depot> depots = new List<Depot>();
        public List<Task> tasks = new List<Task>();
        public List<Depot> supportDepots = new List<Depot>();
    }
}
