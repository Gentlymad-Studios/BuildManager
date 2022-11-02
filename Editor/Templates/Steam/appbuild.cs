using Gameloop.Vdf.JsonConverter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Settings = BuildManager.BuildManagerSettings;

namespace BuildManager.Templates.Steam {
    [DataContract]
    public class Appbuild {
        [DataMember]
        public int appid { get; set; }
        [DataMember]
        public string desc { get; set; } // description for this build
        [DataMember]
        public string buildoutput { get; set; } // build output folder for .log, .csm & .csd files, relative to location of this file
        [DataMember]
        public string contentroot { get; set; } // root content folder, relative to location of this file
        [DataMember]
        public string setlive { get; set; } // branch to set live after successful build, non if empty
        [DataMember]
        public int preview { get; set; } // to enable preview builds
        [DataMember]
        public string local { get; set; } // set to flie path of local content server
        [DataMember]
        public Dictionary<int, string> depots { get; set; }

        /// <summary>
        /// Serialize a DepotBuildConfig object into a steam .vdf file
        /// </summary>
        /// <param name="depotBuildConfig"></param>
        /// <returns></returns>
        public static string SerializeToVdf(Appbuild appBuild) {
            // prepend the name of the class to the start of the file
            // this is special behavior, not part of VDF specs but needed by the steam builder.
            string PrependHeader(string vdfContent) {
                return "\"" + nameof(Appbuild) + "\"\r\n" + vdfContent;
            }

            string vdf = "";
            JToken token = (JToken)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(appBuild));
            vdf = PrependHeader(token.ToVdf().ToString());
            return vdf;
        }

        /// <summary>
        /// Create a simplified app build file.
        /// </summary>
        /// <param name="appID"></param>
        /// <param name="setLive"></param>
        /// <param name="desc"></param>
        /// <returns></returns>
        public static Appbuild Create(int appID, string setLive, string desc) {
            Appbuild appbuild = new Appbuild();
            appbuild.appid = appID;
            appbuild.preview = 0;
            appbuild.local = "";
            appbuild.setlive = setLive;
            appbuild.desc = desc;
            appbuild.buildoutput = Settings.Steam.paths.LogOutputFolder;
            appbuild.contentroot = Settings.General.paths.BuildsFolder;
            appbuild.depots = new Dictionary<int, string>();
            return appbuild;
        }
    }
}