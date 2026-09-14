using UnityEngine;
using UrbanLegendBureau.Save;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 사건 진행 상태를 SaveData 위에서 직접 다루는 도우미.
    ///
    /// 별도의 서비스로 만들지 않은 이유: 이번 단계에 필요한 것은
    /// 기존 SaveData 필드를 읽고 쓰는 몇 줄뿐이다. 상태를 이중으로 들고 있으면
    /// 저장본과 어긋날 위험만 생긴다.
    ///
    /// 사용하는 기존 필드: currentCaseId / currentStepIndex /
    ///                   acquiredClueIds / completedCaseIds / legendStates
    /// </summary>
    public static class CaseFlow
    {
        /// <summary>사건을 시작한다. 이미 진행 중인 같은 사건이면 단계를 유지한다.</summary>
        public static void StartCase(SaveService save, string caseId, string legendId)
        {
            if (save == null || save.Current == null) return;

            var data = save.Current;

            if (data.currentCaseId != caseId)
            {
                data.currentCaseId = caseId;
                data.currentStepIndex = (int)CaseStep.Started;
            }

            if (!string.IsNullOrEmpty(legendId))
            {
                var state = data.GetOrCreateLegendState(legendId);
                state.isDiscovered = true;
            }

            save.MarkDirty();
        }

        public static CaseStep GetStep(SaveService save)
        {
            if (save == null || save.Current == null) return CaseStep.Started;
            return (CaseStep)save.Current.currentStepIndex;
        }

        /// <summary>단계를 진행한다. 뒤로 되돌리지는 않는다.</summary>
        public static void SetStep(SaveService save, CaseStep step)
        {
            if (save == null || save.Current == null) return;
            if (save.Current.currentStepIndex >= (int)step) return;

            save.Current.currentStepIndex = (int)step;
            save.MarkDirty();
        }

        /// <summary>단서를 획득한다. 이미 가지고 있으면 아무 일도 하지 않고 false를 돌려준다.</summary>
        public static bool AcquireClue(SaveService save, string clueId)
        {
            if (save == null || save.Current == null || string.IsNullOrEmpty(clueId)) return false;

            var list = save.Current.acquiredClueIds;
            if (list.Contains(clueId)) return false;

            list.Add(clueId);
            save.MarkDirty();
            return true;
        }

        public static bool HasClue(SaveService save, string clueId)
        {
            if (save == null || save.Current == null || string.IsNullOrEmpty(clueId)) return false;
            return save.Current.acquiredClueIds.Contains(clueId);
        }

        /// <summary>사건을 완료 처리하고 저장한다. 이미 완료된 사건이면 중복 추가하지 않는다.</summary>
        public static bool CompleteCase(SaveService save, string caseId)
        {
            if (save == null || save.Current == null || string.IsNullOrEmpty(caseId)) return false;

            var data = save.Current;
            bool newlyCompleted = !data.completedCaseIds.Contains(caseId);
            if (newlyCompleted)
            {
                data.completedCaseIds.Add(caseId);
            }

            data.currentStepIndex = (int)CaseStep.Completed;
            save.MarkDirty();

            if (!save.Save())
            {
                Debug.LogError("[CaseFlow] 사건 완료 저장에 실패했다.");
            }

            return newlyCompleted;
        }

        public static bool IsCaseCompleted(SaveService save, string caseId)
        {
            if (save == null || save.Current == null || string.IsNullOrEmpty(caseId)) return false;
            return save.Current.completedCaseIds.Contains(caseId);
        }
    }
}
