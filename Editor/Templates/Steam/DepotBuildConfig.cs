using Gameloop.Vdf.JsonConverter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;
using UnityEngine;

namespace BuildManager.Templates.Steam {
    [DataContract]
    public class DepotBuildConfig {
        // Set your assigned depot ID here
        [DataMember]
        public int DepotID { get; set; }
        // Set a root for all content.
        // All relative paths specified below (LocalPath in FileMapping entries, and FileExclusion paths)
        // will be resolved relative to this root.
        // If you don't
        // ContentRoot, then it will be assumed to be
        // the location of this script file, which probably isn't what you want
        [DataMember]
        public string ContentRoot { get; set; }
        // include all files recursivley
        [DataMember]
        public FileMapping FileMapping { get; set; }
        // but exclude all symbol files  
        // This can be a full path, or a path relative to ContentRoot
        [DataMember]
        public string FileExclusion { get; set; }
        // the list of file exclusions that is later added in the serialize step
        public string[] customfileExclusions = new string[] { };

        /// <summary>
        /// Serialize a DepotBuildConfig object into a steam .vdf file
        /// </summary>
        /// <param name="depotBuildConfig"></param>
        /// <returns></returns>
        public static string SerializeToVdf(DepotBuildConfig depotBuildConfig) {
            // prepend the name of the class to the start of the file
            // this is special behavior, not part of VDF specs but needed by the steam builder.
            string PrependHeader(string vdfContent) {
                return "\"" + nameof(DepotBuildConfig) + "\"\r\n" + vdfContent;
            }

            // add a list of file exclusions
            // this is special behavior, not part of VDF specs but needed by the steam builder.
            string AddFileExclusions(string vdfContent) {
                int fileExclusionIndex = vdfContent.IndexOf("\"" + nameof(FileExclusion) + "\"");
                string exclusionContent = "";
                foreach (var exclusion in depotBuildConfig.customfileExclusions) {
                    exclusionContent += "\"" + nameof(FileExclusion) + "\" \"" + exclusion + "\"\r\n\t";
                }
                return vdfContent.Insert(fileExclusionIndex, exclusionContent);
            }

            string vdf = "";
            JToken token = (JToken)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(depotBuildConfig));
            vdf = AddFileExclusions(PrependHeader(token.ToVdf().ToString()));
            return vdf;
        }

        /// <summary>
        /// Create a simplified config file.
        /// </summary>
        /// <param name="depotID"></param>
        /// <param name="contentRoot"></param>
        /// <returns></returns>
        public static DepotBuildConfig Create(int depotID, string contentRoot, string[] fileExclusions) {
            DepotBuildConfig buildConfig = new DepotBuildConfig();
            buildConfig.DepotID = depotID;
            buildConfig.ContentRoot = contentRoot;
            buildConfig.FileExclusion = "*.bnm";
            buildConfig.customfileExclusions = fileExclusions;
            buildConfig.FileMapping = FileMapping.DefaultFileMapping;
            return buildConfig;
        }

    }
}

