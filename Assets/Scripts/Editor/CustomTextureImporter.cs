using UnityEngine;
using UnityEditor;

public class CustomTextureImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        TextureImporter textureImporter = (TextureImporter)assetImporter;

        if (!assetPath.Contains("UI_Visteon"))
            return;


        // Check if the texture is already a Sprite (Avoid redundant settings)
        if (textureImporter.textureType != TextureImporterType.Sprite)
        {
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spritePixelsPerUnit = 100; // Adjust PPU as needed
            textureImporter.filterMode = FilterMode.Bilinear; // Default filter mode
            textureImporter.mipmapEnabled = false; // Disable mipmaps for sprites
            textureImporter.alphaIsTransparency = true; // Enable transparency handling
        }
    }
}