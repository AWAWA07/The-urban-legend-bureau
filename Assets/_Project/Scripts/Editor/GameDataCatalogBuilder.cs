using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UrbanLegendBureau.Data;

namespace UrbanLegendBureau.EditorTools
{
    /// <summary>
    /// Data 폴더를 훑어 GameDataCatalog을 채운다.
    /// 괴담을 추가할 때 카탈로그에 손으로 끌어다 놓는 것을 잊는 실수를 막는다.
    /// </summary>
    public static class GameDataCatalogBuilder
    {
        private const string CatalogPath = "Assets/_Project/Data/Config/GameDataCatalog.asset";

        [MenuItem("UrbanLegendBureau/Data/Rebuild Game Data Catalog")]
        public static string Rebuild()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalogSO>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<GameDataCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
                Debug.Log("[GameDataCatalogBuilder] 카탈로그를 새로 만들었다: " + CatalogPath);
            }

            var cases = Collect<CaseSO>();
            var legends = Collect<LegendSO>();
            var rules = Collect<RuleSO>();
            var clues = Collect<ClueSO>();
            var webPages = Collect<WebPageSO>();
            var actions = Collect<InvestigationActionSO>();

            var so = new SerializedObject(catalog);
            so.Update();
            Fill(so, "_cases", cases);
            Fill(so, "_legends", legends);
            Fill(so, "_rules", rules);
            Fill(so, "_clues", clues);
            Fill(so, "_webPages", webPages);
            Fill(so, "_investigationActions", actions);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            var message = "[GameDataCatalogBuilder] 사건 " + cases.Count + " / 괴담 " + legends.Count +
                          " / 규칙 " + rules.Count +
                          " / 단서 " + clues.Count +
                          " / 게시글 " + webPages.Count +
                          " / 조사행동 " + actions.Count + " 등록";
            foreach (var c in cases) message += "\n  사건  " + c.Id;
            foreach (var l in legends) message += "\n  괴담  " + l.Id;
            foreach (var r in rules) message += "\n  규칙  " + r.Id;
            foreach (var c in clues) message += "\n  단서  " + c.Id;
            foreach (var w in webPages) message += "\n  게시글 " + w.Id;
            foreach (var a in actions) message += "\n  조사행동 " + a.Id;
            Debug.Log(message);
            return message;
        }

        /// <summary>해당 타입의 에셋을 전부 모아 ID 순으로 정렬한다. 에셋 검색 순서는 보장되지 않으므로 정렬해 결과를 재현 가능하게 한다.</summary>
        private static List<T> Collect<T>() where T : ScriptableObject, IGameDataAsset
        {
            var result = new List<T>();
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) result.Add(asset);
            }
            result.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return result;
        }

        private static void Fill<T>(SerializedObject so, string propertyName, List<T> assets) where T : Object
        {
            var list = so.FindProperty(propertyName);
            if (list == null)
            {
                Debug.LogError("[GameDataCatalogBuilder] 프로퍼티를 찾지 못했다: " + propertyName);
                return;
            }

            list.arraySize = assets.Count;
            for (int i = 0; i < assets.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
            }
        }
    }
}
