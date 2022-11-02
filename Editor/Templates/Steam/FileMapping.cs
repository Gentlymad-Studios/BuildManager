using System.Runtime.Serialization;

namespace BuildManager.Templates.Steam {
    [DataContract]
    public class FileMapping {
        // This can be a full path, or a path relative to ContentRoot
        [DataMember]
        public string LocalPath { get; set; }
        // This is a path relative to the install folder of your gam
        [DataMember]
        public string DepotPath { get; set; }
        // If LocalPath contains wildcards, setting this means that all
        // matching files within subdirectories of LocalPath will also
        // be included.
        [DataMember]
        public int recursive { get; set; }

        /// <summary>
        /// Create a simplified FileMapping.
        /// </summary>
        /// <returns></returns>
        public static FileMapping Create() {
            FileMapping fileMapping = new FileMapping();
            fileMapping.LocalPath = "*";
            fileMapping.recursive = 1;
            fileMapping.DepotPath = ".";
            return fileMapping;
        }

        /// <summary>
        /// Cached accessor to retrieve a simplified filemapping
        /// </summary>
        public static readonly FileMapping DefaultFileMapping = Create();
    }
}