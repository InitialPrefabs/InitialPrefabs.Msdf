using System.Text.RegularExpressions;
using UnityEditor;

namespace InitialPrefabs.Msdf.EditorExtensions {
#if !DISABLE_MSDF_IMPORTER
    internal class FontAtlasTextureImporter : AssetPostprocessor {

        private void OnPreprocessAsset() {
            // Ensure we are only using a texture importer
            if (assetImporter is not TextureImporter textureImporter) {
                return;
            }

            // Ensure we have a config for our importer
            var importConfig = GetFirst();
            if (importConfig == null) {
                return;
            }

            // Ensure that our assetPath matches what we're trying to import.
            var regex = new Regex(importConfig.FilePattern);
            if (!regex.IsMatch(assetPath)) {
                return;
            }

            // For each platform configured, set the texture importer.
            foreach (var perPlatform in importConfig.PlatformSettings) {
                textureImporter.SetTextureSettings(perPlatform.Settings);
                textureImporter.SetPlatformTextureSettings(perPlatform.PlatformSettings);
            }
        }

        private static FontAtlasImportConfig GetFirst() {
            var guids = AssetDatabase.FindAssets($"t: {nameof(FontAtlasImportConfig)}");
            var importConfigs = new FontAtlasImportConfig[guids.Length];

            for (var i = 0; i < guids.Length; i++) {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                importConfigs[i] = AssetDatabase.LoadAssetAtPath<FontAtlasImportConfig>(path);
            }

            if (guids.Length > 1) {
                foreach (var config in importConfigs) {
                    if (config.IsPrimary) {
                        return config;
                    }
                }
            } else if (guids.Length == 1) {
                return importConfigs[0];
            }
            return null;
        }
    }
#endif
}
