using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace InitialPrefabs.PluginSetup {

    // Copies 
    internal class CopyDllsToProject : EditorWindow {
        [MenuItem("Tools/InitialPrefabs/Copy Plugins")]
        private static void OpenWindow() {

        }

        [SerializeField]
        private VisualTreeAsset layout = default;
    }
}
