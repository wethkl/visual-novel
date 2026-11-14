using UnityEditor;

namespace Verden.EditorTools
{
    public sealed class ArtImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath.IndexOf("/Resources/") < 0) return;
            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
        }
    }
}
