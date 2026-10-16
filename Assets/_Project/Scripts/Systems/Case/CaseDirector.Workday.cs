using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 본편 연결부. 하루 진행 자체는 WorkdayDirector 가 맡고, 여기서는 화면과 숙소(침대·컴퓨터)를 잇기만 한다.
    /// 튜토리얼 흐름은 건드리지 않는다. 본편이 시작되기 전(InMain 이 아닐 때)에는 아무것도 하지 않는다.
    /// </summary>
    public partial class CaseDirector
    {
        [Tooltip("업무 현황판 / 야간 확산 보고 / 근무 종료 화면.")]
        [SerializeField] private WorkdayScreen _workdayScreen;

        private WorkdayDirector _workday;

        /// <summary>현황판에서 고른 사건. 자세한 내용을 오른쪽에 보여 준다.</summary>
        private string _workFocusCaseId;

        /// <summary>본편이 시작됐는가.</summary>
        private bool InMain => _workday != null && _workday.MainStarted;

        private const string WorkboardAppId = "workboard";

        /// <summary>Start 에서 저장본을 불러온 뒤 부른다.</summary>
        private void InitWorkday()
        {
            _workday = gameObject.GetComponent<WorkdayDirector>();
            if (_workday == null) _workday = gameObject.AddComponent<WorkdayDirector>();
            _workday.Init(_tutorial);
            _workday.ShiftEnded += OnWorkShiftEnded;
            _workday.GameOver += OnWorkGameOver;
            _workday.RestoreFromSave();
        }

        /// <summary>
        /// 행동이나 이동으로 시간이 흘렀다. 본편이면 자연 확산과 퇴근 시각을 본다(튜토리얼의 게시판 흐름 대신).
        /// 본편이 아니면 false 를 돌려주고, 부르는 쪽이 튜토리얼 방식으로 처리한다.
        /// </summary>
        private bool OnMainTimeAdvanced()
        {
            if (!InMain) return false;
            _workday.OnTimeAdvanced();
            return true;
        }

        // ------------------------------------------------------------- 숙소

        /// <summary>본편 숙소의 침대. 언제든 잠들 수 있다. 멸망급 괴담이 있으면 먼저 알린다.</summary>
        private bool TryHandleMainRoomPoint(InvestigationPoint point)
        {
            if (!InMain || point == null || point.PointId != RoomBedPointId) return false;

            bool doomed = false;
            foreach (var c in _workday.ActiveCases())
                if (_workday.LegendBelief(c.LegendId) >= 85f) doomed = true;

            var labels = new List<System.Func<string>>
            {
                () => _loc.Get(SleepYesTextId),
                () => _loc.Get(SleepNoTextId),
            };
            string askId = doomed ? WorkSleepWarnTextId : _workday.OnDuty ? WorkSleepEarlyTextId : WorkSleepAskTextId;
            _fieldHudScreen.ShowLine(FieldSpeakerTextId, () => _loc.Get(askId), () =>
                _fieldHudScreen.ShowChoices(FieldSpeakerTextId, labels, OnSleepChoice));
            return true;
        }

        private const string WorkSleepAskTextId = "ui.work.sleep_ask";
        private const string WorkSleepEarlyTextId = "ui.work.sleep_early";
        private const string WorkSleepWarnTextId = "ui.work.sleep_warn";

        /// <summary>
        /// 잠들었다(OnFellAsleep 끝에서 부른다). 튜토리얼 첫날 밤이면 여기서 본편이 시작된다.
        /// 다음 출근(18:00)까지 시간이 흐르고, 깨어나면 야간 확산 보고 → 업무 현황판 순으로 띄운다.
        /// </summary>
        private void AfterFellAsleep()
        {
            if (_workday == null) return;

            WorkdayDirector.DayReport report;
            if (!InMain)
            {
                bool solved = _case != null && _case.PostWriting != null && _postWriting != null && _postWriting.IsFinished(_case.PostWriting);
                _sleepingThrough = true;
                report = _workday.BeginMainFromTutorial(TutorialDirector.TutorialCaseId, solved);
            }
            else
            {
                _sleepingThrough = true;
                report = _workday.Sleep();
            }
            _sleepingThrough = false;
            _save.AutoSave();   // 숨겨진 임시 기록(slot_0). 수동 슬롯은 건드리지 않는다

            ScreenCurtain.Fade(1f, 1.2f, () =>
            {
                WakeInRoom();
                ShowNightReport(report);
                ScreenCurtain.Fade(0f, 1.0f, null);
            });
        }

        /// <summary>18:00 에 깨어난다. 방은 불이 켜지고 한영은 없다.</summary>
        private void WakeInRoom()
        {
            _night = NightState.None;
            var room = GetRoomNight();
            if (room != null)
            {
                room.ResetRoom();
                room.SetHanyoungPresent(false);
            }
            ShowRoom();
            if (_field != null) _field.InvestigationAllowed = true;
            // 침대에서 일어나 방 안쪽에 선다. 숙소에 들어설 때와 같은 자리다.
            if (_field != null && _field.ActiveRoot != null)
                foreach (var walker in _field.ActiveRoot.GetComponentsInChildren<FieldWalker>(true)) walker.ResetToStart();
            PushStatus();
        }

        /// <summary>타이틀의 시작을 눌렀는데 본편 진행이 있으면 숙소에서 이어 간다. 현황판을 먼저 띄운다.</summary>
        private bool TryResumeMain()
        {
            if (!InMain) return false;
            _workday.RestoreFromSave();
            if (_case == null && _cases != null && _cases.TryGetCase(TutorialDirector.TutorialCaseId, out var tutorialCase)) _case = tutorialCase;
            EnterRoom(false);
            var room = GetRoomNight();
            if (room != null) room.SetHanyoungPresent(false);
            ShowWorkBoard();
            Debug.Log($"[CaseDirector] 본편 이어 하기 | 근무 {_workday.Workday}일차 {WorkdayDirector.FormatClock24()}");
            return true;
        }

        // ------------------------------------------------------------- 화면

        private void PushWorkScreen(WorkdayScreen.View view)
        {
            if (_workdayScreen == null) return;
            _workdayScreen.Show(view);
            if (!_ui.Contains(_workdayScreen)) _ui.Push(_workdayScreen);
        }

        private void CloseWorkScreen()
        {
            if (_workdayScreen != null && _ui.Contains(_workdayScreen)) _ui.Close(_workdayScreen);
        }

        private string L(string id, params object[] args) => args == null || args.Length == 0 ? _loc.Get(id) : _loc.Get(id, args);

        private static string Pct(float v) => v < 0f ? "-" : v.ToString("0.#") + "%";

        private static string Delta(float d) => float.IsNaN(d) ? "-" : (d > 0.05f ? "▲ " : d < -0.05f ? "▼ " : "") + Mathf.Abs(d).ToString("0.#") + "%p";

        private string Hours(int minutes) => L("ui.work.duration", minutes / 60, minutes % 60);

        private string CaseName(CaseSO c) => c != null ? _loc.Get(c.CaseNameTextId) : "-";

        private string LegendName(string legendId)
        {
            var legend = _legends != null ? _legends.GetLegend(legendId) : null;
            return legend != null ? _loc.Get(legend.NameTextId) : legendId;
        }

        private static int GradeLevel(float belief) => belief >= 100f ? 6 : (int)LegendRiskLevelExtensions.FromBelief(belief);

        private string GradeName(float belief) => belief >= 100f ? L("ui.work.grade_beyond") : _loc.Get(LegendRiskLevelExtensions.FromBelief(belief).ToTextId());

        /// <summary>업무 현황판. 출근하면, 또는 숙소 컴퓨터의 업무 현황 앱에서 연다.</summary>
        private void ShowWorkBoard()
        {
            if (!InMain) return;
            var active = _workday.ActiveCases();
            if (string.IsNullOrEmpty(_workFocusCaseId) || active.Find(c => c.CaseId == _workFocusCaseId) == null)
                _workFocusCaseId = active.Count > 0 ? active[0].CaseId : null;

            var view = new WorkdayScreen.View
            {
                Bureau = L("ui.work.bureau"),
                Title = L("ui.work.board_title"),
                Sub = L("ui.work.board_sub", _workday.FormatDate(_loc), WorkdayDirector.FormatClock24(), _workday.Workday),
                Columns = new[] { L("ui.work.col_case"), L("ui.work.col_belief"), L("ui.work.col_grade"), L("ui.work.col_delta") },
                ColumnWidths = new[] { 560f, 170f, 180f, 200f },
                SelectedKey = _workFocusCaseId,
                OnSelect = id => { _workFocusCaseId = id; ShowWorkBoard(); },
                PrimaryLabel = L("ui.work.btn_close"),
                OnPrimary = CloseWorkScreen,
            };

            float overall = _workday.OverallBelief;
            view.Stats = new[]
            {
                L("ui.work.stat_overall") + "\n" + Pct(overall) + "  <size=60%>" + Delta(_workday.DeltaSincePrevDay("*")) + "</size>",
                L("ui.work.stat_left") + "\n" + (_workday.OnDuty ? Hours(_workday.MinutesUntilShiftEnd) : L("ui.work.off_duty")),
                L("ui.work.stat_active") + "\n" + active.Count + " / " + _workday.Balance.MaxActiveCases(_workday.SolvedCount),
                L("ui.work.stat_solved") + "\n" + L("ui.work.count", _workday.SolvedCount),
            };
            if (overall >= 85f) view.DangerStat = 0;

            foreach (var c in active)
            {
                float b = _workday.LegendBelief(c.LegendId);
                float d = _workday.DeltaSincePrevDay(c.LegendId);
                bool urgent = b >= 75f;
                view.Rows.Add(new WorkdayScreen.Row
                {
                    Key = c.CaseId,
                    Name = CaseName(c),
                    Sub = LegendName(c.LegendId),
                    Cells = new[] { string.Empty, Pct(b), GradeName(b), Delta(d) },
                    GradeCell = 2,
                    GradeLevel = GradeLevel(b),
                    DeltaCell = 3,
                    DeltaSign = float.IsNaN(d) ? 0 : d > 0.05f ? 1 : d < -0.05f ? -1 : 0,
                    Tag = urgent ? L("ui.work.tag_urgent") : float.IsNaN(d) ? L("ui.work.tag_new") : null,
                    TagDanger = urgent,
                });
            }

            var focus = active.Find(c => c.CaseId == _workFocusCaseId);
            if (focus != null)
            {
                float b = _workday.LegendBelief(focus.LegendId);
                float spread = _spread != null ? _spread.GetSpreadRate(_save.Current, focus.LegendId) : 0f;
                view.DetailTitle = CaseName(focus);
                view.Detail.Add(_loc.Get(focus.CaseDescriptionTextId));
                view.Detail.Add(string.Empty);
                view.Detail.Add(L("ui.work.detail_legend", LegendName(focus.LegendId)));
                view.Detail.Add(L("ui.work.detail_belief", Pct(b), GradeName(b)));
                view.Detail.Add(L("ui.work.detail_spread", spread.ToString("0")));
                view.Detail.Add(L("ui.work.detail_sealed", L(_exorcism != null && _exorcism.IsSealed(_save.Current, focus.LegendId) ? "ui.work.yes" : "ui.work.no")));
            }
            if (active.Count == 0) view.Notes.Add(L("ui.work.note_no_case"));

            PushWorkScreen(view);
        }

        /// <summary>야간 확산 보고. 잠들기 전과 출근 뒤의 믿음도를 괴담마다 늘어놓는다.</summary>
        private void ShowNightReport(WorkdayDirector.DayReport report)
        {
            if (report == null) { ShowWorkBoard(); return; }

            var view = new WorkdayScreen.View
            {
                Bureau = L("ui.work.bureau"),
                Title = L("ui.work.report_title"),
                Sub = L("ui.work.report_sub", _workday.FormatDate(_loc), WorkdayDirector.FormatClock24(), _workday.Workday, Hours(report.MinutesSlept)),
                Columns = new[] { L("ui.work.col_case"), L("ui.work.col_before"), L("ui.work.col_after"), L("ui.work.col_grade") },
                ColumnWidths = new[] { 640f, 220f, 220f, 230f },
                PrimaryLabel = report.GameOver ? L("ui.work.btn_title") : L("ui.work.btn_to_board"),
                OnPrimary = report.GameOver ? (System.Action)(() => { CloseWorkScreen(); ShowTitleScreen(); }) : ShowWorkBoard,
            };

            view.Stats = new[]
            {
                L("ui.work.stat_overall_before") + "\n" + Pct(report.OverallBefore),
                L("ui.work.stat_overall_after") + "\n" + Pct(report.OverallAfter),
                L("ui.work.stat_new_cases") + "\n" + L("ui.work.count", report.NewCaseIds.Count),
            };
            if (report.OverallAfter >= 85f) view.DangerStat = 1;

            foreach (var ch in report.Changes)
            {
                var c = _workday.FindCase(ch.CaseId);
                bool gradeUp = !ch.IsNew && GradeLevel(ch.After) > GradeLevel(ch.Before);
                view.Rows.Add(new WorkdayScreen.Row
                {
                    Key = ch.CaseId,
                    Name = c != null ? CaseName(c) : LegendName(ch.LegendId),
                    Sub = LegendName(ch.LegendId),
                    Cells = new[] { string.Empty, ch.IsNew ? "-" : Pct(ch.Before), Pct(ch.After), GradeName(ch.After) + (gradeUp ? " ▲" : string.Empty) },
                    GradeCell = 3,
                    GradeLevel = GradeLevel(ch.After),
                    Tag = ch.IsNew ? L("ui.work.tag_new") : gradeUp ? L("ui.work.tag_grade_up") : null,
                    TagDanger = gradeUp,
                });
            }

            foreach (var w in report.Warnings)
            {
                var parts = w.Split('|');
                string who = parts[0] == "*" ? L("ui.work.overall") : LegendName(parts[0]);
                view.Notes.Add(L("ui.work.note_warning", who, parts.Length > 1 ? parts[1] : "?"));
            }
            if (report.GameOver)
            {
                string who = report.GameOverLegendId == "*" ? L("ui.work.overall") : LegendName(report.GameOverLegendId);
                view.Banner = L("ui.work.gameover", who);
            }

            PushWorkScreen(view);
        }

        /// <summary>03:00. 근무가 끝났다. 오늘 바뀐 것을 보여 주고 숙소로 돌려보낸다.</summary>
        private void OnWorkShiftEnded()
        {
            var view = new WorkdayScreen.View
            {
                Bureau = L("ui.work.bureau"),
                Title = L("ui.work.end_title"),
                Sub = L("ui.work.end_sub", _workday.FormatDate(_loc), WorkdayDirector.FormatClock24(), _workday.Workday),
                Columns = new[] { L("ui.work.col_case"), L("ui.work.col_start"), L("ui.work.col_now"), L("ui.work.col_grade") },
                ColumnWidths = new[] { 640f, 220f, 220f, 230f },
                PrimaryLabel = L("ui.work.btn_ok"),
                OnPrimary = CloseWorkScreen,
            };
            var w = _save.Current.work;
            foreach (var c in _workday.ActiveCases())
            {
                float start = WorkdayDirector.ReadSnapshot(w.dayStartBeliefs, c.LegendId);
                float now = _workday.LegendBelief(c.LegendId);
                view.Rows.Add(new WorkdayScreen.Row
                {
                    Key = c.CaseId,
                    Name = CaseName(c),
                    Sub = LegendName(c.LegendId),
                    Cells = new[] { string.Empty, Pct(start), Pct(now), GradeName(now) },
                    GradeCell = 3,
                    GradeLevel = GradeLevel(now),
                });
            }
            view.Stats = new[]
            {
                L("ui.work.stat_overall") + "\n" + Pct(_workday.OverallBelief),
                L("ui.work.stat_solved") + "\n" + L("ui.work.count", _workday.SolvedCount),
            };
            view.Notes.Add(L("ui.work.note_go_sleep"));
            PushWorkScreen(view);
        }

        /// <summary>잠드는 중인가. 잠든 사이의 게임 오버는 야간 확산 보고가 알린다.</summary>
        private bool _sleepingThrough;

        /// <summary>
        /// 근무 중 행동으로 괴담 하나 또는 전체가 100% 에 닿았다. 원인과 함께 게임 오버를 띄운다.
        /// 실패 화면의 연출과 저장 파일 불러오기는 퇴마·게임 오버 단계에서 붙는다. 지금은 타이틀로만 돌아간다.
        /// </summary>
        private void OnWorkGameOver(string legendId)
        {
            Debug.Log($"[CaseDirector] 게임 오버 | {legendId}");
            if (_sleepingThrough) return;

            string who = legendId == "*" ? L("ui.work.overall") : LegendName(legendId);
            var view = new WorkdayScreen.View
            {
                Bureau = L("ui.work.bureau"),
                Title = L("ui.work.gameover_title"),
                Sub = L("ui.work.board_sub", _workday.FormatDate(_loc), WorkdayDirector.FormatClock24(), _workday.Workday),
                Banner = L("ui.work.gameover", who),
                Columns = new[] { L("ui.work.col_case"), L("ui.work.col_belief"), L("ui.work.col_grade") },
                ColumnWidths = new[] { 640f, 220f, 230f },
                PrimaryLabel = L("ui.work.btn_title"),
                OnPrimary = () => { CloseWorkScreen(); ShowTitleScreen(); },
            };
            foreach (var c in _workday.ActiveCases())
            {
                float b = _workday.LegendBelief(c.LegendId);
                view.Rows.Add(new WorkdayScreen.Row
                {
                    Key = c.CaseId, Name = CaseName(c), Sub = LegendName(c.LegendId),
                    Cells = new[] { string.Empty, Pct(b), GradeName(b) }, GradeCell = 2, GradeLevel = GradeLevel(b),
                });
            }
            PushWorkScreen(view);
        }
    }
}
