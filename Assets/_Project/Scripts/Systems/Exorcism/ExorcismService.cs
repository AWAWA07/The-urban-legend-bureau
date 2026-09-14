using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.Save;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 괴담 봉인을 담당한다.
    ///
    /// 봉인 여부는 SaveData.legendStates[].isSealed 에 그대로 들어간다.
    /// 별도의 봉인 목록을 만들지 않는다.
    ///
    /// 이번 단계의 봉인 조건은 "이 괴담의 규칙을 하나라도 알아냈는가" 하나다.
    /// RuleSO.conditions 평가나 함정 규칙(isTrue=false) 처리는 아직 하지 않는다.
    /// </summary>
    public class ExorcismService : IService
    {
        private readonly GameDataCatalogSO _catalog;
        private readonly Dictionary<string, LegendSO> _legendsById = new Dictionary<string, LegendSO>();

        public ExorcismService(GameDataCatalogSO catalog)
        {
            _catalog = catalog;
        }

        // ------------------------------------------------------------- 수명 주기

        public void Initialize()
        {
            _legendsById.Clear();

            if (_catalog == null)
            {
                Debug.LogError("[ExorcismService] GameDataCatalog이 지정되지 않았다. GameRoot 인스펙터를 확인할 것.");
                return;
            }

            var legends = _catalog.Legends;
            if (legends == null) return;

            for (int i = 0; i < legends.Count; i++)
            {
                var legend = legends[i];
                if (legend == null || string.IsNullOrEmpty(legend.LegendId)) continue;
                if (_legendsById.ContainsKey(legend.LegendId)) continue;

                _legendsById.Add(legend.LegendId, legend);
            }

            Debug.Log($"[ExorcismService] 준비 완료 | 봉인 대상 괴담 {_legendsById.Count}개");
        }

        public void Shutdown()
        {
            _legendsById.Clear();
        }

        // ------------------------------------------------------------- 조회

        /// <summary>이미 봉인된 괴담인가.</summary>
        public bool IsSealed(SaveData save, string legendId)
        {
            var state = FindState(save, legendId);
            return state != null && state.isSealed;
        }

        /// <summary>지금 봉인할 수 있는가. 봉인 버튼 활성 판단에 쓴다.</summary>
        public bool CanSeal(SaveData save, string legendId)
        {
            return Evaluate(save, legendId) == SealResult.Success;
        }

        /// <summary>
        /// 봉인을 시도한다. 성공하면 isSealed를 true로 바꾼다.
        /// 저장은 호출 측에서 SaveService.MarkDirty / Save 로 처리한다.
        /// </summary>
        public SealResult TrySeal(SaveData save, string legendId)
        {
            var result = Evaluate(save, legendId);
            if (result != SealResult.Success) return result;

            var state = save.GetOrCreateLegendState(legendId);
            state.isSealed = true;
            return SealResult.Success;
        }

        /// <summary>이 괴담에서 플레이어가 알아낸 규칙들. 정답과 함정이 섞여 있을 수 있다.</summary>
        public List<RuleSO> GetDeducedRules(SaveData save, string legendId)
        {
            return CollectDeduced(save, legendId, onlyTrue: false);
        }

        /// <summary>이 괴담에서 알아낸 규칙 중 올바른 것들. 봉인의 근거가 된다.</summary>
        public List<RuleSO> GetDeducedTrueRules(SaveData save, string legendId)
        {
            return CollectDeduced(save, legendId, onlyTrue: true);
        }

        private List<RuleSO> CollectDeduced(SaveData save, string legendId, bool onlyTrue)
        {
            var result = new List<RuleSO>();
            if (save == null || save.deducedRuleIds == null) return result;

            var legend = FindLegend(legendId);
            if (legend == null) return result;

            foreach (var rule in legend.Rules)
            {
                if (rule == null || string.IsNullOrEmpty(rule.RuleId)) continue;
                if (!save.deducedRuleIds.Contains(rule.RuleId)) continue;
                if (onlyTrue && !rule.IsTrue) continue;

                result.Add(rule);
            }
            return result;
        }

        // ------------------------------------------------------------- 내부

        /// <summary>
        /// 봉인 가능 여부를 판정한다. 상태를 바꾸지 않는다.
        /// CanSeal과 TrySeal이 같은 규칙을 쓰도록 한 곳에 모아 뒀다.
        /// </summary>
        private SealResult Evaluate(SaveData save, string legendId)
        {
            if (save == null) return SealResult.NoSaveData;
            if (string.IsNullOrEmpty(legendId)) return SealResult.LegendNotFound;

            var legend = FindLegend(legendId);
            if (legend == null) return SealResult.LegendNotFound;

            if (IsSealed(save, legendId)) return SealResult.AlreadySealed;

            // 올바른 규칙을 하나라도 알아야 봉인할 수 있다.
            // 함정 규칙만 붙들고 있으면 봉인되지 않는다 — 그게 이 게임의 핵심이다.
            if (GetDeducedTrueRules(save, legendId).Count > 0) return SealResult.Success;

            // 규칙을 알긴 아는데 전부 틀렸다면, 아무것도 모르는 것과 구분해 알려준다.
            if (GetDeducedRules(save, legendId).Count > 0) return SealResult.FalseRuleOnly;

            return SealResult.RuleNotFound;
        }

        private LegendSO FindLegend(string legendId)
        {
            if (string.IsNullOrEmpty(legendId)) return null;
            return _legendsById.TryGetValue(legendId, out var legend) ? legend : null;
        }

        /// <summary>기록이 없으면 null. 조회만으로 상태를 새로 만들지 않는다.</summary>
        private static LegendState FindState(SaveData save, string legendId)
        {
            if (save == null || save.legendStates == null || string.IsNullOrEmpty(legendId)) return null;

            for (int i = 0; i < save.legendStates.Count; i++)
            {
                if (save.legendStates[i] != null && save.legendStates[i].legendId == legendId)
                {
                    return save.legendStates[i];
                }
            }
            return null;
        }
    }
}
