using UnityEditor;

namespace Eldoria.EditorTools
{
    /// <summary>
    /// Import settings for the faction flags (Textures/Flags): kept at their own size (no power-of-two rescale, so
    /// they stay 3:2), without mipmaps or compression, since they're only drawn as UI.
    /// </summary>
    public class FlagTextureImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SquadBuilder.FlagDir + "/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaSource = TextureImporterAlphaSource.None;
        }
    }
}
