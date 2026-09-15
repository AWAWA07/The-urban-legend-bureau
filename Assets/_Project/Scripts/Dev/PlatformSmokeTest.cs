using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.InputSystemLayer;
using UrbanLegendBureau.Localization;
using UrbanLegendBureau.Save;
using UrbanLegendBureau.Systems;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Dev
{
    /// <summary>
    /// 플랫폼 빌드 검증용 자동 스모크 테스트.
    /// 빌드된 앱에서는 사람이 직접 눌러보지 않으면 확인할 수 없는 항목들을
    /// 시작 시 스스로 실행하고 결과를 로그로 남긴다.
    ///
    ///   Windows : Player.log
    ///   Android : adb logcat
    ///   WebGL   : 브라우저 콘솔
    ///
    /// 기반 검증이 끝나면 제거해도 되는 개발 전용 컴포넌트다. 게임 로직에는 관여하지 않는다.
    /// </summary>
    public class PlatformSmokeTest : MonoBehaviour
    {
        private const string RunFlagPrefix = "smoke_run_";
        private const string Tag = "[SMOKE]";
        private const string TestLegendId = "legend_test_001";
        private const string MissingLegendId = "legend_not_exist";

        [Tooltip("UI 스택 테스트 단계 사이의 대기 시간. 화면 전환을 눈으로 확인할 여유를 준다.")]
        [SerializeField] private float _stepDelay = 0.4f;

        private readonly List<string> _failures = new List<string>();

        private IEnumerator Start()
        {
            // GameRoot 부팅과 UITestController의 시작 화면 구성이 끝난 뒤에 시작한다.
            yield return null;
            yield return null;

            LogEnvironment();

            yield return RunLocalizationTest();
            yield return RunInputTest();
            yield return RunUIStackTest();
            yield return RunSaveTest();
            yield return RunLegendDataTest();
            yield return RunSpreadLevelTest();
            yield return RunInvestigationTimeTest();

            if (_failures.Count == 0)
            {
                Debug.Log($"{Tag} 결과: 전체 통과");
            }
            else
            {
                Debug.LogError($"{Tag} 결과: {_failures.Count}건 실패\n  " + string.Join("\n  ", _failures));
            }
        }

        // ------------------------------------------------------------- 환경

        private void LogEnvironment()
        {
            var safeArea = Screen.safeArea;
            var sb = new StringBuilder();
            sb.AppendLine($"{Tag} === 환경 ===");
            sb.AppendLine($"  플랫폼      : {PlatformInfo.Kind} / {Application.platform}");
            sb.AppendLine($"  해상도      : {Screen.width}x{Screen.height} (aspect {ScreenSetup.CurrentAspect:F3})");
            sb.AppendLine($"  화면 방향   : {Screen.orientation}");
            sb.AppendLine($"  Safe Area   : x{safeArea.x:F0} y{safeArea.y:F0} w{safeArea.width:F0} h{safeArea.height:F0}");
            sb.AppendLine($"  Safe 여백   : 좌{safeArea.x:F0} 하{safeArea.y:F0} 우{Screen.width - safeArea.xMax:F0} 상{Screen.height - safeArea.yMax:F0}");
            sb.AppendLine($"  저장 Flush  : {PlatformInfo.RequiresStorageFlush}");
            Debug.Log(sb.ToString());

            float aspect = ScreenSetup.CurrentAspect;
            if (aspect < 1f)
            {
                Fail($"화면이 세로 방향이다 (aspect {aspect:F2}). 가로 고정이 적용되지 않았다.");
            }
        }

        // ------------------------------------------------------------- 개별 테스트

        private IEnumerator RunLocalizationTest()
        {
            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc))
            {
                Fail("LocalizationService 미등록");
                yield break;
            }

            string ko = loc.Get("ui.title.game_name");
            loc.SetLanguage("en");
            string en = loc.Get("ui.title.game_name");
            loc.SetLanguage("ko");
            string back = loc.Get("ui.title.game_name");

            Debug.Log($"{Tag} Localization | 항목 {loc.EntryCount}개 | ko='{ko}' en='{en}' 복귀='{back}'");

            if (ko.StartsWith("#")) Fail("한국어 문자열을 찾지 못했다: " + ko);
            if (en.StartsWith("#")) Fail("영어 문자열을 찾지 못했다: " + en);
            if (ko != back) Fail("언어 복귀 후 값이 달라졌다.");

            yield return null;
        }

        private IEnumerator RunInputTest()
        {
            if (!ServiceRegistry.TryGet<InputService>(out var input))
            {
                Fail("InputService 미등록");
                yield break;
            }

            Debug.Log($"{Tag} Input | Ready={input.IsReady} Mode={input.CurrentMode} " +
                      $"Pointer={(input.Pointer != null ? "있음" : "없음")} 터치기기={(Touchscreen() ? "연결됨" : "없음")}");

            if (!input.IsReady) Fail("InputService가 준비되지 않았다.");
            if (input.Pointer == null) Fail("PointerInput이 생성되지 않았다.");

            yield return null;
        }

        private static bool Touchscreen()
        {
            return UnityEngine.InputSystem.Touchscreen.current != null;
        }

        private IEnumerator RunUIStackTest()
        {
            if (!ServiceRegistry.TryGet<UIService>(out var ui))
            {
                Fail("UIService 미등록");
                yield break;
            }

            UIScreen screenB = null;
            UIScreen popup = null;
            foreach (var s in FindObjectsByType<UIScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (s.ScreenId == "Screen_B") screenB = s;
                else if (s.ScreenId == "Popup_Test") popup = s;
            }

            if (screenB == null || popup == null)
            {
                Fail("테스트용 화면(Screen_B / Popup_Test)을 찾지 못했다.");
                yield break;
            }

            int baseCount = ui.Count;
            Debug.Log($"{Tag} UI 시작 스택({ui.Count}): {ui.DescribeStack()}");

            // Push
            ui.Push(screenB);
            yield return new WaitForSeconds(_stepDelay);
            Expect(ui.Count == baseCount + 1, $"Push 후 개수 {ui.Count}, 기대 {baseCount + 1}");
            Expect(ui.Current == screenB, "Push 후 최상단이 Screen_B가 아니다.");

            // Popup (아래를 덮지 않아야 한다)
            ui.Push(popup);
            yield return new WaitForSeconds(_stepDelay);
            Expect(ui.Count == baseCount + 2, $"Popup 후 개수 {ui.Count}, 기대 {baseCount + 2}");
            Expect(screenB.gameObject.activeInHierarchy, "팝업이 아래 화면을 가렸다.");

            // Back (ESC / Android 뒤로가기와 같은 경로)
            ui.Back();
            yield return new WaitForSeconds(_stepDelay);
            Expect(ui.Count == baseCount + 1, $"Back 후 개수 {ui.Count}, 기대 {baseCount + 1}");
            Expect(!popup.gameObject.activeInHierarchy, "Back 후에도 팝업이 남아 있다.");

            // Replace (깊이 유지)
            int beforeReplace = ui.Count;
            ui.Replace(popup);
            yield return new WaitForSeconds(_stepDelay);
            Expect(ui.Count == beforeReplace, $"Replace 후 깊이가 바뀌었다. {beforeReplace} -> {ui.Count}");

            // Pop
            ui.Pop();
            yield return new WaitForSeconds(_stepDelay);
            Expect(ui.Count == baseCount, $"Pop 후 개수 {ui.Count}, 기대 {baseCount}");

            Debug.Log($"{Tag} UI 종료 스택({ui.Count}): {ui.DescribeStack()}");
        }

        private IEnumerator RunSaveTest()
        {
            if (!ServiceRegistry.TryGet<SaveService>(out var save))
            {
                Fail("SaveService 미등록");
                yield break;
            }

            Debug.Log($"{Tag} Save | 백엔드={save.BackendName} 위치={save.SaveLocation}");

            // --- 이전 실행 기록을 불러온다 ---
            bool hadSave = save.HasSave();
            int previousRuns = 0;

            if (hadSave && save.Load())
            {
                previousRuns = CountRuns(save.Current);
                Debug.Log($"{Tag} Save | 이전 저장 발견 -> 지금까지 실행 횟수 = {previousRuns}");
            }
            else
            {
                save.NewGame();
                Debug.Log($"{Tag} Save | 저장 없음 -> 첫 실행으로 시작");
            }

            // --- 같은 실행 안에서 쓰기/읽기 왕복 ---
            var data = save.Current;
            data.currentCaseId = "case_smoke";
            data.currentStepIndex = previousRuns + 1;
            data.global.beliefLevel = 40 + previousRuns;
            data.storyFlags.Add(RunFlagPrefix + (previousRuns + 1));
            save.MarkDirty();

            bool saved = save.Save();
            Expect(saved, "Save()가 실패했다.");
            Expect(save.HasSave(), "저장 후 HasSave()가 false다.");

            save.NewGame();
            Expect(save.Current.currentCaseId == string.Empty, "NewGame 후 메모리가 비워지지 않았다.");

            bool loaded = save.Load();
            Expect(loaded, "Load()가 실패했다.");
            if (loaded)
            {
                Expect(save.Current.currentCaseId == "case_smoke", "로드 후 currentCaseId 불일치");
                Expect(save.Current.currentStepIndex == previousRuns + 1, "로드 후 currentStepIndex 불일치");
                Expect(CountRuns(save.Current) == previousRuns + 1, "로드 후 실행 기록 개수 불일치");
            }

            Debug.Log($"{Tag} Save | 왕복 완료. 이번 실행까지 누적 = {CountRuns(save.Current)}회");
            Debug.Log($"{Tag} Save | 앱을 다시 실행하면 누적이 {CountRuns(save.Current) + 1}회로 늘어나야 한다.");

            yield return null;
        }

        /// <summary>
        /// 괴담 정적 데이터를 ID로 조회할 수 있는지, 그리고 저장된 legendId 가
        /// 실제 LegendSO 로 다시 이어지는지 확인한다.
        /// </summary>
        private IEnumerator RunLegendDataTest()
        {
            if (!ServiceRegistry.TryGet<LegendService>(out var legends))
            {
                Fail("LegendService 미등록");
                yield break;
            }

            ServiceRegistry.TryGet<LocalizationService>(out var loc);
            Debug.Log($"{Tag} Legend | 등록된 괴담 {legends.Count}개");

            // --- 정상 ID ---
            Expect(legends.HasLegend(TestLegendId), $"HasLegend({TestLegendId}) 가 false다.");

            var legend = legends.GetLegend(TestLegendId);
            if (legend == null)
            {
                Fail($"GetLegend({TestLegendId}) 가 null 이다.");
                yield break;
            }

            Expect(legend.LegendId == TestLegendId, "조회한 괴담의 legendId 가 다르다.");
            Expect(legend.Rules.Count > 0, "괴담에 연결된 규칙이 없다.");
            Expect(legend.Clues.Count > 0, "괴담에 연결된 단서가 없다.");
            Expect(legend.WebPages.Count > 0, "괴담에 연결된 게시글이 없다.");

            // --- 없는 ID ---
            Expect(!legends.HasLegend(MissingLegendId), $"HasLegend({MissingLegendId}) 가 true다.");
            Expect(!legends.TryGetLegend(MissingLegendId, out _), $"TryGetLegend({MissingLegendId}) 가 성공했다.");

            // --- Localization 해석 (ko / en) ---
            if (loc != null)
            {
                loc.SetLanguage("ko");
                string koName = loc.Get(legend.NameTextId);
                string koRisk = loc.Get(legend.RiskLevelTextId);

                loc.SetLanguage("en");
                string enName = loc.Get(legend.NameTextId);
                string enRisk = loc.Get(legend.RiskLevelTextId);

                loc.SetLanguage("ko");

                Debug.Log($"{Tag} Legend | ko='{koName}' / '{koRisk}'   en='{enName}' / '{enRisk}'   등급={legend.RiskLevel}");

                Expect(!koName.StartsWith("#"), "괴담 이름의 한국어 문자열이 없다.");
                Expect(!koRisk.StartsWith("#"), "위험 등급의 한국어 문자열이 없다.");
                Expect(!enName.StartsWith("#"), "괴담 이름의 영어 문자열이 없다.");
                Expect(koName != enName, "ko/en 이 같은 문자열이다. 언어 전환이 반영되지 않았다.");
            }

            // --- SaveData 의 legendId 로 다시 조회 ---
            if (ServiceRegistry.TryGet<SaveService>(out var save) && save.Current != null)
            {
                var state = save.Current.GetOrCreateLegendState(TestLegendId);
                state.isDiscovered = true;
                save.MarkDirty();

                var fromSave = legends.GetLegend(state.legendId);
                Expect(fromSave != null, "SaveData 의 legendId 로 LegendSO 를 찾지 못했다.");
                Expect(fromSave == legend, "SaveData 로 찾은 괴담이 원본과 다르다.");
                Debug.Log($"{Tag} Legend | SaveData legendId='{state.legendId}' -> '{(fromSave != null ? fromSave.name : "없음")}' 복원 성공");
            }

            yield return null;
        }

        /// <summary>
        /// 확산도 단계 경계값 검증.
        /// 경계는 한 곳(SpreadService)에서만 정하므로, 여기서 그 경계가 지켜지는지 확인한다.
        /// </summary>
        private IEnumerator RunSpreadLevelTest()
        {
            if (!ServiceRegistry.TryGet<SpreadService>(out var spread))
            {
                Fail("SpreadService 미등록");
                yield break;
            }

            // (확산도, 기대 단계)
            var cases = new (float rate, SpreadLevel expected)[]
            {
                (0f,     SpreadLevel.Stable),
                (24.99f, SpreadLevel.Stable),
                (25f,    SpreadLevel.Spreading),
                (49.99f, SpreadLevel.Spreading),
                (50f,    SpreadLevel.Dangerous),
                (74.99f, SpreadLevel.Dangerous),
                (75f,    SpreadLevel.Critical),
                (100f,   SpreadLevel.Critical),
            };

            var sb = new StringBuilder();
            sb.Append($"{Tag} Spread 경계값 |");

            foreach (var c in cases)
            {
                var actual = spread.GetSpreadLevel(c.rate);
                sb.Append($" {c.rate:0.##}→{actual}");
                Expect(actual == c.expected, $"확산도 {c.rate} 의 단계가 {actual}, 기대 {c.expected}");
            }
            Debug.Log(sb.ToString());

            // 경고 대상 판정
            Expect(!SpreadLevel.Stable.NeedsFieldWarning(), "Stable 이 경고 대상으로 판정됐다.");
            Expect(!SpreadLevel.Spreading.NeedsFieldWarning(), "Spreading 이 경고 대상으로 판정됐다.");
            Expect(SpreadLevel.Dangerous.NeedsFieldWarning(), "Dangerous 가 경고 대상이 아니다.");
            Expect(SpreadLevel.Critical.NeedsFieldWarning(), "Critical 이 경고 대상이 아니다.");

            yield return null;
        }

        /// <summary>
        /// 조사 행동으로 사건 시간이 흐르는지, 사건끼리 섞이지 않는지 확인한다.
        /// 실제 저장에 손대지 않도록 임시 SaveData를 따로 만들어 쓴다.
        /// </summary>
        private IEnumerator RunInvestigationTimeTest()
        {
            if (!ServiceRegistry.TryGet<InvestigationTimeService>(out var time))
            {
                Fail("InvestigationTimeService 미등록");
                yield break;
            }

            if (!ServiceRegistry.TryGet<SaveService>(out var save) || save.Current == null)
            {
                Fail("SaveService 미등록");
                yield break;
            }

            const string caseA = "smoke_case_a";
            const string caseB = "smoke_case_b";

            var data = save.Current;
            Expect(time.GetActionCount(data, caseA) == 0, "기록이 없는 사건의 행동 횟수가 0이 아니다.");

            time.RegisterAction(save, caseA, null, InvestigationAction.InternetView);
            time.RegisterAction(save, caseA, null, InvestigationAction.FieldSearch);
            time.RegisterAction(save, caseB, null, InvestigationAction.InternetView);

            Expect(time.GetActionCount(data, caseA) == 2, "행동 2회를 기록했는데 횟수가 맞지 않는다.");
            Expect(time.GetElapsedMinutes(data, caseA) == InvestigationTimeService.MinutesPerAction * 2,
                "경과 시간이 행동 횟수와 맞지 않는다.");
            Expect(time.GetActionCount(data, caseB) == 1, "다른 사건의 행동 횟수가 섞였다.");

            time.ResetCase(save, caseA);
            Expect(time.GetActionCount(data, caseA) == 0, "초기화 후에도 행동 횟수가 남아 있다.");
            Expect(time.GetActionCount(data, caseB) == 1, "한 사건을 초기화했더니 다른 사건까지 지워졌다.");

            Debug.Log($"{Tag} InvestigationTime | 행동 1회 = {InvestigationTimeService.MinutesPerAction}분 | " +
                      $"사건별 분리 확인 (A={time.GetActionCount(data, caseA)}회 B={time.GetActionCount(data, caseB)}회)");

            // 테스트용 항목은 남기지 않는다.
            data.caseTimes.RemoveAll(s => s != null && (s.caseId == caseA || s.caseId == caseB));

            yield return null;
        }

        private static int CountRuns(SaveData data)
        {
            if (data == null || data.storyFlags == null) return 0;

            int count = 0;
            for (int i = 0; i < data.storyFlags.Count; i++)
            {
                if (data.storyFlags[i] != null && data.storyFlags[i].StartsWith(RunFlagPrefix)) count++;
            }
            return count;
        }

        // ------------------------------------------------------------- 헬퍼

        private void Expect(bool condition, string failureMessage)
        {
            if (!condition) Fail(failureMessage);
        }

        private void Fail(string message)
        {
            _failures.Add(message);
            Debug.LogWarning($"{Tag} 실패: {message}");
        }
    }
}
