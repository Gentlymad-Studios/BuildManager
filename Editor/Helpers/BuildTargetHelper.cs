using UnityEditor;
using static EditorHelper.UI;

namespace BuildManager {
    public class BuildTargetHelper {
        public string name;
        public ToggableButton button = null;
        public BuildTarget target;
    }
}
