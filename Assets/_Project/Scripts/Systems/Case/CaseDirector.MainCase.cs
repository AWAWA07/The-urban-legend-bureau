using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 본편 사건 진행. 현황판에서 사건을 맡고, 괴담넷에서 글을 읽고, 현장에 다녀오고, 숙소 컴퓨터에서
    /// 수사 노트(분석 → 추론 → 보고서)와 정정글을 마치면 사건이 해결된다.
    ///
    /// 흐름 자체는 튜토리얼과 같은 화면과 서비스를 그대로 쓴다(수사 노트, 보고서, 글쓰기, 조사 행동).
    /// 여기서는 본편에서만 다른 것만 잇는다.
    ///   맡은 사건      _case 하나. 바꿔 맡아도 단서·분석·추론·보고서·올린 글은 저장본에 사건별로 남는다.
    ///   현장 오가기    숙소 ↔ 현장, 편도 MainTravelMinutes 분. 막차처럼 정해진 도착 시각은 없다.
    ///   해결          보고서를 확정하고 정정글이 성공하면 해결이다. 같은 사건은 한 번만 센다.
    /// 튜토리얼에서는(InMain 이 아닐 때) 아무것도 하지 않는다.
    /// </summary>
    public partial class CaseDirector
    {
        /// <summary>숙소와 본편 현장 사이 편도 이동 시간(분).</summary>
        private const int MainTravelMinutes = 40;

        private const string MainFieldDurationTextId = "ui.travel.duration_main_field";
        private const string MainRoomDurationTextId = "ui.travel.duration_main_room";

        /// <summary>현장으로 가거나 숙소로 돌아오는 중이다. 그동안 근무 종료·게임 오버 화면은 도착한 뒤에 띄운다.</summary>
        private bool _mainTravelling;
        private bool _shiftEndPending;
        private string _gameOverPending;

        /// <summary>지금 맡은 사건이 본편 활성 사건인가.</summary>
        private bool HoldingMainCase => InMain && _case != null && _workday.IsActive(_case.CaseId);

        // ------------------------------------------------------------- 사건 맡기

        /// <summary>
        /// 사건을 손에 든다. 활성 사건이면 지금 맡은 사건으로 적고, 해결한 사건이면 기록을 읽기만 한다.
        /// 숙소에 있던 상태(방, 밤)는 그대로 둔다. 사건마다 모은 것은 저장본에 사건별로 있어 바꿔 들어도 잃지 않는다.
        /// </summary>
        private bool HoldMainCase(string caseId)
        {
            if (!InMain || _cases == null || !_cases.TryGetCase(caseId, out var c) || c == null) return false;

            if (_case != c)
            {
                bool inRoom = _inRoom;
                var night = _night;
                if (!SelectCase(c)) return false;
                _inRoom = inRoom;
                _night = night;
            }

            if (_workday.IsActive(caseId))
            {
                CaseFlow.StartCase(_save, caseId, c.LegendId);
                // 사건 단계는 저장본에 하나뿐이다. 바꿔 맡았다 돌아오면 지금까지 한 것으로 다시 세운다.
                if (AnyCasePageRead(c)) CaseFlow.SetStep(_save, CaseStep.InternetResearch);
                if (Deduction != null && CaseNotebook.AcquiredClues(_save.Current, Deduction).Count > 0)
                    CaseFlow.SetStep(_save, CaseStep.ClueAcquired);
                _workday.CurrentCaseId = caseId;
            }

            Debug.Log($"[CaseDirector] 본편 사건을 맡음 | {caseId} | 활성={_workday.IsActive(caseId)} 해결={_workday.IsSolved(caseId)}");
            return true;
        }

        private bool AnyCasePageRead(CaseSO c)
        {
            var legend = _legends != null ? _legends.GetLegend(c.LegendId) : null;
            if (legend == null || _internet == null) return false;
            foreach (var page in legend.WebPages)
                if (page != null && _internet.IsPageViewed(_save.Current, page.PageId)) return true;
            return false;
        }

        /// <summary>저장본에 적힌 맡은 사건을 다시 손에 든다. 없거나 이미 해결했으면 첫 활성 사건을 든다.</summary>
        private void HoldSavedMainCase()
        {
            string id = _workday.CurrentCaseId;
            if (string.IsNullOrEmpty(id) || !_workday.IsActive(id))
            {
                var active = _workday.ActiveCases();
                id = active.Count > 0 ? active[0].CaseId : null;
            }
            if (!string.IsNullOrEmpty(id)) HoldMainCase(id);
        }

        // ------------------------------------------------------------- 인터넷 조사

        /// <summary>
        /// 괴담넷에서 글을 열었다. 본편 사건의 괴담 글이면 읽은 것으로 적는다. 읽는 데는 시간이 들지 않는다.
        /// 지금 맡은 사건의 글이면 인터넷 조사를 한 것으로 친다(현장의 몇몇 조사는 이것이 먼저다).
        /// </summary>
        private void OnMainBoardEntryOpened(CommunityBoardEntry entry)
        {
            if (!InMain || entry == null || entry.Page == null || _internet == null) return;
            if (_internet.TryMarkPageViewed(_save, entry.Page.PageId))
                Debug.Log($"[CaseDirector] 본편 | 괴담 글을 읽음 | {entry.Page.PageId} ({entry.LegendId})");
            if (HoldingMainCase && entry.LegendId == _legendId) CaseFlow.SetStep(_save, CaseStep.InternetResearch);
        }

        // ------------------------------------------------------------- 현장 오가기

        /// <summary>현황판의 "현장으로 이동". 맡은 사건의 현장으로 간다.</summary>
        private void GoToMainField()
        {
            if (!HoldingMainCase || _workday.MainStarted && _save.Current.work.gameOver) return;

            CloseWorkScreen();
            if (_desktopScreen != null && _ui.Contains(_desktopScreen)) _ui.Close(_desktopScreen);
            GamePointer.SetVisible(false);

            _mainTravelling = true;
            PlayTravel(TravelToFieldTextId, MainFieldDurationTextId, MainTravelMinutes, FieldTravelSeconds,
                () => { if (!_save.Current.work.gameOver) EnterMainField(); },
                AfterMainTravel);
            Debug.Log($"[CaseDirector] 본편 | 현장으로 이동 | {_caseId} | {MainTravelMinutes}분");
        }

        private void EnterMainField()
        {
            _inRoom = false;
            _night = NightState.None;
            _ui.CloseAll();

            CaseFlow.SetStep(_save, CaseStep.FieldInvestigation);
            _activePoint = null;
            ShowFieldHud();

            // 현장에는 들어설 때마다 입구에서 시작한다.
            if (_field != null && _field.ActiveRoot != null)
            {
                foreach (var walker in _field.ActiveRoot.GetComponentsInChildren<FieldWalker>(true)) walker.ResetToStart();
                foreach (var follower in _field.ActiveRoot.GetComponentsInChildren<FieldFollower>(true)) follower.ResetToStart();
            }
            Debug.Log($"[CaseDirector] 본편 | 현장 도착 | {_caseId} | {WorkdayDirector.FormatClock24()}");
        }

        /// <summary>현장의 "조사 종료". 본편에서는 숙소로 돌아간다.</summary>
        private void ReturnFromMainField()
        {
            if (_inRoom) return;
            _ui.CloseAll();
            _mainTravelling = true;
            PlayTravel(TravelToRoomTextId, MainRoomDurationTextId, MainTravelMinutes, RoomTravelSeconds,
                () =>
                {
                    EnterRoom(withIntro: false);
                    var room = GetRoomNight();
                    if (room != null) room.SetHanyoungPresent(false);
                },
                () => { StartRoomIntro(); AfterMainTravel(); });
            Debug.Log($"[CaseDirector] 본편 | 숙소로 돌아감 | {MainTravelMinutes}분");
        }

        /// <summary>오가는 사이에 근무가 끝났거나 게임 오버가 났으면 도착한 뒤에 알린다.</summary>
        private void AfterMainTravel()
        {
            _mainTravelling = false;
            if (_gameOverPending != null)
            {
                string id = _gameOverPending;
                _gameOverPending = null;
                _shiftEndPending = false;
                OnWorkGameOver(id);
                return;
            }
            if (_shiftEndPending)
            {
                _shiftEndPending = false;
                OnWorkShiftEnded();
            }
        }

        // ------------------------------------------------------------- 보고서와 해결

        private const string MainReportDoneTextId = "ui.work.toast_report_done";
        private const string MainSolvedTextId = "ui.work.toast_solved";
        private const string MainAboveTargetTextId = "ui.work.toast_above_target";

        /// <summary>글쓰기를 본편 규칙에 잇는다. Start 에서 한 번 부른다.</summary>
        private void HookMainPostWriting()
        {
            if (_postWriting == null) return;
            // 본편 글은 해결 판정과 같은 믿음도(소수 첫째 자리)로 보여 주고 평가한다. 튜토리얼 글은 예전 그대로다.
            _postWriting.BeliefOf = id => InMain ? _workday.DisplayBelief(id) : _tutorial != null ? _tutorial.GetLegendBelief(id) : 0f;
            _postWriting.OverallOf = () => InMain ? _workday.DisplayOverall : _tutorial != null ? _tutorial.BoardBelief : 0f;
            // 정정글이 성공해도 사건을 정식으로 해결하기 전까지는 더 쓸 수 있다.
            _postWriting.KeepOpen = d => InMain && d != null && !d.IsTutorial && !_workday.IsSolved(d.CaseId);
            _postWriting.CaseGoalLine = d =>
            {
                var c = d != null ? _workday.FindCase(d.CaseId) : null;
                return c == null ? null : L("ui.post.case_goal", _workday.ResolveTarget(c), Pct(_workday.DisplayBelief(c.LegendId)));
            };
            _postWriting.Published += OnPostPublished;
            _postWriting.ResultConfirmed += OnPostResultConfirmed;
        }

        /// <summary>글을 실제로 올렸다(확인창에서 올리기를 고른 순간 한 번). 본편이면 그만큼 시간이 흐르고 괴담이 퍼진다.</summary>
        private void OnPostPublished(PostWritingSO data)
        {
            if (!InMain || data == null || data.IsTutorial) return;
            AdvanceTime(_workday.Balance.PostMinutes);
            Debug.Log($"[CaseDirector] 본편 | 글을 올렸다 | {data.PostWritingId} | {_workday.Balance.PostMinutes}분 | {WorkdayDirector.FormatClock24()}");
        }

        /// <summary>글의 평가를 읽었다(성공이든 아니든). 본편이면 사건을 정식으로 해결할 수 있는지 본다.</summary>
        private void OnPostResultConfirmed(PostWritingSO data)
        {
            if (!InMain || data == null || data.IsTutorial) return;
            TryResolveMainCase(data);
        }

        /// <summary>
        /// 사건을 정식으로 해결할 수 있으면 해결로 적는다(WorkdayDirector.TryResolve, 조건은 Evaluate).
        /// 정정글이 성공했어도 괴담 믿음도가 목표보다 높으면 사건은 계속된다. 보고서·올린 글·오른 믿음도는 그대로이고 새 정정글을 쓸 수 있다.
        /// 해결하면 활성에서 빠지고, 그 괴담에 묶인 글은 모두 믿음도가 더 오르지 않는다. 같은 사건은 한 번만 센다.
        /// </summary>
        private bool TryResolveMainCase(PostWritingSO data)
        {
            if (!InMain || data == null || data.IsTutorial) return false;

            string caseId = data.CaseId;
            var c = _workday.FindCase(caseId);
            if (_workday.IsSolved(caseId))
            {
                Debug.Log($"[CaseDirector] 본편 | {caseId} 는 이미 해결했다. 다시 세지 않는다");
                return true;
            }

            if (!_workday.TryResolve(caseId))
            {
                var status = _workday.Evaluate(c);
                if (status == WorkdayDirector.CaseStatus.AboveTarget)
                    ShowMainToast(_loc.Get(MainAboveTargetTextId, Pct(_workday.DisplayBelief(c.LegendId)), _workday.ResolveTarget(c)));
                // 아직 끝나지 않았다. 글쓰기 단추를 다시 세운다.
                if (_communityScreen != null && _ui.Contains(_communityScreen)) BindWriteButton();
                RefreshRoomDesktop();
                return false;
            }

            CaseFlow.SetStep(_save, CaseStep.Completed);
            if (_workday.CurrentCaseId == caseId) _workday.CurrentCaseId = null;
            if (_postWriting != null) _postWriting.Close();
            _save.AutoSave();

            ShowMainToast(_loc.Get(MainSolvedTextId, CaseName(c)));
            RefreshRoomDesktop();
            UpdateRoomComputerBeckon();
            Debug.Log($"[CaseDirector] 본편 | 사건 해결 | {caseId} | 해결 {_workday.SolvedCount}건");
            return true;
        }

        private void ShowMainToast(string text)
        {
            if (_toastScreen == null) return;
            _toastScreen.Closed = s => _ui.Close(s);
            if (!_ui.Contains(_toastScreen)) _ui.Push(_toastScreen);
            _toastScreen.Show(text);
        }

        /// <summary>현황판과 기록부에 적는 사건 상태 한마디.</summary>
        private string StatusText(WorkdayDirector.CaseStatus status) => L("ui.work.status_" + status.ToString().ToLowerInvariant());

        // ------------------------------------------------------------- 현황판

        /// <summary>현황판 오른쪽 칸의 진행 줄과 단추. 고른 사건이 활성이면 맡기·현장 이동을 세운다.</summary>
        private void AddCaseProgress(WorkdayScreen.View view, CaseSO focus)
        {
            var status = _workday.Evaluate(focus);
            int target = _workday.ResolveTarget(focus);
            view.Detail.Add(L(_workday.HasSpecialTarget(focus) ? "ui.work.detail_target_special" : "ui.work.detail_target", target));

            var data = focus.Deduction;
            if (data != null)
            {
                view.Detail.Add(L("ui.work.detail_progress",
                    CaseNotebook.AcquiredClues(_save.Current, data).Count, data.FieldClues.Count,
                    CaseNotebook.AnalysisCount(_save.Current, data), data.Analyses.Count,
                    CaseNotebook.SolvedCount(_save.Current, data), data.Questions.Count));
            }
            bool filed = CaseNotebook.IsReportConfirmed(_save.Current, focus.CaseId);
            bool posted = _workday.HasCorrection(focus);
            bool solved = status == WorkdayDirector.CaseStatus.Resolved;
            // 정정글 작성 완료와 사건 해결 완료는 다르다. 따로 적는다.
            view.Detail.Add(L("ui.work.detail_report", L(filed ? "ui.work.yes" : "ui.work.no"), L(posted ? "ui.work.yes" : "ui.work.no"), L(solved ? "ui.work.yes" : "ui.work.no")));
            view.Detail.Add(L("ui.work.detail_status", StatusText(status)));

            if (solved)
            {
                view.Detail.Add(L("ui.work.detail_solved"));
                return;
            }

            bool holding = _case == focus;
            bool inRoom = _inRoom;
            view.Actions.Add(new WorkdayScreen.ActionButton
            {
                Label = holding ? L("ui.work.btn_holding") : L("ui.work.btn_take"),
                Enabled = !holding,
                OnClick = () => { HoldMainCase(focus.CaseId); ShowWorkBoard(); },
            });
            view.Actions.Add(new WorkdayScreen.ActionButton
            {
                Label = L("ui.work.btn_go_field", MainTravelMinutes),
                Enabled = inRoom && !_save.Current.work.gameOver,
                OnClick = () => { if (HoldMainCase(focus.CaseId)) GoToMainField(); },
            });
        }
    }
}
