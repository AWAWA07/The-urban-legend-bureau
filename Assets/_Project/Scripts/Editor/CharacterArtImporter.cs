using UnityEditor;

namespace UrbanLegendBureau.EditorTools
{
    /// <summary>
    /// 인물 그림(Resources/Characters 아래)은 언제나 한 장짜리 Sprite 로 들여온다.
    ///
    /// 2D 프로젝트는 PNG 를 여러 장 묶음(Multiple)으로 들여올 때가 있다. 그러면 처음 들여올 때 잘린 틀이 남아,
    /// 그림을 같은 이름으로 바꿔 넣어도 예전 크기로 잘려 보인다. 그림을 바꿔 넣을 일이 잦으므로 여기서 막는다.
    /// </summary>
    public class CharacterArtImporter : AssetPostprocessor
    {
        private const string Folder = "Assets/_Project/Resources/Characters/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
        }
    }
}
