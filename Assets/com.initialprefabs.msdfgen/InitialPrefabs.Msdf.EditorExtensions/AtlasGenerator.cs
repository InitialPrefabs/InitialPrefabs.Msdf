using InitialPrefabs.Msdf.Runtime;
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace InitialPrefabs.Msdf.EditorExtensions {

    public class AtlasGenerator : EditorWindow {

        /// <summary>
        /// Stores a bunch of sizes to downscale a glyph by before writing it to the atlas.
        /// </summary>
        private static readonly int[] DownScaleSamples = { 8, 16, 32, 64, 128 };

        /// <summary>
        /// Stores a bunch of atlas sizes.
        /// </summary>
        private static readonly int[] AtlasSizes = { 128, 256, 512, 1024, 2048, 4096 };

        [MenuItem("Tools/InitialPrefabs/AtlasGenerator")]
        public static void ShowWindow() {
            var wnd = GetWindow<AtlasGenerator>();
            // wnd.minSize = new Vector2(400, 485);
            // wnd.maxSize = new Vector2(400, 485);
            wnd.titleContent = new GUIContent("AtlasGenerator");
        }

        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        private GeneratorSettings generatorSettings;
        private SerializedObject serializedObject;

        // The following stores a bunch of properties associated with the Args struct.
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
        private SerializedProperty threadCountProp;
        private SerializedProperty scaleToPO2Prop;

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
            threadCountProp = generatorArgsProp.FindPropertyRelative(nameof(Args.thread_count));
            scaleToPO2Prop = generatorArgsProp.FindPropertyRelative(nameof(Args.scale_texture_to_po2));

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

            // Ensure that the export button is enabled/disable if the resourcePath is 'valid'.
            var dirLabel = root.Q<Label>("dir-label");
            var exportBtn = root.Q<Button>("export");
            var pullAllBtn = root.Q<Button>("pull-all");
            _ = root.schedule.Execute(timerState => {
                var dirExists = Directory.Exists(resourcePathProp.stringValue) && !string.IsNullOrEmpty(resourcePathProp.stringValue);
                exportBtn.SetEnabled(fontProp.objectReferenceValue != null && dirExists);
                dirLabel.text = resourcePathProp.stringValue;
                pullAllBtn.SetEnabled(fontProp.objectReferenceValue is Font font && !font.dynamic);
            }).Every(500);

            var fontField = root.Q<ObjectField>("font");
            fontField.value = fontProp.objectReferenceValue;

            _ = fontField.RegisterValueChangedCallback(changeEvt => {
                if (changeEvt.previousValue != changeEvt.newValue && changeEvt.newValue is Font font) {
                    using var _ = new SerializedObjectScope(serializedObject);
                    fontProp.objectReferenceValue = font;
                }
            });

            var scaleField = root.Q<SliderInt>("scale");
            var scaleLabel = root.Q<Label>("scale-label");

            // Set the downscale low and high values
            scaleField.value = Array.IndexOf(DownScaleSamples, (int)(1.0f / uniformScaleProp.floatValue));
            scaleField.lowValue = 0;
            scaleField.highValue = DownScaleSamples.Length - 1;

            // Initialize the label for the downscaler
            scaleLabel.text = DownScaleSamples[scaleField.value].ToString();

            _ = scaleField.RegisterValueChangedCallback(changeEvt => {
                if (changeEvt.previousValue != changeEvt.newValue) {
                    using var _ = new SerializedObjectScope(serializedObject);
                    var downscale = DownScaleSamples[changeEvt.newValue];
                    uniformScaleProp.floatValue = 1.0f / downscale;
                    scaleLabel.text = downscale.ToString();
                }
            });

            var maxAtlasWidthSlider = root.Q<SliderInt>("width");
            var atlasWidthLabel = root.Q<Label>("width-label");

            // Set the max atlas low and high value
            maxAtlasWidthSlider.value = Array.IndexOf(AtlasSizes, (int)maxAtlasWidthProp.uintValue);
            maxAtlasWidthSlider.lowValue = 0;
            maxAtlasWidthSlider.highValue = AtlasSizes.Length - 1;

            // Initialize the label for the atlas width
            atlasWidthLabel.text = maxAtlasWidthProp.uintValue.ToString();

            _ = maxAtlasWidthSlider.RegisterValueChangedCallback(changeEvt => {
                if (changeEvt.previousValue != changeEvt.newValue) {
                    using var _ = new SerializedObjectScope(serializedObject);
                    maxAtlasWidthProp.uintValue = (uint)AtlasSizes[changeEvt.newValue];
                    atlasWidthLabel.text = maxAtlasWidthProp.uintValue.ToString();
                }
            });

            var charField = root.Q<TextField>("chars");
            root.Q<Button>("reset").RegisterCallback<MouseUpEvent>(_ => {
                charField.value = "☺☻♥♦♣♠•◘○◙♂♀♪♫☼►◄↕‼¶§▬↨↑↓→←∟↔▲▼!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~⌂ÇüéâäàåçêëèïîìÄÅÉæÆôöòûùÿÖÜ¢£¥₧ƒáíóúñÑªº¿⌐¬½¼¡«»░▒▓│┤╡╢╖╕╣║╗╝╜╛┐└┴┬├─┼╞╟╚╔╩╦╠═╬╧╨╤╥╙╘╒╓╫╪┘┌█▄▌▐▀αßΓπΣσµτΦΘΩδ∞φε∩≡±≥≤⌠⌡÷≈°∙·√ⁿ²■✖✕ ";
            });

            pullAllBtn.RegisterCallback<MouseUpEvent>(_ => {
                if (fontProp.objectReferenceValue is Font font && !font.dynamic) {
                    var characterInfo = font.characterInfo;
                    var sb = new StringBuilder(characterInfo.Length);
                    foreach (var charInfo in characterInfo) {
                        var c = (char)charInfo.index;
                        sb.Append(c);
                    }
                    charField.value = sb.ToString();
                }
            });

            // Just bind the property directly to the fields from UI Toolkit, we don't need custom logic.
            charField.BindProperty(defaultCharsProp);
            root.Q<FloatField>("range").BindProperty(rangeProp);
            root.Q<UnsignedIntegerField>("padding").BindProperty(paddingProp);
            root.Q<EnumFlagsField>("uvspace").BindProperty(uvSpaceProp);
            root.Q<EnumField>("colortype").BindProperty(colorTypeProp);
            root.Q<Slider>("degrees").BindProperty(degreesProp);
            root.Q<SliderInt>("thread-count").BindProperty(threadCountProp);
            root.Q<Toggle>("scale-to-po2").BindProperty(scaleToPO2Prop);

            exportBtn.RegisterCallback<MouseUpEvent>(mouseUpEvent => {
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
                using var absoluteAtlasPath = new Utf16($"{savePath}{font.name}_MSDFAtlas.png");
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
                var soPath = savePath[savePath.IndexOf("Assets")..] + $"{font.name}_MSDFAtlas.asset";

                AssetDatabase.CreateAsset(serializedFontData, soPath);
                AssetDatabase.SaveAssets();

                AssetDatabase.ImportAsset(soPath, ImportAssetOptions.ForceUpdate);

                var relativeAtlasPath = savePath[savePath.IndexOf("Assets")..] + $"{font.name}_MSDFAtlas.png";
                AssetDatabase.ImportAsset(relativeAtlasPath, ImportAssetOptions.ForceUpdate);
                // Release memory allocated on the heap that we no longer need.
                NativeMethods.drop_byte_buffer(data.glyph_data);
            });
        }
    }
}
