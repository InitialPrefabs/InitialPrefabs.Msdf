using PlasticGui.WorkspaceWindow.Items;
using SharpFont;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace InitialPrefabs.Msdf.Generator {

    public class FontGenerator : EditorWindow {
        [SerializeField]
        private VisualTreeAsset visualTreeAsset = default;

        [MenuItem("InitialPrefabs/Font Generator")]
        private static void ShowWindow() {
            FontGenerator wnd = GetWindow<FontGenerator>();
            wnd.titleContent = new GUIContent("Font Generator");
        }

        private Library library;
        private Font font;

        private void OnEnable() {
            library = new Library();
        }

        private void OnDisable() {
            library.Dispose();
        }

        public void CreateGUI() {
            VisualElement root = visualTreeAsset.CloneTree();

            Button generateFont = root.Q<Button>("generate-font");
            ObjectField fontField = root.Q<ObjectField>("font-path");
            fontField.RegisterValueChangedCallback(changeEvt => {
                Debug.Log("clicked");
                if (changeEvt.previousValue != changeEvt.newValue) {
                    font = (Font)changeEvt.newValue;
                    Debug.Log(font.name);
                }
            });

            generateFont.RegisterCallback<MouseUpEvent>(_ => {
            });

            Debug.Log("test");

            rootVisualElement.Add(root);
        }

        private void GenerateFont(string path) {
            using Face face = new Face(library, path);
        }
    }
}
