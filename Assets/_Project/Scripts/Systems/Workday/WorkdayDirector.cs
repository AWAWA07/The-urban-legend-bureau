using System;
using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.Localization;
using UrbanLegendBureau.Save;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 본편의 하루 진행을 맡는다. 튜토리얼이 끝난 뒤(첫날 밤 취침)부터 켜진다.
    ///
    ///   시간    벽시계(GameClock) 하나를 그대로 쓴다. 1일차 22:30 부터 흐른 분에서 달력 날짜, 근무 일차, 근무 중인지를 계산한다.
    ///           본편에서는 실제 시간으로 흐르지 않고 행동할 때만 흐른다.
    ///   근무    18:00 출근 ~ 다음 날 03:00 퇴근. 자정을 넘겨도 같은 근무 일차다.
    ///   확산    맡고 있는 괴담은 근무 중에도, 잠든 동안에도 위험등급에 맞는 속도로 믿음도가 오른다(WorkdayBalanceSO).
    ///           어디까지 반영했는지 저장본에 적어 두어, 불러온 뒤 같은 시간이 두 번 반영되지 않는다.
    ///   사건    여러 사건이 동시에 활성이다. 근무가 시작될 때 등장 조건을 본 사건이 새로 활성된다.
    ///   게시판  믿음도는 지금처럼 게시판 글의 평균이다. 본편 사건의 괴담 글을 게시판에 걸고(괴담 ID 를 붙여),
    ///           글마다의 믿음도·조회·반응을 저장본에 적어 불러올 때 그 모습으로 되살린다.
    ///
    /// 튜토리얼에서는 아무것도 하지 않는다(MainStarted 가 아닐 때).
    /// </summary>
    public class WorkdayDirector : MonoBehaviour
    {
        // ------------------------------------------------------------- 보고서에 담는 것

        public class LegendChange
        {
            public string CaseId;
            public string LegendId;
            public float Before;
            public float After;
            public bool IsNew;
        }

        public class DayReport
        {
            public int MinutesSlept;
            public List<LegendChange> Changes = new List<LegendChange>();
            public float OverallBefore;
            public float OverallAfter;
            public List<string> NewCaseIds = new List<string>();
            public List<string> Warnings = new List<string>();   // "괴담ID|단계"
            public bool GameOver;
            public string GameOverLegendId;
        }

        private TutorialDirector _tutorial;
        private SaveService _save;
        private CaseService _cases;
        private LegendService _legends;
        private SpreadService _spread;
        private WorkdayBalanceSO _balance;

        /// <summary>근무가 03:00 으로 끝났을 때. 진행 담당이 근무 종료 화면을 띄운다.</summary>
        public event Action ShiftEnded;

        /// <summary>게임 오버가 났을 때. 원인 괴담 ID("*" 이면 전체).</summary>
        public event Action<string> GameOver;

        private const int ElapsedChunk = 10;

        public void Init(TutorialDirector tutorial)
        {
            _tutorial = tutorial;
            ServiceRegistry.TryGet(out _save);
            ServiceRegistry.TryGet(out _cases);
            ServiceRegistry.TryGet(out _legends);
            ServiceRegistry.TryGet(out _spread);
            _balance = WorkdayBalanceSO.Load();

            if (_tutorial != null) _tutorial.BoardRebuilt += OnBoardRebuilt;
            if (_tutorial != null) _tutorial.RiseBlocked = IsLegendResolved;
            if (_save != null) _save.BeforeSave += SyncToSave;
        }

        private void OnDestroy()
        {
            if (_tutorial != null) _tutorial.BoardRebuilt -= OnBoardRebuilt;
            if (_save != null) _save.BeforeSave -= SyncToSave;
        }

        private WorkState W => _save != null && _save.Current != null ? _save.Current.work : null;

        public bool MainStarted => W != null && W.mainStarted;
        public WorkdayBalanceSO Balance => _balance;

        // ------------------------------------------------------------- 시간 계산

        /// <summary>1일차 0시부터 흐른 분.</summary>
        public static int AbsoluteMinute => GameClock.StartMinuteOfDay + GameClock.ElapsedMinutes;

        /// <summary>달력으로 며칠째인가. 1일차 = 튜토리얼 날.</summary>
        public static int CalendarDay => AbsoluteMinute / 1440 + 1;

        /// <summary>그날 0시부터 흐른 분.</summary>
        public static int MinuteOfDay => ((AbsoluteMinute % 1440) + 1440) % 1440;

        private int ShiftStart => _balance.ShiftStartHour * 60;
        private int ShiftEnd => _balance.ShiftEndHour * 60;

        /// <summary>근무 일차. 18:00 에 하나 늘고, 자정을 지나 03:00 까지는 같은 일차다. 1일차 18:00 이전은 0.</summary>
        public int Workday => Mathf.FloorToInt((AbsoluteMinute - ShiftStart) / 1440f) + 1;

        /// <summary>지금 근무 시간(18:00~03:00) 안인가.</summary>
        public bool OnDuty => MinuteOfDay >= ShiftStart || MinuteOfDay < ShiftEnd;

        /// <summary>퇴근(03:00)까지 남은 분. 근무 시간이 아니면 0.</summary>
        public int MinutesUntilShiftEnd
        {
            get
            {
                int m = MinuteOfDay;
                if (m >= ShiftStart) return 1440 - m + ShiftEnd;
                if (m < ShiftEnd) return ShiftEnd - m;
                return 0;
            }
        }

        /// <summary>다음 출근(18:00)까지 남은 분.</summary>
        public int MinutesUntilNextShift
        {
            get
            {
                int m = MinuteOfDay;
                return m < ShiftStart ? ShiftStart - m : 1440 - m + ShiftStart;
            }
        }

        /// <summary>달력 날짜 문구. 1일차가 WorkdayBalanceSO 의 시작 날짜다.</summary>
        public string FormatDate(LocalizationService loc)
        {
            var date = _balance.StartDate.AddDays(CalendarDay - 1);
            string dow = loc.Get("ui.work.dow_" + (int)date.DayOfWeek);
            return loc.Get("ui.work.date", date.Month, date.Day, dow);
        }

        public static string FormatClock24()
        {
            int m = MinuteOfDay;
            return (m / 60).ToString("00") + ":" + (m % 60).ToString("00");
        }

        // ------------------------------------------------------------- 시작 / 되살리기

        /// <summary>새 판(튜토리얼부터)을 시작할 때. 본편 상태를 비운다. 시계는 다시 실제 시간으로 흐른다.</summary>
        public void ResetForNewRun()
        {
            if (_save == null || _save.Current == null) return;
            // 상승 차단부터 푼다. 지난 판의 글을 걷으며 되돌리는 몫이 막히지 않게 한다.
            _save.Current.work.resolvedLegendIds?.Clear();
            ResetMainCaseProgress();
            _save.Current.work = new WorkState();
            _save.Current.boardStates.Clear();
            _save.Current.completedCaseIds.Clear();   // 해결한 고유 사건 수도 새 판부터 다시 센다
            GameClock.SetRealTimeFlow(true);
            GameClock.Restart();   // 1일차 22:30 부터 다시 센다
            _save.Current.global.clockElapsedMinutes = -1;
            _save.Current.global.boardActivityMinute = -1;
        }

        /// <summary>
        /// 본편 사건들의 진행을 처음으로 되돌린다. 새 판에서 지난 판의 단서, 수사 노트, 올린 글, 확산도가 이어지지 않게 한다.
        /// 튜토리얼 사건은 튜토리얼이 스스로 처음부터 되돌리므로 건드리지 않는다.
        /// </summary>
        private void ResetMainCaseProgress()
        {
            var data = _save.Current;
            if (_cases == null) return;
            foreach (var c in _cases.GetAllCases())
            {
                if (c == null || !c.MainStory) continue;
                var legend = _legends != null ? _legends.GetLegend(c.LegendId) : null;
                if (legend != null)
                {
                    foreach (var clue in legend.Clues) if (clue != null) data.acquiredClueIds.Remove(clue.ClueId);
                    foreach (var rule in legend.Rules) if (rule != null) data.deducedRuleIds.Remove(rule.RuleId);
                    foreach (var page in legend.WebPages) if (page != null) data.storyFlags.Remove(ViewedFlagPrefix + page.PageId);
                }
                data.doneActions?.RemoveAll(k => k != null && k.StartsWith(c.CaseId + "|"));
                if (c.PostWriting != null) data.storyFlags.Remove(PostWritingDirector.PublishedFlag(c.PostWriting));
                PlayerPostStore.RemoveCase(data, c.CaseId, _tutorial);
                CaseNotebook.ResetCase(data, c.CaseId);
                data.legendStates?.RemoveAll(s => s != null && s.legendId == c.LegendId);
                data.caseTimes?.RemoveAll(s => s != null && s.caseId == c.CaseId);
                if (data.currentCaseId == c.CaseId) data.currentCaseId = null;
            }
            _save.MarkDirty();
        }

        /// <summary>InternetService 가 읽은 글에 붙이는 표시와 같다.</summary>
        private const string ViewedFlagPrefix = "web_viewed_";

        /// <summary>저장본을 불러온 뒤. 본편이면 시계를 멈춰 두고 적어 둔 시각으로 되돌린다.</summary>
        public void RestoreFromSave()
        {
            var w = W;
            if (w == null || !w.mainStarted) return;

            GameClock.SetRealTimeFlow(false);
            if (w.elapsedMinutes >= 0) GameClock.RestoreElapsed(w.elapsedMinutes);
            RepairResolvedLegends();
            GameClock.SetRealTimeFlow(false);
            Debug.Log($"[Workday] 본편 저장본 | {CalendarDay}일째 {FormatClock24()} | 근무 {Workday}일차");
        }

        /// <summary>
        /// 튜토리얼 첫날 밤에 잠들었다. 여기서 본편이 시작된다.
        /// 막차 사건을 해결 1건으로 적고, 다음 출근(2일차 18:00)까지 잠든 시간만큼 괴담이 퍼진다.
        /// </summary>
        public DayReport BeginMainFromTutorial(string tutorialCaseId, bool tutorialSolved)
        {
            var w = W;
            if (w == null) return null;

            w.mainStarted = true;
            if (tutorialSolved) MarkSolved(tutorialCaseId);

            // 이제부터 시간은 행동할 때만 흐른다.
            GameClock.SetRealTimeFlow(false);
            w.spreadAppliedMinute = GameClock.ElapsedMinutes;

            Debug.Log($"[Workday] 본편 시작 | 해결 {SolvedCount}건 | {CalendarDay}일째 {FormatClock24()}");
            return Sleep();
        }

        public int SolvedCount => _save != null && _save.Current != null ? _save.Current.completedCaseIds.Count : 0;

        /// <summary>사건 하나를 해결했다고 적는다. 같은 사건은 한 번만 센다.</summary>
        public void MarkSolved(string caseId)
        {
            if (string.IsNullOrEmpty(caseId) || _save == null || _save.Current == null) return;
            var list = _save.Current.completedCaseIds;
            if (!list.Contains(caseId)) list.Add(caseId);
            W?.activeCaseIds.Remove(caseId);
            ResolveLegendOf(caseId);
            _save.MarkDirty();
        }

        /// <summary>
        /// 사건이 다루는 괴담을 해결한 괴담으로 적는다. 그 괴담에 묶인 글(처음 글, 목격담, 소문, 나중에 올라온 같은 괴담 글)은
        /// 이때부터 믿음도가 더 오르지 않는다. 글은 지우지 않고, 해결 전에 오른 몫(실패한 정정글의 몫 포함)도 되돌리지 않는다.
        /// </summary>
        private void ResolveLegendOf(string caseId)
        {
            var c = FindCase(caseId);
            var w = W;
            if (c == null || w == null || string.IsNullOrEmpty(c.LegendId) || w.resolvedLegendIds.Contains(c.LegendId)) return;
            w.resolvedLegendIds.Add(c.LegendId);
            Debug.Log($"[Workday] 괴담 해결 | {c.LegendId} 글은 더 오르지 않는다 (믿음도 {LegendBelief(c.LegendId):0.00}% 그대로)");
        }

        /// <summary>본편에서 해결한 괴담인가. 게시판이 오르는 몫을 막을 때 묻는다. 튜토리얼에서는 늘 false 다.</summary>
        public bool IsLegendResolved(string legendId)
        {
            var w = W;
            return w != null && w.mainStarted && !string.IsNullOrEmpty(legendId) && w.resolvedLegendIds.Contains(legendId);
        }

        /// <summary>v12 이전 저장본처럼 해결한 사건은 있는데 해결한 괴담이 비어 있으면 채운다. 몇 번 불러도 같다.</summary>
        private void RepairResolvedLegends()
        {
            foreach (var id in _save.Current.completedCaseIds) ResolveLegendOf(id);
        }

        public bool IsSolved(string caseId) => _save != null && _save.Current != null && _save.Current.completedCaseIds.Contains(caseId);

        // ------------------------------------------------------------- 사건

        public List<CaseSO> ActiveCases()
        {
            var list = new List<CaseSO>();
            var w = W;
            if (w == null || _cases == null) return list;
            foreach (var id in w.activeCaseIds)
            {
                if (_cases.TryGetCase(id, out var c) && c != null) list.Add(c);
            }
            return list;
        }

        /// <summary>
        /// 근무가 시작될 때 새 사건을 활성한다. 등장 일차가 됐고, 아직 해결하지 않았고, 동시 활성 상한이 남았으면 연다.
        /// 사건이 열리면 그 괴담 글을 시작 믿음도로 게시판에 건다.
        /// </summary>
        private List<string> ActivateCases()
        {
            var opened = new List<string>();
            var w = W;
            if (w == null || _cases == null) return opened;

            int limit = _balance.MaxActiveCases(SolvedCount);
            foreach (var c in _cases.GetAllCases())
            {
                if (w.activeCaseIds.Count >= limit) break;
                if (c == null || !c.MainStory || !c.IsPlayable) continue;
                if (w.activeCaseIds.Contains(c.CaseId) || IsSolved(c.CaseId)) continue;
                // 이미 해결한 괴담을 다루는 사건은 열지 않는다. 다시 퍼지게 하는 재발 규칙은 아직 정하지 않았다.
                if (IsLegendResolved(c.LegendId)) { Debug.Log($"[Workday] {c.CaseId} 는 이미 해결한 괴담({c.LegendId})이라 열지 않는다"); continue; }
                if (Workday < c.AppearWorkday) continue;

                w.activeCaseIds.Add(c.CaseId);
                if (!w.postedCaseIds.Contains(c.CaseId)) w.postedCaseIds.Add(c.CaseId);
                AddCaseEntries(c);

                // 이 괴담이 처음 퍼지기 시작하는 확산도. 조사하며 소란을 피우면 여기서 더 오른다.
                var legend = _legends != null ? _legends.GetLegend(c.LegendId) : null;
                if (legend != null && _spread != null && _spread.GetSpreadRate(_save.Current, c.LegendId) <= 0f)
                    _spread.TrySetSpreadRate(_save.Current, c.LegendId, legend.InitialSpreadRate);
                opened.Add(c.CaseId);
                Debug.Log($"[Workday] 새 사건 | {c.CaseId} ({c.LegendId}) 시작 믿음도 {c.StartBelief}%");
            }
            return opened;
        }

        /// <summary>자연 확산을 받는 괴담. 활성 사건의 괴담뿐이다. 해결한 괴담은 들지 않는다.</summary>
        private List<string> SpreadingLegends()
        {
            var ids = new List<string>();
            if (_cases == null) return ids;
            foreach (var c in _cases.GetAllCases())
            {
                if (c == null || string.IsNullOrEmpty(c.LegendId) || ids.Contains(c.LegendId)) continue;
                bool active = W != null && W.activeCaseIds.Contains(c.CaseId);
                if (active && !IsLegendResolved(c.LegendId)) ids.Add(c.LegendId);
            }
            return ids;
        }

        // ------------------------------------------------------------- 게시판

        private const int CaseEntryViews = 140;

        /// <summary>사건의 괴담 글(LegendSO.WebPages)을 괴담 ID 를 붙여 게시판에 건다. 이미 있으면 건너뛴다.</summary>
        private void AddCaseEntries(CaseSO c)
        {
            if (_tutorial == null || _legends == null || c == null) return;
            var legend = _legends.GetLegend(c.LegendId);
            if (legend == null) return;

            int i = 0;
            foreach (var page in legend.WebPages)
            {
                if (page == null || HasEntry(page.TitleTextId)) continue;
                var entry = _tutorial.MakePageEntry(page.PageId, CaseEntryViews + i * 37, c.StartBelief,
                    -GameClock.ElapsedMinutes + 30 + i * 45, 6 + i * 3, 1 + i, "ui.net.author_anon", c.LegendId);
                if (entry == null) continue;
                _tutorial.AddBoardPost(entry);
                i++;
            }
        }

        private bool HasEntry(string titleTextId)
        {
            foreach (var e in _tutorial.BoardEntries)
                if (e != null && !e.IsPlayerPost && e.TitleTextId == titleTextId) return true;
            return false;
        }

        /// <summary>
        /// 게시판을 새로 지었다(불러온 뒤 처음 열 때 등). 본편이면 활성 사건의 괴담 글을 다시 걸고,
        /// 저장해 둔 글마다의 믿음도와 반응으로 덮는다. 튜토리얼에서는 아무것도 하지 않는다.
        /// </summary>
        private void OnBoardRebuilt()
        {
            if (!MainStarted) return;
            // 저장 전에 시간이 되어 올라와 있던 글을 다시 건다. 빠지면 전체 평균이 불러올 때마다 달라진다.
            var activity = FindFirstObjectByType<BoardActivityDirector>();
            int passed = activity != null ? activity.RestorePassedPosts(Mathf.Max(GameClock.ElapsedMinutes, W.elapsedMinutes)) : 0;
            if (passed > 0) Debug.Log($"[Workday] 지난 시각에 올라온 글 {passed}개를 다시 걸었다");

            // 글을 건 사건은 해결해서 활성에서 빠졌어도 글이 그대로 남는다.
            foreach (var id in PostedCaseIds())
            {
                var c = FindCase(id);
                if (c != null) AddCaseEntries(c);
            }

            var states = _save.Current.boardStates;
            int applied = 0, withComments = 0;
            foreach (var e in _tutorial.BoardEntries)
            {
                if (e == null || e.IsPlayerPost) continue;
                var s = states.Find(x => x != null && x.key == e.TitleTextId);
                if (s == null) continue;
                e.BeliefPercent = s.belief;
                e.Views = s.views;
                e.Likes = s.likes;
                e.Dislikes = s.dislikes;
                e.LikePressed = s.likePressed;
                e.DislikePressed = s.dislikePressed;
                e.GrowthCarry = s.growthCarry != null && s.growthCarry.Length == 4 ? (float[])s.growthCarry.Clone() : new float[4];
                e.GrowthAddedComments = s.growthAddedComments;
                if (s.comments != null && s.comments.Count > 0 && e.Comments != null)
                {
                    // 같은 목록을 고쳐 채운다. 인기글은 튜토리얼이 이 목록을 따로 쥐고 있다.
                    e.Comments.Clear();
                    foreach (var line in s.comments)
                    {
                        if (string.IsNullOrEmpty(line)) continue;
                        var parts = line.Split('|');
                        e.Comments.Add(new CommunityComment
                        {
                            AuthorTextId = parts[0],
                            BodyTextId = parts.Length > 1 ? parts[1] : string.Empty,
                            IsPlayer = parts.Length > 2 && parts[2] == "1",
                        });
                    }
                    withComments++;
                }
                applied++;
            }
            Debug.Log($"[Workday] 게시판 되살림 | 저장된 글 {applied}개 (댓글까지 {withComments}개)");
        }

        /// <summary>저장 직전. 시각과 게시판 글의 숫자, 댓글을 저장본에 옮긴다.</summary>
        public void SyncToSave()
        {
            if (!MainStarted || _tutorial == null) return;
            W.elapsedMinutes = GameClock.ElapsedMinutes;

            var states = _save.Current.boardStates;
            states.Clear();
            foreach (var e in _tutorial.BoardEntries)
            {
                if (e == null || e.IsPlayerPost || string.IsNullOrEmpty(e.TitleTextId)) continue;
                var s = new BoardEntryState
                {
                    key = e.TitleTextId, belief = e.BeliefPercent, views = e.Views, likes = e.Likes, dislikes = e.Dislikes,
                    likePressed = e.LikePressed, dislikePressed = e.DislikePressed,
                    growthCarry = e.GrowthCarry != null && e.GrowthCarry.Length == 4 ? (float[])e.GrowthCarry.Clone() : new float[4],
                    growthAddedComments = e.GrowthAddedComments,
                };
                if (e.Comments != null)
                    foreach (var c in e.Comments)
                        if (c != null) s.comments.Add(c.AuthorTextId + "|" + c.BodyTextId + "|" + (c.IsPlayer ? "1" : "0"));
                states.Add(s);
            }
        }

        /// <summary>괴담 글을 게시판에 건 사건. 활성 사건도 빠짐없이 든다.</summary>
        public List<string> PostedCaseIds()
        {
            var ids = new List<string>();
            var w = W;
            if (w == null) return ids;
            foreach (var id in w.postedCaseIds) if (!ids.Contains(id)) ids.Add(id);
            foreach (var id in w.activeCaseIds) if (!ids.Contains(id)) ids.Add(id);
            return ids;
        }

        /// <summary>지금 맡아 조사하는 사건. 현황판에서 고른다.</summary>
        public string CurrentCaseId
        {
            get => W != null ? W.currentCaseId : null;
            set
            {
                if (W == null) return;
                W.currentCaseId = value;
                _save.MarkDirty();
            }
        }

        public bool IsActive(string caseId) => W != null && W.activeCaseIds.Contains(caseId);

        // ------------------------------------------------------------- 믿음도

        /// <summary>괴담 믿음도(소수 포함). 게시판 글 평균에 1%에 못 미친 확산 몫을 더한 값이다. 글이 없으면 -1.</summary>
        public float LegendBelief(string legendId)
        {
            if (_tutorial == null) return -1f;
            float avg = _tutorial.GetLegendBeliefExact(legendId);
            return avg < 0f ? -1f : Mathf.Min(100f, avg + GetCarry(legendId));
        }

        public float OverallBelief => _tutorial != null ? _tutorial.BoardBeliefExact : 0f;

        private float GetCarry(string legendId)
        {
            var w = W;
            if (w == null) return 0f;
            foreach (var s in w.legendCarry)
            {
                int bar = s.IndexOf('|');
                if (bar > 0 && s.Substring(0, bar) == legendId && float.TryParse(s.Substring(bar + 1),
                        System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v)) return v;
            }
            return 0f;
        }

        private void SetCarry(string legendId, float value)
        {
            var w = W;
            w.legendCarry.RemoveAll(s => s.StartsWith(legendId + "|"));
            w.legendCarry.Add(legendId + "|" + value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>"괴담ID|%" 목록에서 하나를 읽는다. 없으면 -1.</summary>
        public static float ReadSnapshot(List<string> list, string key)
        {
            if (list == null) return -1f;
            foreach (var s in list)
            {
                int bar = s.IndexOf('|');
                if (bar > 0 && s.Substring(0, bar) == key && float.TryParse(s.Substring(bar + 1),
                        System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v)) return v;
            }
            return -1f;
        }

        private void TakeSnapshot(List<string> list)
        {
            list.Clear();
            foreach (var id in SpreadingLegends())
                list.Add(id + "|" + LegendBelief(id).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
            list.Add("*|" + OverallBelief.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 자연 확산을 지금 시각까지 반영한다. 반영한 곳(spreadAppliedMinute)부터 10분씩 끊어,
        /// 그때그때의 위험등급에 맞는 속도로 올린다. 도중에 등급이 바뀌면 그 다음 조각부터 새 속도다.
        /// 1%가 찰 때마다 그 괴담 글을 모두 1%씩 올린다(게시판 평균 규칙 그대로).
        /// 개별 괴담이나 전체가 100%에 닿으면 그 자리에서 멈추고 게임 오버를 알린다. 멈춘 시각을 돌려준다(-1 이면 끝까지 갔다).
        /// </summary>
        private int ApplySpreadUntil(int untilMinute, bool sleeping, DayReport report)
        {
            var w = W;
            if (w == null || w.gameOver) return -1;
            if (w.spreadAppliedMinute < 0) w.spreadAppliedMinute = untilMinute;

            var legends = SpreadingLegends();
            int cursor = w.spreadAppliedMinute;
            while (cursor < untilMinute)
            {
                int step = Mathf.Min(ElapsedChunk, untilMinute - cursor);
                float hours = step / 60f;
                foreach (var id in legends)
                {
                    float belief = LegendBelief(id);
                    if (belief < 0f) continue;

                    int grade = (int)LegendRiskLevelExtensions.FromBelief(belief);
                    float spreadRate = _spread != null ? _spread.GetSpreadRate(_save.Current, id) : 0f;
                    float rise = _balance.RisePerHour(grade, sleeping) * (1f + spreadRate / 100f * _balance.SpreadBoost) * hours;

                    float carry = GetCarry(id) + rise;
                    while (carry >= 1f)
                    {
                        _tutorial.ShiftLegendBelief(id, 1);
                        carry -= 1f;
                    }
                    SetCarry(id, carry);
                    CheckWarnings(id, LegendBelief(id), report);
                }
                cursor += step;
                w.spreadAppliedMinute = cursor;
                CheckWarnings("*", OverallBelief, report);

                string over = FindGameOver(legends);
                if (over != null)
                {
                    w.gameOver = true;
                    w.gameOverLegendId = over;
                    if (report != null) { report.GameOver = true; report.GameOverLegendId = over; }
                    Debug.Log($"[Workday] 게임 오버 | {over} 100% | {cursor - GameClock.ElapsedMinutes:+0;-0;0}분 지점");
                    _save.MarkDirty();
                    return cursor;
                }
            }
            _save.MarkDirty();
            return -1;
        }

        private string FindGameOver(List<string> legends)
        {
            float limit = _balance.GameOverBelief;
            foreach (var id in legends)
                if (LegendBelief(id) >= limit) return id;
            return OverallBelief >= limit ? "*" : null;
        }

        /// <summary>경고 단계를 처음 넘었으면 한 번만 적는다. 같은 단계는 다시 알리지 않는다.</summary>
        private void CheckWarnings(string key, float belief, DayReport report)
        {
            var w = W;
            foreach (int t in _balance.WarningThresholds)
            {
                if (belief < t) continue;
                string mark = key + "|" + t;
                if (w.warned.Contains(mark)) continue;
                w.warned.Add(mark);
                report?.Warnings.Add(mark);
                Debug.Log($"[Workday] 경고 | {key} {t}% 넘음");
            }
        }

        // ------------------------------------------------------------- 시간이 흐를 때

        /// <summary>
        /// 근무 중 행동으로 시간이 흘렀다(벽시계는 이미 움직였다). 그만큼 괴담이 퍼지고, 03:00 을 넘었으면 근무가 끝난다.
        /// </summary>
        public DayReport OnTimeAdvanced()
        {
            if (!MainStarted) return null;
            var report = new DayReport();
            int stop = ApplySpreadUntil(GameClock.ElapsedMinutes, false, report);
            if (stop >= 0)
            {
                // 행동하는 도중 100% 에 닿았다. 시계를 그 시각에 둔다.
                GameClock.Skip(stop - GameClock.ElapsedMinutes);
                W.elapsedMinutes = GameClock.ElapsedMinutes;
                RaiseGameOver();
                return report;
            }

            if (!OnDuty && !W.shiftEnded)
            {
                W.shiftEnded = true;
                _save.MarkDirty();
                Debug.Log($"[Workday] 근무 종료 | {CalendarDay}일째 {FormatClock24()}");
                ShiftEnded?.Invoke();
            }
            return report;
        }

        /// <summary>
        /// 잠든다. 다음 출근(18:00)까지 시간이 한 번에 흐르고, 그동안 잠든 속도로 괴담이 퍼진다.
        /// 18:00 이 되면 새 근무가 시작되고 등장할 사건이 열린다. 다음 날 보고서를 돌려준다.
        /// </summary>
        public DayReport Sleep()
        {
            var w = W;
            var report = new DayReport();
            if (w == null) return report;
            if (w.gameOver)
            {
                // 이미 끝났다. 시간을 더 흘리지 않는다.
                report.GameOver = true;
                report.GameOverLegendId = w.gameOverLegendId;
                return report;
            }

            // 근무 중에 흐른 몫이 남아 있으면 먼저 근무 속도로 반영한다.
            ApplySpreadUntil(GameClock.ElapsedMinutes, false, report);

            foreach (var id in SpreadingLegends())
                report.Changes.Add(new LegendChange { LegendId = id, CaseId = CaseOf(id), Before = LegendBelief(id) });
            report.OverallBefore = OverallBelief;

            int minutes = MinutesUntilNextShift;
            int from = GameClock.ElapsedMinutes;
            GameClock.Skip(minutes);
            report.MinutesSlept = minutes;

            int stop = w.gameOver ? from : ApplySpreadUntil(from + minutes, true, report);
            if (stop >= 0)
            {
                // 잠든 사이에 100% 에 닿았다. 시계를 그 시각에 둔다.
                GameClock.Skip(stop - GameClock.ElapsedMinutes);
                report.MinutesSlept = stop - from;
            }
            else
            {
                StartWorkday(report);
            }

            foreach (var c in report.Changes) c.After = LegendBelief(c.LegendId);
            report.OverallAfter = OverallBelief;

            w.elapsedMinutes = GameClock.ElapsedMinutes;
            _save.MarkDirty();
            Debug.Log($"[Workday] 잠들었다 {report.MinutesSlept}분 | {CalendarDay}일째 {FormatClock24()} 근무 {Workday}일차 | 전체 {report.OverallBefore:0.#}% -> {report.OverallAfter:0.#}%");

            if (report.GameOver) RaiseGameOver();
            return report;
        }

        /// <summary>18:00 새 근무. 지난 근무 시작 때의 믿음도를 남겨 두고(전날 대비), 새 사건을 연다.</summary>
        private void StartWorkday(DayReport report)
        {
            var w = W;
            w.shiftEnded = false;
            w.prevDayBeliefs.Clear();
            w.prevDayBeliefs.AddRange(w.dayStartBeliefs);

            foreach (var id in ActivateCases())
            {
                report.NewCaseIds.Add(id);
                var c = FindCase(id);
                if (c != null) report.Changes.Add(new LegendChange { CaseId = id, LegendId = c.LegendId, Before = -1f, IsNew = true });
            }
            TakeSnapshot(w.dayStartBeliefs);
            Debug.Log($"[Workday] 근무 {Workday}일차 시작 | 활성 사건 {w.activeCaseIds.Count}개");
        }

        private void RaiseGameOver()
        {
            GameOver?.Invoke(W.gameOverLegendId);
        }

        public CaseSO FindCase(string caseId) => _cases != null && _cases.TryGetCase(caseId, out var c) ? c : null;

        private string CaseOf(string legendId)
        {
            if (_cases == null) return null;
            foreach (var c in _cases.GetAllCases())
                if (c != null && c.LegendId == legendId && (W.activeCaseIds.Contains(c.CaseId) || IsSolved(c.CaseId))) return c.CaseId;
            return null;
        }

        /// <summary>현황판의 "전날 대비". 지난 근무 시작 때 믿음도에서 지금까지 바뀐 양. 비교할 값이 없으면 NaN.</summary>
        public float DeltaSincePrevDay(string legendId)
        {
            float prev = ReadSnapshot(W?.prevDayBeliefs, legendId);
            if (prev < 0f) return float.NaN;
            float now = legendId == "*" ? OverallBelief : LegendBelief(legendId);
            return now - prev;
        }
    }
}
