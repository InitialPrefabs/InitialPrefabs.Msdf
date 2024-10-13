using System;
using UnityEditor;
using UnityEngine;

namespace InitialPrefabs.Msdf.EditorExtensions {

    /// <summary>
    /// Configurations for the FontAtlasTextureImporter. You can have multiple Configs, however you must mark 1 as the primary.
    /// </summary>
    [CreateAssetMenu(menuName = "InitialPrefabs/MSDF/FontAtlasImportConfig")]
    public class FontAtlasImportConfig : ScriptableObject {

        public Span<PerPlatformSettings> PlatformSettings => new Span<PerPlatformSettings>(perPlatformSettings);

        [Serializable]
        public class PerPlatformSettings {
            [HideInInspector]
            public string Name;
            public TextureImporterSettings Settings;
            public TextureImporterPlatformSettings PlatformSettings;
        }

        [Tooltip("Is this the primary import config that should run? If no primary configs exist, the FontAtlasTextureImporter will take the first available config.")]
        public bool IsPrimary;

        [Tooltip("How are the file names formatted for the FontAtlas? This uses regex internally.")]
        [SerializeField]
        public string FilePattern = @"(_MSDFAtlas.png)$";

        [Tooltip("What are the platform settings?")]
        [SerializeField]
        private PerPlatformSettings[] perPlatformSettings;

        private void OnValidate() {
            // Sets the name so it's easier to see in the inspeector
            for (var i = 0; i < perPlatformSettings.Length; i++) {
                ref var p = ref perPlatformSettings[i];
                p.Name = p.PlatformSettings.name;
            }
        }
    }
}
