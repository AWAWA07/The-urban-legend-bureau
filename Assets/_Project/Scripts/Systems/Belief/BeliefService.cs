using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Save;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 전역 믿음 수치를 다룬다.
    ///
    /// 값은 SaveData.global.beliefLevel 에 그대로 들어간다.
    /// 믿음이 높을수록 괴담이 실체에 가까워진다는 것이 기획 의도지만,
    /// 이번 단계에서는 수치가 오르내리는 것까지만 구현한다.
    ///
    /// 주의: SaveData.global.beliefLevel 은 int 다. SaveData 구조를 바꾸지 않기 위해
    /// 소수점 증감은 반올림해서 반영한다. 세밀한 누적이 필요해지면 그때 필드를 논의한다.
    /// </summary>
    public class BeliefService : IService
    {
        public const float MinBelief = 0f;
        public const float MaxBelief = 100f;

        /// <summary>
        /// 확산 증가분이 믿음으로 옮겨가는 비율.
        /// 밸런스가 확정되지 않았으므로 한 곳에서만 조절한다.
        /// 예: 확산 +25 -> 믿음 +5
        /// </summary>
        public const float SpreadToBeliefRatio = 0.2f;

        public void Initialize()
        {
            Debug.Log($"[BeliefService] 준비 완료 | 범위 {MinBelief}~{MaxBelief} | 확산→믿음 비율 {SpreadToBeliefRatio}");
        }

        public void Shutdown()
        {
        }

        // ------------------------------------------------------------- 믿음 수치

        public float GetBeliefLevel(SaveData save)
        {
            if (save == null || save.global == null) return 0f;
            return save.global.beliefLevel;
        }

        public bool TryAddBelief(SaveData save, float amount)
        {
            if (save == null || save.global == null) return false;
            if (amount < 0f) return TryReduceBelief(save, -amount);

            float next = save.global.beliefLevel + amount;
            save.global.beliefLevel = Mathf.RoundToInt(Mathf.Clamp(next, MinBelief, MaxBelief));
            return true;
        }

        public bool TryReduceBelief(SaveData save, float amount)
        {
            if (save == null || save.global == null) return false;
            if (amount < 0f) return TryAddBelief(save, -amount);

            float next = save.global.beliefLevel - amount;
            save.global.beliefLevel = Mathf.RoundToInt(Mathf.Clamp(next, MinBelief, MaxBelief));
            return true;
        }

        /// <summary>확산 증가분에 대응하는 믿음 증가분을 계산한다. 실제 적용은 호출 측이 한다.</summary>
        public static float SpreadToBelief(float spreadAmount)
        {
            return spreadAmount * SpreadToBeliefRatio;
        }
    }
}
