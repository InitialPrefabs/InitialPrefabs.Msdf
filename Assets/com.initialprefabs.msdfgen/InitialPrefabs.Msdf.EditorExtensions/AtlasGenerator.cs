using InitialPrefabs.Msdf.EditorExtensions;
using InitialPrefabs.Msdf.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class AtlasGenerator : EditorWindow {
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("Tools/InitialPrefabs/AtlasGenerator")]
    public static void ShowExample() {
        var wnd = GetWindow<AtlasGenerator>();
        wnd.titleContent = new GUIContent("AtlasGenerator");
    }

    private Font font = null;
    private string savePath = string.Empty;
    private string generatorChars = string.Empty;
    private float range = 4.0f;
    private float uniformScale = 1 / 32f;
    private uint padding = 10;
    private uint atlasWidth = 512;
    private UVSpace uVSpace = UVSpace.OneMinusV;

    public unsafe void CreateGUI() {
        // Each editor window contains a root VisualElement object
        var root = rootVisualElement;
        // Instantiate UXML
        VisualElement tree = m_VisualTreeAsset.Instantiate();
        root.Add(tree);

        root.Q<VisualElement>("params").SetEnabled(!string.IsNullOrEmpty(savePath));

        root.Q<Button>("dir").RegisterCallback<MouseUpEvent>(callback => {
            var atlasPath = EditorUtility.OpenFolderPanel("Save Atlas", "Assets", string.Empty);
            savePath = string.IsNullOrEmpty(atlasPath) ? string.Empty : $"{atlasPath}/";
            root.Q<Label>("dir-label").text = savePath;
            root.Q<VisualElement>("params").SetEnabled(!string.IsNullOrEmpty(savePath));
        });

        root.Q<Button>("export").SetEnabled(font != null);
        _ = root.Q<ObjectField>("font").RegisterValueChangedCallback(changeEvt => {
            if (changeEvt.previousValue != changeEvt.newValue) {
                font = (Font)changeEvt.newValue;
                root.Q<Button>("export").SetEnabled(font != null);
            }
        });

        var charField = root.Q<TextField>("chars");
        generatorChars = charField.value;

        _ = root.Q<TextField>("chars").RegisterValueChangedCallback(changeEvt => {
            if (changeEvt.previousValue != changeEvt.newValue) {
                generatorChars = changeEvt.newValue;
            }
        });

        _ = root.Q<FloatField>("range").RegisterValueChangedCallback(changeEvt => {
            if (changeEvt.previousValue != changeEvt.newValue) {
                range = changeEvt.newValue;
            }
        });

        _ = root.Q<FloatField>("scale").RegisterValueChangedCallback(changeEvt => {
            if (changeEvt.previousValue != changeEvt.newValue) {
                uniformScale = changeEvt.newValue;
            }
        });

        _ = root.Q<UnsignedIntegerField>("padding").RegisterValueChangedCallback(changeEvt => {
            if (changeEvt.previousValue != changeEvt.newValue) {
                padding = changeEvt.newValue;
            }
        });

        _ = root.Q<UnsignedIntegerField>("width").RegisterValueChangedCallback(changeEvt => {
            if (changeEvt.previousValue != changeEvt.newValue) {
                atlasWidth = changeEvt.newValue;
            }
        });

        _ = root.Q<EnumField>("uvspace").RegisterValueChangedCallback(changeEvt => {
            if (changeEvt.previousValue != changeEvt.newValue) {
                uVSpace = (UVSpace)changeEvt.newValue;
            }
        });

        root.Q<Button>("export").RegisterCallback<MouseUpEvent>(mouseUpEvent => {
            if (font == null) {
                Debug.LogError("Cannot create a texture atlas without a valid Font!");
                return;
            }
            _ = new LibraryScope("msdf_atlas");
            var fontPath = AssetDatabase.GetAssetPath(font);
            using var absoluteFontPath = new Utf16(Application.dataPath + fontPath["Assets".Length..]);
            using var absoluteAtlasPath = new Utf16($"{savePath}{font.name}.png");
            using var chars = new Utf16(generatorChars);

            var args = new Args {
                uniform_scale = uniformScale,
                padding = padding,
                uv_space = uVSpace,
                max_atlas_width = atlasWidth,
                range = range,
            };

            var data = NativeMethods.get_glyph_data_utf16(
                absoluteFontPath.AsU16Ptr(),
                absoluteAtlasPath.AsU16Ptr(),
                chars.AsU16Ptr(), args);
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
            AssetDatabase.SaveAssets();
        });
    }
}
