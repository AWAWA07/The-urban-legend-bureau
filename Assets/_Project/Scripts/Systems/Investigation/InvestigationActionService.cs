using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.Save;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 플레이어가 고른 조사 행동을 판정하고 실행한다.
    ///
    /// 책임 분담:
    ///  - 조건을 만족하는가         : 이 서비스
    ///  - 사건 시간과 확산          : InvestigationTimeService (15단계 그대로)
    ///  - 단서 보관                 : CaseFlow / SaveData (기존 그대로)
    ///  - 무엇을 보여줄 것인가      : CaseDirector / 화면
    ///
    /// 조건을 만족하지 못하면 아무것도 바꾸지 않는다. 시간도 확산도 움직이지 않는다.
    /// </summary>
    public class InvestigationActionService : IService
    {
        // 결과 문구 String ID. 행동 데이터가 자기 문구를 가지고 있으면 그쪽이 우선한다.
        public const string DoneTextId = "ui.action.result_done";
        public const string NewClueTextId = "ui.action.result_clue";
        public const string RepeatTextId = "ui.action.result_repeat";
        public const string FailClueTextId = "ui.action.fail_clue";
        public const string FailStepTextId = "ui.action.fail_step";
        public const string FailMissingTextId = "ui.action.fail_missing";

        private readonly GameDataCatalogSO _catalog;
        private readonly InvestigationTimeService _time;
        private readonly Dictionary<string, InvestigationActionSO> _byId =
            new Dictionary<string, InvestigationActionSO>();

        public InvestigationActionService(GameDataCatalogSO catalog, InvestigationTimeService time)
        {
            _catalog = catalog;
            _time = time;
        }

        public int Count => _byId.Count;

        // ------------------------------------------------------------- 수명 주기

        public void Initialize()
        {
            _byId.Clear();

            if (_catalog == null)
            {
                Debug.LogError("[InvestigationActionService] GameDataCatalog이 지정되지 않았다. GameRoot 인스펙터를 확인할 것.");
                return;
            }

            var actions = _catalog.InvestigationActions;
            if (actions == null) return;

            int skipped = 0;
            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];

                if (action == null)
                {
                    Debug.LogError($"[InvestigationActionService] 카탈로그 {i}번 행동 항목이 비어 있다.");
                    skipped++;
                    continue;
                }

                if (string.IsNullOrEmpty(action.ActionId))
                {
                    Debug.LogError($"[InvestigationActionService] '{action.name}' 의 actionId가 비어 있다. 등록하지 않는다.");
                    skipped++;
                    continue;
                }

                if (_byId.TryGetValue(action.ActionId, out var existing))
                {
                    Debug.LogError(
                        $"[InvestigationActionService] actionId '{action.ActionId}' 가 중복된다. " +
                        $"'{existing.name}' 를 유지하고 '{action.name}' 를 건너뛴다.");
                    skipped++;
                    continue;
                }

                _byId.Add(action.ActionId, action);
            }

            Debug.Log($"[InvestigationActionService] 조사 행동 {_byId.Count}개 등록" +
                      (skipped > 0 ? $" (건너뜀 {skipped}개)" : ""));
        }

        public void Shutdown()
        {
            _byId.Clear();
        }

        // ------------------------------------------------------------- 조회

        public InvestigationActionSO GetAction(string actionId)
        {
            if (string.IsNullOrEmpty(actionId)) return null;
            return _byId.TryGetValue(actionId, out var action) ? action : null;
        }

        /// <summary>이 괴담에서 고를 수 있는 조사 행동. 사건이 다른데 섞이지 않게 괴담 데이터가 정한다.</summary>
        public List<InvestigationActionSO> GetActionsForLegend(LegendSO legend)
        {
            var result = new List<InvestigationActionSO>();
            if (legend == null) return result;

            foreach (var action in legend.InvestigationActions)
            {
                if (action != null) result.Add(action);
            }
            return result;
        }

        // ------------------------------------------------------------- 조건

        /// <summary>지금 이 행동을 할 수 있는가. 못 한다면 그 이유를 돌려준다.</summary>
        public InvestigationFailure CheckRequirements(SaveService save, InvestigationActionSO action)
        {
            if (action == null) return InvestigationFailure.NotFound;
            return CheckRequirements(save, action.RequiredClueIds, action.RequireStep, action.RequiredStep);
        }

        /// <summary>
        /// 조사 지점의 해금 조건을 본다.
        /// 행동과 조건 형태가 같으므로 같은 판정을 쓴다. 조건 규칙이 두 벌로 갈라지지 않게 한다.
        /// </summary>
        public InvestigationFailure CheckPointRequirements(SaveService save, InvestigationPoint point)
        {
            if (point == null) return InvestigationFailure.NotFound;
            return CheckRequirements(save, point.RequiredClueIds, point.RequireStep, point.RequiredStep);
        }

        /// <summary>단서 보유 / 사건 단계 조건을 본다. 행동과 지점이 공유한다.</summary>
        private InvestigationFailure CheckRequirements(SaveService save, IReadOnlyList<string> requiredClueIds,
            bool requireStep, CaseStep requiredStep)
        {
            if (save == null || save.Current == null) return InvestigationFailure.NoSaveData;

            if (requiredClueIds != null)
            {
                for (int i = 0; i < requiredClueIds.Count; i++)
                {
                    var clueId = requiredClueIds[i];
                    if (string.IsNullOrEmpty(clueId)) continue;
                    if (!save.Current.acquiredClueIds.Contains(clueId)) return InvestigationFailure.MissingClue;
                }
            }

            if (requireStep && CaseFlow.GetStep(save) < requiredStep)
            {
                return InvestigationFailure.StepNotReached;
            }

            return InvestigationFailure.None;
        }

        public bool CanPerform(SaveService save, InvestigationActionSO action)
        {
            return CheckRequirements(save, action) == InvestigationFailure.None;
        }

        // ------------------------------------------------------------- 실행

        /// <summary>
        /// 조사 행동을 실행한다.
        ///
        /// 순서: 조건 확인 -> 단서 처리 -> 사건 시간/확산 등록 -> 단계 진행.
        /// 조건에서 막히면 그 뒤는 하나도 실행되지 않는다.
        /// </summary>
        public InvestigationActionResult Perform(SaveService save, string caseId, string legendId,
            InvestigationActionSO action)
        {
            int count = _time != null && save != null && save.Current != null
                ? _time.GetActionCount(save.Current, caseId) : 0;
            int minutes = _time != null && save != null && save.Current != null
                ? _time.GetElapsedMinutes(save.Current, caseId) : 0;

            var failure = CheckRequirements(save, action);
            if (failure != InvestigationFailure.None)
            {
                string reasonId = FailureTextId(failure);
                string blockedId = action != null ? action.ActionId : string.Empty;

                Debug.Log($"[InvestigationActionService] 조건 미충족 | {blockedId} -> {failure} " +
                          "(시간/확산 변화 없음)");

                return InvestigationActionResult.Blocked(blockedId, failure, count, minutes, reasonId);
            }

            // --- 단서 ---
            string acquired = null;
            if (action.HasRewardClue && CaseFlow.AcquireClue(save, action.RewardClueId))
            {
                acquired = action.RewardClueId;
            }

            // --- 사건 시간과 확산 (15단계 서비스에 그대로 맡긴다) ---
            var tick = default(InvestigationTickResult);
            if (_time != null)
            {
                int cost = action.MinutesOverride > 0
                    ? action.MinutesOverride
                    : InvestigationTimeService.MinutesPerAction;

                float spread = action.UseSpreadOverride
                    ? action.SpreadOverride
                    : InvestigationTimeService.GetSpreadCost(action.ActionKind);

                tick = _time.RegisterAction(save, caseId, legendId, action.ActionKind, cost, spread);
                count = tick.ActionCount;
                minutes = tick.ElapsedMinutes;
            }

            // --- 사건 단계 ---
            if (action.AdvanceStep) CaseFlow.SetStep(save, action.StepOnSuccess);

            string messageId = ResolveMessageTextId(action, acquired);

            Debug.Log($"[InvestigationActionService] 실행 | {action.ActionId} ({action.ActionKind}) | " +
                      $"{count}회 / {minutes}분" +
                      (acquired != null ? $" | 단서 획득 {acquired}" : string.Empty) +
                      (tick.Spread.Changed
                          ? $" | 확산 {tick.Spread.PreviousRate:F1} -> {tick.Spread.CurrentRate:F1}"
                          : string.Empty));

            return new InvestigationActionResult(true, InvestigationFailure.None, action.ActionId,
                action.ActionKind, action.MinutesOverride > 0 ? action.MinutesOverride : InvestigationTimeService.MinutesPerAction,
                count, minutes, tick.Spread, acquired, messageId);
        }

        // ------------------------------------------------------------- 내부

        /// <summary>
        /// 결과 문구를 고른다.
        /// 행동이 자기 문구를 가지고 있으면 그것을 쓰고, 없으면 공용 문구로 넘어간다.
        /// </summary>
        private static string ResolveMessageTextId(InvestigationActionSO action, string acquiredClueId)
        {
            if (!string.IsNullOrEmpty(acquiredClueId))
            {
                return !string.IsNullOrEmpty(action.ResultTextId) ? action.ResultTextId : NewClueTextId;
            }

            // 단서를 주는 행동인데 아무것도 못 얻었다면 이미 확인한 내용이다.
            if (action.HasRewardClue) return RepeatTextId;

            return !string.IsNullOrEmpty(action.ResultTextId) ? action.ResultTextId : DoneTextId;
        }

        public static string FailureTextId(InvestigationFailure failure)
        {
            switch (failure)
            {
                case InvestigationFailure.MissingClue: return FailClueTextId;
                case InvestigationFailure.StepNotReached: return FailStepTextId;
                default: return FailMissingTextId;
            }
        }
    }
}
