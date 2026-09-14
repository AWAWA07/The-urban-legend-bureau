using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.Save;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 규칙 정적 데이터 조회 + 단서 보유 여부로 규칙을 추론(해금)한다.
    ///
    /// 이번 단계의 추론 조건은 하나뿐이다:
    ///   RuleSO.requiredClueIds 를 플레이어가 모두 가지고 있으면 추론 가능.
    ///
    /// RuleSO.conditions(PerformAction/AtLocation/...)는 데이터로만 존재하며
    /// 여기서 평가하지 않는다. 그 판정은 이후 단계에서 붙인다.
    /// </summary>
    public class RuleService : IService
    {
        private readonly GameDataCatalogSO _catalog;
        private readonly Dictionary<string, RuleSO> _byId = new Dictionary<string, RuleSO>();
        private readonly List<RuleSO> _all = new List<RuleSO>();

        public RuleService(GameDataCatalogSO catalog)
        {
            _catalog = catalog;
        }

        public int Count => _all.Count;

        public IReadOnlyList<RuleSO> GetAllRules() => _all;

        // ------------------------------------------------------------- 수명 주기

        public void Initialize()
        {
            _byId.Clear();
            _all.Clear();

            if (_catalog == null)
            {
                Debug.LogError("[RuleService] GameDataCatalog이 지정되지 않았다. GameRoot 인스펙터를 확인할 것.");
                return;
            }

            var rules = _catalog.Rules;
            if (rules == null)
            {
                Debug.LogError("[RuleService] 카탈로그의 규칙 목록이 비어 있다.");
                return;
            }

            int skipped = 0;
            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];

                if (rule == null)
                {
                    Debug.LogError($"[RuleService] 카탈로그 {i}번 규칙 항목이 비어 있다.");
                    skipped++;
                    continue;
                }

                if (string.IsNullOrEmpty(rule.RuleId))
                {
                    Debug.LogError($"[RuleService] '{rule.name}' 의 ruleId가 비어 있다. 등록하지 않는다.");
                    skipped++;
                    continue;
                }

                if (_byId.TryGetValue(rule.RuleId, out var existing))
                {
                    Debug.LogError(
                        $"[RuleService] ruleId '{rule.RuleId}' 가 중복된다. " +
                        $"'{existing.name}' 를 유지하고 '{rule.name}' 를 건너뛴다.");
                    skipped++;
                    continue;
                }

                _byId.Add(rule.RuleId, rule);
                _all.Add(rule);
            }

            Debug.Log($"[RuleService] 규칙 {_all.Count}개 등록" + (skipped > 0 ? $" (건너뜀 {skipped}개)" : ""));
        }

        public void Shutdown()
        {
            _byId.Clear();
            _all.Clear();
        }

        // ------------------------------------------------------------- 조회

        /// <summary>ID로 규칙을 찾는다. 없으면 null을 돌려주고 경고를 남긴다.</summary>
        public RuleSO GetRule(string ruleId)
        {
            if (string.IsNullOrEmpty(ruleId))
            {
                Debug.LogWarning("[RuleService] 빈 ruleId로 조회했다.");
                return null;
            }

            if (_byId.TryGetValue(ruleId, out var rule)) return rule;

            Debug.LogWarning($"[RuleService] '{ruleId}' 에 해당하는 규칙이 없다. 카탈로그를 확인할 것.");
            return null;
        }

        public bool HasRule(string ruleId)
        {
            return !string.IsNullOrEmpty(ruleId) && _byId.ContainsKey(ruleId);
        }

        // ------------------------------------------------------------- 추론

        /// <summary>
        /// 필요한 단서를 모두 가지고 있어 추론할 수 있는 상태인가.
        /// 이미 추론한 규칙은 false를 돌려준다. (더 이상 새로 발견할 것이 없다)
        /// </summary>
        public bool IsRuleDeduceable(SaveService save, string ruleId)
        {
            var rule = GetRuleQuiet(ruleId);
            if (rule == null || save == null || save.Current == null) return false;
            if (save.Current.deducedRuleIds.Contains(ruleId)) return false;

            return HasAllRequiredClues(save, rule);
        }

        /// <summary>
        /// 규칙을 추론해 SaveData에 기록한다.
        /// 조건이 안 되거나 이미 추론했으면 false. 새로 해금했을 때만 true.
        /// </summary>
        public bool TryDeduceRule(SaveService save, string ruleId)
        {
            var rule = GetRuleQuiet(ruleId);
            if (rule == null || save == null || save.Current == null) return false;

            var deduced = save.Current.deducedRuleIds;
            if (deduced.Contains(ruleId)) return false;      // 중복 방지
            if (!HasAllRequiredClues(save, rule)) return false;

            deduced.Add(ruleId);
            save.MarkDirty();
            return true;
        }

        /// <summary>
        /// 지금 추론할 수 있는 규칙들을 모은다.
        /// legend를 주면 그 괴담의 규칙만, 주지 않으면 전체를 대상으로 한다.
        /// </summary>
        public List<RuleSO> GetDeduceableRules(SaveService save, LegendSO legend = null)
        {
            var result = new List<RuleSO>();
            if (save == null || save.Current == null) return result;

            var source = legend != null ? (IReadOnlyList<RuleSO>)legend.Rules : _all;
            for (int i = 0; i < source.Count; i++)
            {
                var rule = source[i];
                if (rule == null || string.IsNullOrEmpty(rule.RuleId)) continue;
                if (save.Current.deducedRuleIds.Contains(rule.RuleId)) continue;
                if (!HasAllRequiredClues(save, rule)) continue;

                result.Add(rule);
            }
            return result;
        }

        public bool IsRuleDeduced(SaveService save, string ruleId)
        {
            if (save == null || save.Current == null || string.IsNullOrEmpty(ruleId)) return false;
            return save.Current.deducedRuleIds.Contains(ruleId);
        }

        // ------------------------------------------------------------- 내부

        /// <summary>로그 없이 조회한다. 내부 판정용.</summary>
        private RuleSO GetRuleQuiet(string ruleId)
        {
            if (string.IsNullOrEmpty(ruleId)) return null;
            return _byId.TryGetValue(ruleId, out var rule) ? rule : null;
        }

        /// <summary>필요한 단서를 모두 보유했는가. 필요 단서가 없는 규칙은 조건이 없는 것으로 본다.</summary>
        private static bool HasAllRequiredClues(SaveService save, RuleSO rule)
        {
            var required = rule.RequiredClueIds;
            if (required == null || required.Count == 0) return true;

            var owned = save.Current.acquiredClueIds;
            for (int i = 0; i < required.Count; i++)
            {
                var clueId = required[i];
                if (string.IsNullOrEmpty(clueId)) continue;   // 빈 항목은 조건으로 치지 않는다
                if (!owned.Contains(clueId)) return false;
            }
            return true;
        }
    }
}
