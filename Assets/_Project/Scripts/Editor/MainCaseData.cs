using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UrbanLegendBureau.Data;

namespace UrbanLegendBureau.EditorTools
{
    /// <summary>
    /// 본편 사건 데이터를 코드로 맞춘다. 메뉴 한 번이면 없는 에셋은 만들고 있는 것은 고친다(여러 번 눌러도 같다).
    /// 조사 행동, 단서, 괴담에 단서·행동 걸기, 수사 노트의 현장 단서 칸까지 다룬다.
    /// 문구는 Strings.txt 에 따로 둔다. 다 맞춘 뒤 게임 데이터 목록(GameDataCatalog)을 다시 짓는다.
    /// </summary>
    public static partial class MainCaseData
    {
        private const string DataRoot = "Assets/_Project/Data/";

        [MenuItem("UrbanLegendBureau/Data/Apply Main Case Data")]
        public static void Apply()
        {
            ApplyCase001();
            ApplyCase002();
            ApplyNewCases();
            AssetDatabase.SaveAssets();
            EditorApplication.ExecuteMenuItem("UrbanLegendBureau/Data/Rebuild Game Data Catalog");
            Debug.Log("[MainCaseData] 본편 사건 데이터를 맞췄다");
        }

        // ------------------------------------------------------------- 사건 #001 #002

        private static void ApplyCase001()
        {
            // 302호 문 앞 우편물: 사흘째 아무도 드나들지 않았다(엉뚱한 짝을 고르면 틀린다).
            Clue("clue_test_009", "clue.test.009.text", ClueType.Physical, "InvestigationPoint_PostPile");
            Action("action_test_012", "action.test.012", 10, "clue_test_009");
            // 책상의 삭제 기록 수첩. 예전에는 괴담넷 글로만 얻던 기록을 현장에서도 얻는다.
            Action("action_test_011", "action.test.011", 15, "clue_test_005");

            var legend = Load<LegendSO>("Legends/legend_test_001");
            AddRef(legend, "_clues", Load<ClueSO>("Clues/clue_test_009"));
            AddRef(legend, "_investigationActions", Load<InvestigationActionSO>("Actions/action_test_011"));
            AddRef(legend, "_investigationActions", Load<InvestigationActionSO>("Actions/action_test_012"));

            var d = Load<CaseDeductionSO>("Cases/deduction_test_001");
            FieldClue(d, "clue_test_005", "ui.casefile.place_post_book", "");
            FieldClue(d, "clue_test_009", "ui.casefile.place_post_pile", "");
        }

        private static void ApplyCase002()
        {
            // 홀 게시판의 관리실 안내문. 예전에는 괴담넷 글로만 얻던 안내를 현장에서도 얻는다.
            Action("action_test_010", "action.test.010", 10, "clue_test_007");
            var legend = Load<LegendSO>("Legends/legend_test_002");
            AddRef(legend, "_investigationActions", Load<InvestigationActionSO>("Actions/action_test_010"));

            var d = Load<CaseDeductionSO>("Cases/deduction_test_002");
            FieldClue(d, "clue_test_007", "ui.casefile.place_elev_board", "");
            // 원글 제목이 '새벽에 집에 오다 겪은 일' 로 바뀌었다(구오 빌라 주민의 글).
            BlankAnswers(d, "target", "새벽", "~[새벽|새벽녘|새벽에]", "dawn");
        }

        // ------------------------------------------------------------- 만들기 도구

        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(DataRoot + path + ".asset");

        private static T Ensure<T>(string folder, string id) where T : ScriptableObject
        {
            string path = DataRoot + folder + "/" + id + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            asset.name = id;
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static ClueSO Clue(string id, string textId, ClueType type, string sourceId, params string[] required)
        {
            var clue = Ensure<ClueSO>("Clues", id);
            var so = new SerializedObject(clue);
            so.FindProperty("_clueId").stringValue = id;
            so.FindProperty("_clueTextId").stringValue = textId;
            so.FindProperty("_clueType").intValue = (int)type;
            so.FindProperty("_discoverySource").intValue = 0;
            so.FindProperty("_sourceId").stringValue = sourceId;
            SetStrings(so.FindProperty("_requiredClueIds"), required);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(clue);
            return clue;
        }

        /// <summary>조사 행동. 글 ID 는 keyPrefix + .name/.desc/.result. 살피면 minutes 분이 흐르고 rewardClue 를 준다.</summary>
        private static InvestigationActionSO Action(string id, string keyPrefix, int minutes, string rewardClue, params string[] required)
        {
            var action = Ensure<InvestigationActionSO>("Actions", id);
            var so = new SerializedObject(action);
            so.FindProperty("_actionId").stringValue = id;
            so.FindProperty("_actionNameTextId").stringValue = keyPrefix + ".name";
            so.FindProperty("_descriptionTextId").stringValue = keyPrefix + ".desc";
            so.FindProperty("_resultTextId").stringValue = keyPrefix + ".result";
            so.FindProperty("_actionKind").intValue = 6;
            so.FindProperty("_usage").intValue = 2;
            so.FindProperty("_minutesOverride").intValue = minutes;
            SetStrings(so.FindProperty("_requiredClueIds"), required);
            so.FindProperty("_rewardClueId").stringValue = rewardClue ?? string.Empty;
            so.FindProperty("_advanceStep").boolValue = false;
            so.FindProperty("_stepOnSuccess").intValue = 4;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(action);
            return action;
        }

        /// <summary>수사 노트의 현장 단서 칸을 넣거나 고친다.</summary>
        private static void FieldClue(CaseDeductionSO d, string clueId, string placeTextId, string fromPageId)
        {
            var so = new SerializedObject(d);
            var list = so.FindProperty("_fieldClues");
            SerializedProperty item = null;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).FindPropertyRelative("clueId").stringValue == clueId) item = list.GetArrayElementAtIndex(i);
            if (item == null)
            {
                list.arraySize++;
                item = list.GetArrayElementAtIndex(list.arraySize - 1);
            }
            item.FindPropertyRelative("clueId").stringValue = clueId;
            item.FindPropertyRelative("placeTextId").stringValue = placeTextId;
            item.FindPropertyRelative("fromPageId").stringValue = fromPageId ?? string.Empty;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(d);
        }

        /// <summary>보고서 빈칸의 정답으로 받아 주는 말들을 바꾼다.</summary>
        private static void BlankAnswers(CaseDeductionSO d, string blankId, params string[] answers)
        {
            var so = new SerializedObject(d);
            var list = so.FindProperty("_reportBlanks");
            for (int i = 0; i < list.arraySize; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                if (item.FindPropertyRelative("blankId").stringValue != blankId) continue;
                SetStrings(item.FindPropertyRelative("acceptedAnswers"), answers);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(d);
        }

        /// <summary>에셋 목록에 하나를 더한다(이미 있으면 그대로).</summary>
        private static void AddRef(Object owner, string listName, Object item)
        {
            if (owner == null || item == null) { Debug.LogWarning($"[MainCaseData] {listName} 에 넣을 것이 없다"); return; }
            var so = new SerializedObject(owner);
            var list = so.FindProperty(listName);
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == item) return;
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(owner);
        }

        private static void SetStrings(SerializedProperty list, IList<string> values)
        {
            if (list == null) return;
            values ??= new string[0];
            list.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) list.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        /// <summary>새 사건들. 사건 #003(퇴마), #004, #005 는 MainCaseData.NewCases.cs 에서 채운다.</summary>
        static partial void ApplyNewCasesPartial();

        private static void ApplyNewCases() => ApplyNewCasesPartial();
    }
}
