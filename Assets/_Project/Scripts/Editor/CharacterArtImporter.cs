using UnityEditor;
using UnityEngine;

namespace UrbanLegendBureau.EditorTools
{
    /// <summary>
    /// 인물 그림(Resources/Characters 아래)은 언제나 한 장짜리 Sprite 로 들여온다.
    ///
    /// 2D 프로젝트는 PNG 를 여러 장 묶음(Multiple)으로 들여올 때가 있다. 그러면 처음 들여올 때 잘린 틀이 남아,
    /// 그림을 같은 이름으로 바꿔 넣어도 예전 크기로 잘려 보인다. 그림을 바꿔 넣을 일이 잦으므로 여기서 막는다.
    ///
    /// 현장에서 걷는 그림(Art/Characters/이름/Field 아래)도 여기서 맞춘다. 발밑이 기준점이라
    /// 그림을 바꿔 끼워도 바닥에 선 자리가 그대로다.
    /// </summary>
    public class CharacterArtImporter : AssetPostprocessor
    {
        private const string Folder = "Assets/_Project/Resources/Characters/";
        private const string ArtFolder = "Assets/_Project/Art/Characters/";

        /// <summary>
        /// 현장 그림의 한 단위당 픽셀. 칸 높이가 약 480px 이다.
        /// 한영은 키 2.35, 차지한은 그보다 조금 큰 2.55 가 되게 한다. 예전 도형 인물(1.8)보다 크게 보이게 키웠다. 전신 일러스트의 키 차이와 같다.
        /// </summary>
        private const float HanyoungFieldPpu = 200f;
        private const float ChajihanFieldPpu = 188f;

        private void OnPreprocessTexture()
        {
            bool field = assetPath.StartsWith(ArtFolder) && assetPath.Contains("/Field/");
            if (!assetPath.StartsWith(Folder) && !field) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            if (!field) return;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.spritePixelsPerUnit = assetPath.Contains("/Chajihan/") ? ChajihanFieldPpu : HanyoungFieldPpu;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
