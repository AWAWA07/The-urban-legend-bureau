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

        /// <summary>
        /// 정정글이 성공했다. 보고서를 확정한 본편 사건이면 해결로 적는다. 같은 사건은 한 번만 센다.
        /// 해결한 사건은 활성에서 빠지고 자연 확산이 멈춘다(CaseSO.SpreadsAfterSolved 를 켠 사건만 계속 퍼진다).
        /// 게시판의 글과 올린 정정글은 그대로 남는다.
        /// </summary>
        private bool TryResolveMainCase(PostWritingSO data)
        {
            if (!InMain || data == null || data.IsTutorial) return false;

            string caseId = data.CaseId;
            if (_workday.IsSolved(caseId))
            {
                Debug.Log($"[CaseDirector] 본편 | {caseId} 는 이미 해결했다. 다시 세지 않는다");
                return true;
            }
            if (!CaseNotebook.IsReportConfirmed(_save.Current, caseId))
            {
                Debug.LogWarning($"[CaseDirector] 본편 | {caseId} 보고서 확정 없이 글이 성공했다. 해결로 적지 않는다");
                return true;
            }

            _workday.MarkSolved(caseId);
            CaseFlow.SetStep(_save, CaseStep.Completed);
            if (_workday.CurrentCaseId == caseId) _workday.CurrentCaseId = null;
            _save.AutoSave();

            var c = _workday.FindCase(caseId);
            if (_toastScreen != null)
            {
                _toastScreen.Closed = s => _ui.Close(s);
                if (!_ui.Contains(_toastScreen)) _ui.Push(_toastScreen);
                _toastScreen.Show(_loc.Get(MainSolvedTextId, CaseName(c)));
            }
            RefreshRoomDesktop();
            UpdateRoomComputerBeckon();
            Debug.Log($"[CaseDirector] 본편 | 사건 해결 | {caseId} | 해결 {_workday.SolvedCount}건");
            return true;
        }

        // ------------------------------------------------------------- 현황판

        /// <summary>현황판 오른쪽 칸의 진행 줄과 단추. 고른 사건이 활성이면 맡기·현장 이동·수사 노트를 세운다.</summary>
        private void AddCaseProgress(WorkdayScreen.View view, CaseSO focus)
        {
            var data = focus.Deduction;
            if (data != null)
            {
                view.Detail.Add(string.Empty);
                view.Detail.Add(L("ui.work.detail_progress",
                    CaseNotebook.AcquiredClues(_save.Current, data).Count, data.FieldClues.Count,
                    CaseNotebook.AnalysisCount(_save.Current, data), data.Analyses.Count,
                    CaseNotebook.SolvedCount(_save.Current, data), data.Questions.Count));
                bool filed = CaseNotebook.IsReportConfirmed(_save.Current, focus.CaseId);
                bool posted = focus.PostWriting != null && _postWriting != null && _postWriting.IsFinished(focus.PostWriting);
                view.Detail.Add(L("ui.work.detail_report", L(filed ? "ui.work.yes" : "ui.work.no"), L(posted ? "ui.work.yes" : "ui.work.no")));
            }

            if (_workday.IsSolved(focus.CaseId))
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
