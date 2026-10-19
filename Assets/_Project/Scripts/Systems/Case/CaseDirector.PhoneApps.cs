using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 휴대폰 앱과 본편 숙소의 물건.
    ///   휴대폰 앱      컴퓨터 바탕화면에서 열리는 앱은 휴대폰에서도 연다(괴담넷, 메모장, 수사 노트, 업무 현황, 기록).
    ///   앱 열림 알림    새로 쓸 수 있게 된 앱은 한 번 위쪽 알림으로 알린다. 알린 것은 storyFlags 에 남긴다.
    ///   본편 숙소      자고 일어난 뒤의 숙소는 튜토리얼 문장을 쓰지 않는다. 방문은 어디로 향할지 묻는다.
    /// </summary>
    public partial class CaseDirector
    {
        /// <summary>앱을 알렸다는 표시. 뒤에 앱 ID 를 붙인다. 예: app_unlocked:workboard</summary>
        private const string AppUnlockedFlagPrefix = "app_unlocked:";

        private const string AppUnlockedTextId = "ui.app.unlocked";
        private const string AppListSeparatorTextId = "ui.app.list_sep";

        private const string DoorAskTextId = "field.room.door.ask";
        private const string DoorGoTextId = "field.room.door.go";
        private const string DoorStayTextId = "field.room.door.stay";
        private const string DoorOffDutyTextId = "field.room.door.off_duty";
        private const string DoorNoCaseTextId = "field.room.door.no_case";
        private const string BookshelfMainTextId = "field.room.bookshelf.result.main";
        private const string ClockMainTextId = "field.room.clock.result.main";
        private const string ClockMainOffTextId = "field.room.clock.result.main_off";

        private const string RoomDoorPointId = "point_room_door";
        private const string RoomBookshelfPointId = "point_room_bookshelf";

        // ------------------------------------------------------------- 휴대폰 앱

        /// <summary>
        /// 휴대폰에 보일 앱. 컴퓨터 바탕화면과 같은 때에 열린다.
        /// 괴담넷은 늘, 메모장은 한영이 메모하라고 이른 뒤부터, 수사 노트는 숙소에 도착한 뒤부터, 업무 현황과 기록은 본편부터.
        /// </summary>
        private bool IsPhoneAppVisible(string appId)
        {
            switch (appId)
            {
                case NetAppId: return true;
                case MemoAppId: return MemoScreen.IsUnlocked;
                case CaseFileAppId: return InMain || HasFlag(AppUnlockedFlagPrefix + CaseFileAppId) || (_inRoom && Deduction != null);
                case WorkboardAppId: return InMain;
                case SaveAppId: return InMain;
                default: return false;
            }
        }

        /// <summary>휴대폰 앱을 눌렀을 때. 컴퓨터의 같은 앱과 같은 화면을 연다.</summary>
        private void HandlePhoneApp(string appId)
        {
            if (!IsPhoneAppVisible(appId)) return;

            switch (appId)
            {
                case MemoAppId:
                    // 휴대폰에 이미 켜 둔 메모장을 한 번 더 누르면 화면 가운데에 크게 띄운다. 다시 누르면 돌아온다.
                    if (_memoScreen != null && _ui.Contains(_memoScreen) && _memoScreen.IsPhone) _memoScreen.ToggleEnlarged();
                    else OpenPhoneMemo();
                    return;

                case NetAppId:
                    OpenPhoneNet();
                    return;

                // 나머지는 화면 전체를 쓰는 앱이다. 휴대폰은 넣고 연다. 닫으면 지금 있던 자리로 돌아온다.
                case CaseFileAppId:
                    PutPhoneAway();
                    OpenCaseArchive();
                    return;

                case WorkboardAppId:
                    PutPhoneAway();
                    ShowWorkBoard();
                    return;

                case SaveAppId:
                    PutPhoneAway();
                    OpenSlots(fromTitle: false, saveMode: true);   // 숙소 밖에서는 불러오기만 된다
                    return;
            }
        }

        private void PutPhoneAway()
        {
            if (_fieldHudScreen != null) _fieldHudScreen.SetPhoneOpen(false);
        }

        // ------------------------------------------------------------- 앱 열림 알림

        /// <summary>
        /// 새로 열린 앱을 알린다. 메모장과 수사 노트는 제 때에 따로 알리므로, 여기서는 본편에서 열리는 앱만 본다.
        /// 다른 화면(야간 보고, 업무 현황 등)이 위에 떠 있으면 미뤘다가 그 화면을 닫은 뒤에 알린다.
        /// </summary>
        private void AnnounceUnlockedApps()
        {
            if (!InMain || _save == null || _save.Current == null) return;
            if (_workdayScreen != null && _ui.Contains(_workdayScreen)) return;
            if (_ui.Current != _fieldHudScreen) return;

            var names = new List<string>();
            foreach (var app in new[] { WorkboardAppId, SaveAppId })
            {
                if (!IsPhoneAppVisible(app) || HasFlag(AppUnlockedFlagPrefix + app)) continue;
                _save.Current.storyFlags.Add(AppUnlockedFlagPrefix + app);
                names.Add(_loc.Get(app == WorkboardAppId ? "ui.desktop.app_workboard" : "ui.desktop.app_saveslot"));
            }
            if (names.Count == 0) return;

            _save.MarkDirty();
            ShowMainToast(_loc.Get(AppUnlockedTextId, string.Join(_loc.Get(AppListSeparatorTextId), names)));
            Debug.Log($"[CaseDirector] 앱이 열렸다 | {string.Join(", ", names)}");
        }

        private System.Collections.IEnumerator AnnounceUnlockedAppsSoon()
        {
            yield return new WaitForSecondsRealtime(0.6f);
            AnnounceUnlockedApps();
        }

        private bool HasFlag(string flag) => _save != null && _save.Current != null && _save.Current.storyFlags.Contains(flag);

        private void MarkAppAnnounced(string appId)
        {
            if (_save == null || _save.Current == null || HasFlag(AppUnlockedFlagPrefix + appId)) return;
            _save.Current.storyFlags.Add(AppUnlockedFlagPrefix + appId);
            _save.MarkDirty();
        }

        // ------------------------------------------------------------- 본편 숙소의 물건

        /// <summary>
        /// 본편 숙소에서 물건을 눌렀다. 튜토리얼 첫날의 문장 대신 본편 문장을 쓰고, 방문은 어디로 향할지 묻는다.
        /// 처리했으면 true. 컴퓨터와 스위치처럼 튜토리얼과 같게 움직이는 것은 false 를 돌려 원래 흐름에 맡긴다.
        /// </summary>
        private bool TryHandleMainRoomObject(InvestigationPoint point)
        {
            if (!InMain || point == null) return false;

            switch (point.PointId)
            {
                case RoomDoorPointId:
                    AskWhereToGo();
                    return true;

                case RoomBookshelfPointId:
                    RoomRemark(() => _loc.Get(BookshelfMainTextId));
                    return true;

                case RoomClockPointId:
                    RoomRemark(() => _workday.OnDuty
                        ? _loc.Get(ClockMainTextId, WorkdayDirector.FormatClock24(), Hours(_workday.MinutesUntilShiftEnd))
                        : _loc.Get(ClockMainOffTextId, WorkdayDirector.FormatClock24()));
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 방문. "어디로 향할까?" 를 묻고 맡을 수 있는 사건의 현장을 늘어놓는다. 고르면 그 사건을 맡고 현장으로 간다.
        /// 근무 시간이 아니거나 맡을 사건이 없으면 한마디만 한다.
        /// </summary>
        private void AskWhereToGo()
        {
            if (!_workday.OnDuty || _save.Current.work.gameOver)
            {
                RoomRemark(() => _loc.Get(DoorOffDutyTextId));
                return;
            }

            var cases = _workday.ActiveCases();
            if (cases.Count == 0)
            {
                RoomRemark(() => _loc.Get(DoorNoCaseTextId));
                return;
            }

            var picks = new List<CaseSO>(cases);
            var labels = new List<System.Func<string>>();
            foreach (var c in picks)
            {
                var captured = c;
                labels.Add(() => _loc.Get(DoorGoTextId, CaseName(captured)));
            }
            labels.Add(() => _loc.Get(DoorStayTextId));

            _fieldHudScreen.ShowLine(FieldSpeakerTextId, () => _loc.Get(DoorAskTextId), () =>
                _fieldHudScreen.ShowChoices(FieldSpeakerTextId, labels, index =>
                {
                    _fieldHudScreen.ClearSpeech();
                    if (index < 0 || index >= picks.Count) return;
                    if (!HoldMainCase(picks[index].CaseId)) return;
                    GoToMainField();
                }));
        }
    }
}
