using InitialPrefabs.Msdf.Runtime;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace InitialPrefabs.Msdf.EditorExtensions {

    public readonly ref struct SerializedObjectScope {
        private readonly SerializedObject serializedObject;

        public SerializedObjectScope(SerializedObject serializedObject) {
            this.serializedObject = serializedObject;
            this.serializedObject.Update();
        }

        public void Dispose() {
            serializedObject.ApplyModifiedProperties();
        }
    }

    public class AtlasGenerator : EditorWindow {
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        [MenuItem("Tools/InitialPrefabs/AtlasGenerator")]
        public static void ShowExample() {
            var wnd = GetWindow<AtlasGenerator>();
            wnd.minSize = new Vector2(400, 325);
            wnd.maxSize = new Vector2(400, 350);
            wnd.titleContent = new GUIContent("AtlasGenerator");
        }

        private GeneratorSettings generatorSettings;
        private SerializedObject serializedObject;

        private SerializedProperty resourcePathProp;
        private SerializedProperty defaultCharsProp;
        private SerializedProperty fontProp;
        private SerializedProperty uniformScaleProp;
        private SerializedProperty paddingProp;
        private SerializedProperty maxAtlasWidthProp;
        private SerializedProperty rangeProp;
        private SerializedProperty uvSpaceProp;
        private SerializedProperty colorTypeProp;
        private SerializedProperty degreesProp;

        private void OnEnable() {
            var guids = AssetDatabase.FindAssets("t: GeneratorSettings");
            if (guids.Length == 0) {
                generatorSettings = CreateInstance<GeneratorSettings>();
                generatorSettings.GeneratorArgs = Args.CreateDefault();
                var path = "Assets/PrimaryAtlasGeneratorSettings.asset";
                AssetDatabase.CreateAsset(generatorSettings, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            } else {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                generatorSettings = AssetDatabase.LoadAssetAtPath<GeneratorSettings>(assetPath);
            }

            serializedObject = new SerializedObject(generatorSettings);
            resourcePathProp = serializedObject.FindProperty(nameof(GeneratorSettings.ResourcePath));
            defaultCharsProp = serializedObject.FindProperty(nameof(GeneratorSettings.DefaultCharacters));
            fontProp = serializedObject.FindProperty(nameof(GeneratorSettings.Font));

            var generatorArgsProp = serializedObject.FindProperty(nameof(GeneratorSettings.GeneratorArgs));
            uniformScaleProp = generatorArgsProp.FindPropertyRelative(nameof(Args.uniform_scale));
            paddingProp = generatorArgsProp.FindPropertyRelative(nameof(Args.padding));
            maxAtlasWidthProp = generatorArgsProp.FindPropertyRelative(nameof(Args.max_atlas_width));
            rangeProp = generatorArgsProp.FindPropertyRelative(nameof(Args.range));
            uvSpaceProp = generatorArgsProp.FindPropertyRelative(nameof(Args.uv_space));
            colorTypeProp = generatorArgsProp.FindPropertyRelative(nameof(Args.color_type));
            degreesProp = generatorArgsProp.FindPropertyRelative(nameof(Args.degrees));

            rootVisualElement.Bind(serializedObject);
        }

        private void OnDisable() {
            serializedObject.Dispose();
        }

        public unsafe void CreateGUI() {
            // Each editor window contains a root VisualElement object
            var root = rootVisualElement;
            // Instantiate UXML
            VisualElement tree = m_VisualTreeAsset.Instantiate();
            root.Add(tree);

            root.Q<Button>("dir").RegisterCallback<MouseUpEvent>(callback => {
                using var _ = new SerializedObjectScope(serializedObject);
                var atlasPath = EditorUtility.OpenFolderPanel("Save Atlas", "Assets", string.Empty);
                if (!string.IsNullOrEmpty(atlasPath)) {
                    resourcePathProp.stringValue = $"{atlasPath}/";
                }
            });

            var export = root.Q<Button>("export");
            root.schedule.Execute(timerState => {
                export.SetEnabled(fontProp.objectReferenceValue != null);
            }).Every(500);

            _ = root.Q<ObjectField>("font").RegisterValueChangedCallback(changeEvt => {
                if (changeEvt.previousValue != changeEvt.newValue && changeEvt.newValue is Font font) {
                    using var _ = new SerializedObjectScope(serializedObject);
                    fontProp.objectReferenceValue = font;
                }
            });

            root.Q<FloatField>("range").BindProperty(rangeProp);
            root.Q<FloatField>("scale").BindProperty(uniformScaleProp);
            root.Q<UnsignedIntegerField>("padding").BindProperty(paddingProp);
            root.Q<UnsignedIntegerField>("width").BindProperty(maxAtlasWidthProp);
            root.Q<EnumField>("uvspace").BindProperty(uvSpaceProp);
            root.Q<EnumField>("colortype").BindProperty(colorTypeProp);
            root.Q<Slider>("degrees").BindProperty(degreesProp);

            root.Q<Button>("export").RegisterCallback<MouseUpEvent>(mouseUpEvent => {
                var font = fontProp.objectReferenceValue;
                if (font == null) {
                    Debug.LogError("Cannot create a texture atlas without a valid Font!");
                    return;
                }

                if (serializedObject.targetObject is not GeneratorSettings settings) {
                    return;
                }

                _ = new LibraryScope("msdf_atlas");

                var savePath = resourcePathProp.stringValue;
                var fontPath = AssetDatabase.GetAssetPath(fontProp.objectReferenceValue);
                var generatorChars = defaultCharsProp.stringValue;

                using var absoluteFontPath = new Utf16(Application.dataPath + fontPath["Assets".Length..]);
                using var absoluteAtlasPath = new Utf16($"{savePath}{font.name}.png");
                using var chars = new Utf16(generatorChars);

                var data = NativeMethods.get_glyph_data_utf16(
                    absoluteFontPath.AsU16Ptr(),
                    absoluteAtlasPath.AsU16Ptr(),
                    chars.AsU16Ptr(),
                    settings.GeneratorArgs);

                var serializedFontData = CreateInstance<SerializedFontData>();
                serializedFontData.FaceData = data.ToRuntimeFaceData();

                var size = data.glyph_data->ElementLen();
                serializedFontData.Glyphs = new RuntimeGlyphData[size];
                for (var i = 0; i < size; i++) {
                    var glyphData = data.glyph_data->ElementAt(i);
                    serializedFontData.Glyphs[i] = glyphData.ToRuntime();
                }
                NativeMethods.drop_byte_buffer(data.glyph_data);
                var soPath = savePath[savePath.IndexOf("Assets")..] + $"{font.name}.asset";
                Debug.Log(soPath);

                AssetDatabase.CreateAsset(serializedFontData, soPath);
                AssetDatabase.ImportAsset(fontPath);
                AssetDatabase.SaveAssets();
            });
        }
    }
}
