using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.EditorTools
{
    /// <summary>
    /// 게임 데이터 에셋의 기본 오류를 찾는다.
    /// 검증 프레임워크가 아니라, 자주 나는 실수만 잡는 단순한 도구다.
    ///
    /// 검사 항목:
    ///   ID 비어 있음 / ID 중복 / Text ID 비어 있음 / 리스트 안 null /
    ///   존재하지 않는 ID 참조 / Localization 테이블에 없는 String ID
    /// </summary>
    public static class GameDataValidator
    {
        [MenuItem("UrbanLegendBureau/Data/Validate Game Data")]
        public static void ValidateMenu()
        {
            var report = Validate();
            if (report.ErrorCount == 0 && report.WarningCount == 0)
            {
                Debug.Log("[GameDataValidator] 문제 없음\n" + report.Summary);
            }
            else if (report.ErrorCount == 0)
            {
                Debug.LogWarning("[GameDataValidator] 경고 " + report.WarningCount + "건\n" + report.Text);
            }
            else
            {
                Debug.LogError("[GameDataValidator] 오류 " + report.ErrorCount + "건 / 경고 " + report.WarningCount + "건\n" + report.Text);
            }
        }

        public class Report
        {
            public int ErrorCount;
            public int WarningCount;
            public string Summary = string.Empty;
            public string Text = string.Empty;
        }

        public static Report Validate()
        {
            var legends = LoadAll<LegendSO>();
            var rules = LoadAll<RuleSO>();
            var clues = LoadAll<ClueSO>();
            var pages = LoadAll<WebPageSO>();

            var report = new Report();
            var sb = new StringBuilder();

            var clueIds = CollectIds(clues);

            CheckIds(legends, "LegendSO", report, sb);
            CheckIds(rules, "RuleSO", report, sb);
            CheckIds(clues, "ClueSO", report, sb);
            CheckIds(pages, "WebPageSO", report, sb);

            // --- 텍스트 ID ---
            var localization = LoadLocalization();

            foreach (var l in legends)
            {
                CheckTextId(l, l.NameTextId, "nameTextId", localization, report, sb);
                CheckTextId(l, l.DescriptionTextId, "descriptionTextId", localization, report, sb);
                CheckTextId(l, l.RiskLevelTextId, "riskLevel 표시 이름", localization, report, sb);

                CheckNoNulls(l, l.Rules, "rules", report, sb);
                CheckNoNulls(l, l.Clues, "clues", report, sb);
                CheckNoNulls(l, l.WebPages, "webPages", report, sb);
            }

            foreach (var r in rules)
            {
                CheckTextId(r, r.RuleTextId, "ruleTextId", localization, report, sb);
                CheckReferencedClueIds(r, r.RequiredClueIds, "requiredClueIds", clueIds, report, sb);

                for (int i = 0; i < r.Conditions.Count; i++)
                {
                    var c = r.Conditions[i];
                    if (c == null)
                    {
                        Error(report, sb, r, "conditions[" + i + "] 가 null 이다.");
                        continue;
                    }
                    if (c.Type == RuleConditionType.HasClue && !string.IsNullOrEmpty(c.TargetId) && !clueIds.Contains(c.TargetId))
                    {
                        Error(report, sb, r, "conditions[" + i + "] 의 HasClue 대상 '" + c.TargetId + "' 에 해당하는 ClueSO 가 없다.");
                    }
                }
            }

            foreach (var c in clues)
            {
                CheckTextId(c, c.ClueTextId, "clueTextId", localization, report, sb);
                CheckReferencedClueIds(c, c.RequiredClueIds, "requiredClueIds", clueIds, report, sb);
            }

            foreach (var p in pages)
            {
                CheckTextId(p, p.TitleTextId, "titleTextId", localization, report, sb);
                CheckTextId(p, p.BodyTextId, "bodyTextId", localization, report, sb);
                CheckReferencedClueIds(p, p.RelatedClueIds, "relatedClueIds", clueIds, report, sb);
            }

            localization.Shutdown();

            report.Summary =
                "Legend " + legends.Count + " / Rule " + rules.Count +
                " / Clue " + clues.Count + " / WebPage " + pages.Count;
            report.Text = report.Summary + "\n" + sb;
            return report;
        }

        // ------------------------------------------------------------- 검사

        private static void CheckIds<T>(List<T> assets, string typeName, Report report, StringBuilder sb)
            where T : ScriptableObject, IGameDataAsset
        {
            var seen = new Dictionary<string, T>();
            foreach (var a in assets)
            {
                if (string.IsNullOrEmpty(a.Id))
                {
                    Error(report, sb, a, typeName + " 의 ID 가 비어 있다.");
                    continue;
                }

                if (seen.TryGetValue(a.Id, out var other))
                {
                    Error(report, sb, a, "ID '" + a.Id + "' 가 " + other.name + " 와 중복된다.");
                }
                else
                {
                    seen[a.Id] = a;
                }
            }
        }

        private static void CheckTextId(ScriptableObject owner, string textId, string fieldName,
            LocalizationService localization, Report report, StringBuilder sb)
        {
            if (string.IsNullOrEmpty(textId))
            {
                Error(report, sb, owner, fieldName + " 가 비어 있다.");
                return;
            }

            if (localization != null && !localization.Has(textId))
            {
                Warning(report, sb, owner, fieldName + " '" + textId + "' 가 Localization 테이블에 없다.");
            }
        }

        private static void CheckNoNulls<T>(ScriptableObject owner, IReadOnlyList<T> list, string fieldName,
            Report report, StringBuilder sb) where T : Object
        {
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                {
                    Error(report, sb, owner, fieldName + "[" + i + "] 가 비어 있다.");
                }
            }
        }

        private static void CheckReferencedClueIds(ScriptableObject owner, IReadOnlyList<string> ids, string fieldName,
            HashSet<string> clueIds, Report report, StringBuilder sb)
        {
            if (ids == null) return;
            for (int i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                if (string.IsNullOrEmpty(id))
                {
                    Error(report, sb, owner, fieldName + "[" + i + "] 가 비어 있다.");
                    continue;
                }
                if (!clueIds.Contains(id))
                {
                    Error(report, sb, owner, fieldName + "[" + i + "] 의 '" + id + "' 에 해당하는 ClueSO 가 없다.");
                }
            }
        }

        // ------------------------------------------------------------- 헬퍼

        private static List<T> LoadAll<T>() where T : ScriptableObject
        {
            var result = new List<T>();
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) result.Add(asset);
            }
            return result;
        }

        private static HashSet<string> CollectIds<T>(List<T> assets) where T : ScriptableObject, IGameDataAsset
        {
            var set = new HashSet<string>();
            foreach (var a in assets)
            {
                if (!string.IsNullOrEmpty(a.Id)) set.Add(a.Id);
            }
            return set;
        }

        /// <summary>
        /// String ID 존재 여부 확인에 쓸 LocalizationService 인스턴스를 만든다.
        /// 런타임과 같은 클래스를 그대로 쓰므로 파싱 규칙이 어긋날 일이 없다.
        /// (LocalizationService 는 수정하지 않았다. 기존 Has() 만 사용한다.)
        /// </summary>
        private static LocalizationService LoadLocalization()
        {
            var service = new LocalizationService();
            service.Initialize();
            return service;
        }

        private static void Error(Report report, StringBuilder sb, Object owner, string message)
        {
            report.ErrorCount++;
            sb.AppendLine("  [오류] " + owner.name + ": " + message);
        }

        private static void Warning(Report report, StringBuilder sb, Object owner, string message)
        {
            report.WarningCount++;
            sb.AppendLine("  [경고] " + owner.name + ": " + message);
        }
    }
}
