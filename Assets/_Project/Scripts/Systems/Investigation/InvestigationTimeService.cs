using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Save;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 사건 경과 시간을 다룬다.
    ///
    /// 실제 시간(Time.deltaTime)으로 흐르지 않는다. 플레이어가 조사 행동을 끝낼 때마다
    /// 사건 시간이 한 칸씩 진행된다. 그래서 ITickable이 아니다.
    ///
    /// 값은 SaveData.caseTimes[] 에 사건별로 들어간다.
    /// 서비스가 따로 수치를 들고 있지 않은 것은 SpreadService와 같은 이유다.
    /// 저장본과 어긋날 여지를 만들지 않는다.
    ///
    /// 확산 처리는 직접 계산하지 않고 SpreadService에 위임한다.
    /// 검열/봉인처럼 이미 확산 규칙을 가진 행동은 여기서 0을 더한다.
    /// 같은 행동에 확산이 두 번 붙지 않게 하기 위해서다.
    /// </summary>
    public class InvestigationTimeService : IService
    {
        /// <summary>조사 행동 1회가 진행시키는 사건 시간(분). 밸런스 조절점.</summary>
        public const int MinutesPerAction = 10;

        // --- 행동별 확산 증가량. 작게 시작한다. 여기서만 정한다. ---
        public const float InternetViewSpread = 0.5f;
        public const float FieldSearchSpread = 1.5f;
        public const float ClueFoundSpread = 1.0f;

        /// <summary>검열은 기존 검열 규칙이 확산을 정한다. 시간만 흐른다.</summary>
        public const float CensorSpread = 0f;

        /// <summary>봉인도 기존 봉인 규칙이 확산을 정한다. 시간만 흐른다.</summary>
        public const float SealSpread = 0f;

        private readonly SpreadService _spread;

        public InvestigationTimeService(SpreadService spread)
        {
            _spread = spread;
        }

        // ------------------------------------------------------------- 수명 주기

        public void Initialize()
        {
            Debug.Log($"[InvestigationTimeService] 준비 완료 | 행동 1회 = {MinutesPerAction}분 | " +
                      $"확산 인터넷 {InternetViewSpread} / 현장 {FieldSearchSpread} / 단서 {ClueFoundSpread}");
        }

        public void Shutdown()
        {
        }

        // ------------------------------------------------------------- 조회

        /// <summary>이 사건의 조사 행동 횟수. 기록이 없으면 0.</summary>
        public int GetActionCount(SaveData save, string caseId)
        {
            var state = FindState(save, caseId);
            return state != null ? state.actionCount : 0;
        }

        /// <summary>이 사건의 경과 시간(분). 기록이 없으면 0.</summary>
        public int GetElapsedMinutes(SaveData save, string caseId)
        {
            var state = FindState(save, caseId);
            return state != null ? state.elapsedMinutes : 0;
        }

        /// <summary>행동 종류별 확산 증가량.</summary>
        public static float GetSpreadCost(InvestigationAction action)
        {
            switch (action)
            {
                case InvestigationAction.InternetView: return InternetViewSpread;
                case InvestigationAction.FieldSearch: return FieldSearchSpread;
                case InvestigationAction.ClueFound: return ClueFoundSpread;
                case InvestigationAction.PageCensor: return CensorSpread;
                case InvestigationAction.Seal: return SealSpread;
                default: return 0f;
            }
        }

        // ------------------------------------------------------------- 진행

        /// <summary>
        /// 조사 행동 한 번을 기록한다. 사건 시간이 진행되고, 행동에 따라 확산이 늘어난다.
        ///
        /// legendId가 비어 있으면 시간만 진행한다. 어느 괴담이 퍼지는지는 사건이 알고 있고,
        /// 이 서비스는 모른다.
        /// </summary>
        public InvestigationTickResult RegisterAction(SaveService save, string caseId, string legendId,
            InvestigationAction action)
        {
            if (save == null || save.Current == null || string.IsNullOrEmpty(caseId))
            {
                return InvestigationTickResult.Failed(0, 0);
            }

            var state = save.Current.GetOrCreateCaseTime(caseId);
            if (state == null) return InvestigationTickResult.Failed(0, 0);

            state.actionCount++;
            state.elapsedMinutes += MinutesPerAction;

            var spreadChange = default(SpreadChangeResult);
            float cost = GetSpreadCost(action);

            if (cost > 0f && _spread != null && !string.IsNullOrEmpty(legendId))
            {
                spreadChange = _spread.ApplySpreadDelta(save.Current, legendId, cost);
            }

            save.MarkDirty();

            Debug.Log($"[InvestigationTimeService] {caseId} | {action} | " +
                      $"{state.actionCount}회 / {state.elapsedMinutes}분" +
                      (spreadChange.Changed
                          ? $" | 확산 {spreadChange.PreviousRate:F1} -> {spreadChange.CurrentRate:F1} ({spreadChange.CurrentLevel})"
                          : string.Empty));

            return new InvestigationTickResult(true, state.actionCount, state.elapsedMinutes, spreadChange);
        }

        /// <summary>
        /// 사건의 시간을 처음부터 다시 센다. 사건을 새로 시작할 때만 부른다.
        /// 다른 사건의 기록은 건드리지 않는다.
        /// </summary>
        public void ResetCase(SaveService save, string caseId)
        {
            if (save == null || save.Current == null || string.IsNullOrEmpty(caseId)) return;

            var state = save.Current.GetOrCreateCaseTime(caseId);
            if (state == null) return;

            state.actionCount = 0;
            state.elapsedMinutes = 0;
            save.MarkDirty();

            Debug.Log($"[InvestigationTimeService] 사건 시간 초기화 | {caseId}");
        }

        // ------------------------------------------------------------- 내부

        /// <summary>기록이 없으면 null. 조회만으로 항목을 새로 만들지 않는다.</summary>
        private static CaseTimeState FindState(SaveData save, string caseId)
        {
            if (save == null || save.caseTimes == null || string.IsNullOrEmpty(caseId)) return null;

            for (int i = 0; i < save.caseTimes.Count; i++)
            {
                if (save.caseTimes[i] != null && save.caseTimes[i].caseId == caseId) return save.caseTimes[i];
            }
            return null;
        }
    }
}
