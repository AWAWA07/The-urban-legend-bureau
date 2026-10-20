using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UrbanLegendBureau.Data;

namespace UrbanLegendBureau.EditorTools
{
    /// <summary>
    /// 새 본편 사건 셋.
    ///
    ///   #003 7호실의 노크        구도시 동명고시원. 진짜 괴담. 생방송 하나로 믿음도가 치솟아 한영이 직접 퇴마한다(결계 순서 퍼즐).
    ///                            수사 노트와 정정글이 없다. 결계를 다 치면 믿음도가 크게 떨어져 해결된다.
    ///   #004 4시 44분의 호출     새벽마트 구도시점. 가짜. 4시 44분 방송은 택배 수령 안내 예약 방송이다.
    ///   #005 빈 그네의 아이      달빛 어린이공원. 가짜. 자정에 도는 상가 환풍기 바람이 헐거운 그네를 흔든다.
    ///
    /// #004 #005 는 사건 #002 의 에셋(사건, 수사 노트, 정정글, 원글, 안내 글)을 복사해 ID 만 바꾸고 문구는 Strings.txt 에 새로 쓴다.
    /// 해결 조건은 기존 본편 사건과 같다(분석·추론 → 보고서 → 정정글 → 목표 이하).
    /// </summary>
    public static partial class MainCaseData
    {
        static partial void ApplyNewCasesPartial()
        {
            ApplyCase003();
            ApplyClonedCase("004", new Dictionary<string, string>
            {
                { "clue_test_003", "clue_test_013" }, { "clue_test_004", "clue_test_014" },
                { "clue_test_007", "clue_test_015" }, { "clue_test_008", "clue_test_016" },
                { "web_test_003", "web_test_007" }, { "web_test_005", "web_test_008" },
            }, new[] { "action_test_016", "action_test_017", "action_test_018", "action_test_019" },
                new[] { "ui.casefile.place_store_counter", "ui.casefile.place_store_locker", "ui.casefile.place_store_notice", "ui.casefile.place_store_counter" },
                startBelief: 41, appearWorkday: 2, riskLevel: 2, spread: 16,
                overview: new[] { "택배", "~[택배|택배안내|택배수령]", "parcel", "delivery" },
                target: new[] { "불렸어요", "~[불렸어요|불렸다|불림]", "called" });
            ApplyClonedCase("005", new Dictionary<string, string>
            {
                { "clue_test_003", "clue_test_017" }, { "clue_test_004", "clue_test_018" },
                { "clue_test_007", "clue_test_019" }, { "clue_test_008", "clue_test_020" },
                { "web_test_003", "web_test_009" }, { "web_test_005", "web_test_010" },
            }, new[] { "action_test_020", "action_test_021", "action_test_022", "action_test_023" },
                new[] { "ui.casefile.place_park_swing", "ui.casefile.place_park_vent", "ui.casefile.place_park_board", "ui.casefile.place_park_swing" },
                startBelief: 72, appearWorkday: 3, riskLevel: 4, spread: 30,   // 진짜 괴담: 엄마를 기다리던 아이
                overview: new[] { "배웅", "~[배웅|배웅해|보내]", "farewell", "send off" },
                target: new[] { "흔들려요", "~[흔들려요|흔들린다|흔들림]", "swinging" });
        }

        // ------------------------------------------------------------- #003 퇴마

        private static void ApplyCase003()
        {
            // 결계 순서를 알려 주는 단서 셋. 관리인 수첩 두 쪽, 7호실 창틀의 촛농.
            Clue("clue_test_010", "clue.test.010.text", ClueType.Physical, "InvestigationPoint_GosiDesk");
            Clue("clue_test_011", "clue.test.011.text", ClueType.Physical, "InvestigationPoint_GosiDesk");
            Clue("clue_test_012", "clue.test.012.text", ClueType.Physical, "InvestigationPoint_RiteWindow");
            Action("action_test_013", "action.test.013", 10, "clue_test_010");
            Action("action_test_014", "action.test.014", 10, "clue_test_011");
            Action("action_test_015", "action.test.015", 10, "clue_test_012");
            Clue("clue_test_021", "clue.test.021.text", ClueType.Digital, "web_test_006");

            var page = Ensure<WebPageSO>("WebPages", "web_test_006");
            var pso = new SerializedObject(page);
            pso.FindProperty("_pageId").stringValue = "web_test_006";
            pso.FindProperty("_titleTextId").stringValue = "web.test.006.title";
            pso.FindProperty("_bodyTextId").stringValue = "web.test.006.body";
            pso.FindProperty("_pageType").intValue = 0;
            var comments = pso.FindProperty("_comments");
            comments.arraySize = 2;
            SetComment(comments.GetArrayElementAtIndex(0), "ui.net.author_nick_4", "web.test.006.c1", false);
            SetComment(comments.GetArrayElementAtIndex(1), "ui.net.author_nick_9", "web.test.006.c2", false);
            SetStrings(pso.FindProperty("_relatedClueIds"), new[] { "clue_test_021" });   // 생방송 다시보기를 읽으면 방송 기록 단서
            pso.FindProperty("_isCensorable").boolValue = false;
            pso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(page);

            var legend = Ensure<LegendSO>("Legends", "legend_test_003");
            var lso = new SerializedObject(legend);
            lso.FindProperty("_legendId").stringValue = "legend_test_003";
            lso.FindProperty("_nameTextId").stringValue = "legend.test.003.name";
            lso.FindProperty("_descriptionTextId").stringValue = "legend.test.003.description";
            lso.FindProperty("_riskLevel").intValue = 4;   // 발현: 진짜로 나타난다
            lso.FindProperty("_initialSpreadRate").floatValue = 35f;
            lso.FindProperty("_verdictKind").intValue = 0;
            lso.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(legend, "_clues", Load<ClueSO>("Clues/clue_test_010"), Load<ClueSO>("Clues/clue_test_011"), Load<ClueSO>("Clues/clue_test_012"), Load<ClueSO>("Clues/clue_test_021"));
            SetRefs(legend, "_webPages", page);
            SetRefs(legend, "_investigationActions", Load<InvestigationActionSO>("Actions/action_test_013"),
                Load<InvestigationActionSO>("Actions/action_test_014"), Load<InvestigationActionSO>("Actions/action_test_015"));

            var c = Ensure<CaseSO>("Cases", "case_test_003");
            var cso = new SerializedObject(c);
            cso.FindProperty("_caseId").stringValue = "case_test_003";
            cso.FindProperty("_caseNameTextId").stringValue = "case.test.003.name";
            cso.FindProperty("_caseDescriptionTextId").stringValue = "case.test.003.description";
            cso.FindProperty("_legendId").stringValue = "legend_test_003";
            cso.FindProperty("_startingStep").intValue = 1;
            cso.FindProperty("_isPlayable").boolValue = true;
            // 퇴마 뒤 남은 소문은 다른 사건처럼 수사 노트와 정정글로 정리한다(#002 의 틀을 복사해 ID 만 바꾼다).
            var map = new Dictionary<string, string>
            {
                { "clue_test_003", "clue_test_010" }, { "clue_test_004", "clue_test_011" },
                { "clue_test_007", "clue_test_021" }, { "clue_test_008", "clue_test_012" },
                { "web_test_003", "web_test_006" }, { "web_test_005", "web_test_006" },
                { "test_002", "test_003" }, { "test.002", "test.003" }, { "TEST-002", "TEST-003" },
            };
            var deduction = CloneFrom<CaseDeductionSO>("Cases/deduction_test_002", "Cases", "deduction_test_003", map);
            var post = CloneFrom<PostWritingSO>("PostWriting/post_test_002", "PostWriting", "post_test_003", map);
            string[] places = { "ui.casefile.place_gosi_desk", "ui.casefile.place_gosi_desk", "ui.casefile.place_test_net", "ui.casefile.place_gosi_window" };
            string[] clueIds = { "clue_test_010", "clue_test_011", "clue_test_021", "clue_test_012" };
            for (int i = 0; i < 4; i++) FieldClue(deduction, clueIds[i], places[i], i == 2 ? "web_test_006" : "");
            var dso = new SerializedObject(deduction);
            var q2 = dso.FindProperty("_questions").GetArrayElementAtIndex(1);
            SetStrings(q2.FindPropertyRelative("evidenceClueIds"), new[] { "clue_test_021" });
            SetStrings(q2.FindPropertyRelative("evidenceAnalysisIds"), new[] { "analysis_test_003_b" });
            dso.ApplyModifiedPropertiesWithoutUndo();
            BlankAnswers(deduction, "verdict", "결계", "~[결계|결계로]", "ward");
            BlankAnswers(deduction, "target", "들림", "~[들림|들려|들린다]", "hear");
            cso.FindProperty("_postWriting").objectReferenceValue = post;
            cso.FindProperty("_deduction").objectReferenceValue = deduction;
            cso.FindProperty("_mainStory").boolValue = true;
            cso.FindProperty("_startBelief").intValue = 82;   // 생방송 하나로 하룻밤에 치솟았다
            cso.FindProperty("_appearWorkday").intValue = 2;
            cso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(c);
        }

        private static void SetComment(SerializedProperty item, string author, string body, bool isAuthor)
        {
            item.FindPropertyRelative("AuthorTextId").stringValue = author;
            item.FindPropertyRelative("BodyTextId").stringValue = body;
            item.FindPropertyRelative("IsAuthor").boolValue = isAuthor;
        }

        // ------------------------------------------------------------- #004 #005 복사

        /// <summary>
        /// 사건 #002 의 에셋을 복사해 사건 n 을 만든다. 문자열 속 ID(test_002, test.002, 단서·글 ID)를 바꾸고 서로의 참조를 새 에셋으로 잇는다.
        /// 단서 넷과 조사 행동 넷을 새로 만들고, 현장 단서 칸의 장소 글을 바꾼다. 두 번째 추론은 두 번째 분석을 근거로 삼는다.
        /// </summary>
        private static void ApplyClonedCase(string n, Dictionary<string, string> idMap, string[] actionIds, string[] places,
            int startBelief, int appearWorkday, int riskLevel, float spread, string[] overview, string[] target)
        {
            var map = new Dictionary<string, string>(idMap)
            {
                { "test_002", "test_" + n }, { "test.002", "test." + n }, { "TEST-002", "TEST-" + n },
            };

            var deduction = CloneFrom<CaseDeductionSO>("Cases/deduction_test_002", "Cases", "deduction_test_" + n, map);
            var post = CloneFrom<PostWritingSO>("PostWriting/post_test_002", "PostWriting", "post_test_" + n, map);
            var rumor = CloneFrom<WebPageSO>("WebPages/web_test_003", "WebPages", idMap["web_test_003"], map);
            var notice = CloneFrom<WebPageSO>("WebPages/web_test_005", "WebPages", idMap["web_test_005"], map);
            // 글의 문구 ID 는 글 ID 를 따라간다(web.test.007.title 처럼).
            RetargetPageText(rumor, idMap["web_test_003"]);
            RetargetPageText(notice, idMap["web_test_005"]);

            // 단서 넷. 첫째·둘째는 분석 a, 셋째(안내 글 = 현장 안내문)·넷째는 분석 b.
            string[] clues = { idMap["clue_test_003"], idMap["clue_test_004"], idMap["clue_test_007"], idMap["clue_test_008"] };
            var clueAssets = new List<Object>();
            for (int i = 0; i < 4; i++)
            {
                string key = "clue.test." + clues[i].Substring("clue_test_".Length) + ".text";
                clueAssets.Add(Clue(clues[i], key, i == 2 ? ClueType.Digital : ClueType.Physical, places[i]));
                Action(actionIds[i], "action.test." + actionIds[i].Substring("action_test_".Length), i == 3 ? 20 : 10, clues[i]);
            }
            // 안내 글도 같은 단서를 준다(괴담넷에서 읽어도 얻는다).
            var nso = new SerializedObject(notice);
            SetStrings(nso.FindProperty("_relatedClueIds"), new[] { clues[2] });
            nso.ApplyModifiedPropertiesWithoutUndo();

            for (int i = 0; i < 4; i++) FieldClue(deduction, clues[i], places[i], "");
            var dso = new SerializedObject(deduction);
            var q2 = dso.FindProperty("_questions").GetArrayElementAtIndex(1);
            SetStrings(q2.FindPropertyRelative("evidenceClueIds"), new[] { clues[3] });
            SetStrings(q2.FindPropertyRelative("evidenceAnalysisIds"), new[] { "analysis_test_" + n + "_b" });
            dso.ApplyModifiedPropertiesWithoutUndo();
            BlankAnswers(deduction, "verdict", overview);
            BlankAnswers(deduction, "target", target);

            var legend = CloneFrom<LegendSO>("Legends/legend_test_002", "Legends", "legend_test_" + n, map);
            var lso = new SerializedObject(legend);
            lso.FindProperty("_riskLevel").intValue = riskLevel;
            lso.FindProperty("_initialSpreadRate").floatValue = spread;
            lso.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(legend, "_rules");
            SetRefs(legend, "_clues", clueAssets.ToArray());
            SetRefs(legend, "_webPages", rumor, notice);
            var actions = new List<Object>();
            foreach (var id in actionIds) actions.Add(Load<InvestigationActionSO>("Actions/" + id));
            SetRefs(legend, "_investigationActions", actions.ToArray());

            var c = CloneFrom<CaseSO>("Cases/case_test_002", "Cases", "case_test_" + n, map);
            var cso = new SerializedObject(c);
            cso.FindProperty("_postWriting").objectReferenceValue = post;
            cso.FindProperty("_deduction").objectReferenceValue = deduction;
            cso.FindProperty("_startBelief").intValue = startBelief;
            cso.FindProperty("_appearWorkday").intValue = appearWorkday;
            cso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(c);
        }

        /// <summary>원본을 복사해 새 에셋을 만든다(없을 때만). 그리고 안의 모든 글자에서 ID 를 바꾼다. 여러 번 해도 같다.</summary>
        private static T CloneFrom<T>(string srcPath, string folder, string id, Dictionary<string, string> map) where T : ScriptableObject
        {
            string dst = DataRoot + folder + "/" + id + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(dst);
            if (asset == null)
            {
                AssetDatabase.CopyAsset(DataRoot + srcPath + ".asset", dst);
                asset = AssetDatabase.LoadAssetAtPath<T>(dst);
            }
            var so = new SerializedObject(asset);
            var it = so.GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.String) continue;
                string s = it.stringValue;
                if (string.IsNullOrEmpty(s)) continue;
                string r = s;
                foreach (var kv in map) r = r.Replace(kv.Key, kv.Value);
                if (r != s) it.stringValue = r;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            asset.name = id;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void RetargetPageText(WebPageSO page, string pageId)
        {
            string key = "web.test." + pageId.Substring("web_test_".Length);
            var so = new SerializedObject(page);
            so.FindProperty("_pageId").stringValue = pageId;
            so.FindProperty("_titleTextId").stringValue = key + ".title";
            so.FindProperty("_bodyTextId").stringValue = key + ".body";
            var comments = so.FindProperty("_comments");
            for (int i = 0; i < comments.arraySize; i++)
                comments.GetArrayElementAtIndex(i).FindPropertyRelative("BodyTextId").stringValue = key + ".c" + (i + 1);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(page);
        }

        /// <summary>에셋 목록을 통째로 바꾼다.</summary>
        private static void SetRefs(Object owner, string listName, params Object[] items)
        {
            var so = new SerializedObject(owner);
            var list = so.FindProperty(listName);
            list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(owner);
        }
    }
}
