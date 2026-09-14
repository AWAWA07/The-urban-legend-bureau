using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Save;

namespace UrbanLegendBureau.Dev
{
    /// <summary>
    /// 3단계 저장 시스템 검증용. 개발 확인이 끝나면 제거해도 된다.
    /// 저장 -> 메모리 초기화 -> 로드 -> 값 일치 확인을 한 번에 수행한다.
    /// </summary>
    public class SaveTest : MonoBehaviour
    {
        private const string TestCaseId = "case_test_001";
        private const string TestCompletedCaseId = "case_test_000";
        private const string TestClueId = "clue_test_cctv_log";
        private const string TestRuleId = "rule_test_midnight_only";
        private const string TestPageId = "page_test_forum_17";
        private const string TestLegendId = "legend_test_red_mask";
        private const string TestFlag = "flag_test_prologue_done";
        private const int TestStepIndex = 3;
        private const int TestBeliefLevel = 42;

        [ContextMenu("저장/로드 테스트 실행")]
        public void RunTest()
        {
            if (!ServiceRegistry.TryGet<SaveService>(out var save))
            {
                Debug.LogError("[SaveTest] SaveService를 찾지 못했다. GameRoot가 부팅되었는지 확인할 것.");
                return;
            }

            Debug.Log($"[SaveTest] 백엔드={save.BackendName} | 저장 위치={save.SaveLocation}");

            // --- 1) 기존 저장 제거 후 초기 상태 확인 ---
            save.DeleteSave();
            Debug.Log($"[SaveTest] 1) 삭제 후 HasSave() = {save.HasSave()} (false 기대)");

            // --- 2) 테스트 데이터 작성 ---
            save.NewGame();
            var data = save.Current;
            data.currentCaseId = TestCaseId;
            data.currentStepIndex = TestStepIndex;
            data.completedCaseIds.Add(TestCompletedCaseId);
            data.acquiredClueIds.Add(TestClueId);
            data.deducedRuleIds.Add(TestRuleId);
            data.censoredPageIds.Add(TestPageId);
            data.storyFlags.Add(TestFlag);
            data.global.beliefLevel = TestBeliefLevel;
            data.global.chapterIndex = 2;

            var legend = data.GetOrCreateLegendState(TestLegendId);
            legend.spreadRate = 63.5f;
            legend.isDiscovered = true;
            legend.isSealed = false;

            data.settings.languageCode = "ko";
            data.settings.bgmVolume = 0.5f;
            save.MarkDirty();

            // --- 3) 저장 ---
            bool saved = save.Save();
            Debug.Log($"[SaveTest] 3) Save() = {saved} / HasSave() = {save.HasSave()} (둘 다 true 기대)");
            if (!saved) return;

            // --- 4) 메모리 상태를 완전히 날린다 ---
            save.NewGame();
            Debug.Log(
                $"[SaveTest] 4) 초기화 후 메모리 상태: caseId='{save.Current.currentCaseId}' " +
                $"step={save.Current.currentStepIndex} belief={save.Current.global.beliefLevel} " +
                $"clues={save.Current.acquiredClueIds.Count} (전부 비어 있어야 함)");

            // --- 5) 로드 ---
            bool loaded = save.Load();
            Debug.Log($"[SaveTest] 5) Load() = {loaded}");
            if (!loaded) return;

            // --- 6) 값 대조 ---
            var r = save.Current;
            var failures = new List<string>();

            Check(failures, "saveVersion", r.saveVersion, SaveMigration.CurrentVersion);
            Check(failures, "currentCaseId", r.currentCaseId, TestCaseId);
            Check(failures, "currentStepIndex", r.currentStepIndex, TestStepIndex);
            Check(failures, "completedCaseIds[0]", First(r.completedCaseIds), TestCompletedCaseId);
            Check(failures, "acquiredClueIds[0]", First(r.acquiredClueIds), TestClueId);
            Check(failures, "deducedRuleIds[0]", First(r.deducedRuleIds), TestRuleId);
            Check(failures, "censoredPageIds[0]", First(r.censoredPageIds), TestPageId);
            Check(failures, "storyFlags[0]", First(r.storyFlags), TestFlag);
            Check(failures, "global.beliefLevel", r.global.beliefLevel, TestBeliefLevel);
            Check(failures, "global.chapterIndex", r.global.chapterIndex, 2);
            Check(failures, "settings.languageCode", r.settings.languageCode, "ko");
            Check(failures, "settings.bgmVolume", r.settings.bgmVolume, 0.5f);

            if (r.legendStates.Count != 1)
            {
                failures.Add($"legendStates.Count: 기대 1, 실제 {r.legendStates.Count}");
            }
            else
            {
                var ls = r.legendStates[0];
                Check(failures, "legend.legendId", ls.legendId, TestLegendId);
                Check(failures, "legend.spreadRate", ls.spreadRate, 63.5f);
                Check(failures, "legend.isDiscovered", ls.isDiscovered, true);
                Check(failures, "legend.isSealed", ls.isSealed, false);
            }

            if (failures.Count == 0)
            {
                Debug.Log("[SaveTest] 6) 전체 항목 일치. 저장/로드 검증 성공.");
            }
            else
            {
                Debug.LogError("[SaveTest] 6) 불일치 발견:\n  " + string.Join("\n  ", failures));
            }

            // --- 7) 삭제 동작 확인 ---
            bool deleted = save.DeleteSave();
            Debug.Log($"[SaveTest] 7) DeleteSave() = {deleted} / HasSave() = {save.HasSave()} (true, false 기대)");
        }

        private static string First(List<string> list)
        {
            return list != null && list.Count > 0 ? list[0] : "(없음)";
        }

        private static void Check<T>(List<string> failures, string label, T actual, T expected)
        {
            if (!EqualityComparer<T>.Default.Equals(actual, expected))
            {
                failures.Add($"{label}: 기대 '{expected}', 실제 '{actual}'");
            }
        }
    }
}
