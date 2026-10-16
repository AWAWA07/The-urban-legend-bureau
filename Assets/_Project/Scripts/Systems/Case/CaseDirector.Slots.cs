using System;
using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.Save;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 수동 저장 칸(SAVE 01 ~ SAVE 20). RPG 쯔꾸르식 A안이다.
    ///
    ///   저장       숙소 컴퓨터의 「기록」 앱. 본편에서만 연다. 빈 칸은 바로, 쓰던 칸은 덮어쓰기 확인 뒤에 저장한다.
    ///   불러오기   같은 앱, 또는 타이틀의 「불러오기」. 빈 칸은 누를 수 없다.
    ///   이어하기   타이틀의 「이어하기」. 가장 최근에 수동 저장한 칸을 불러온다.
    ///   독립       칸마다 저장 파일이 따로다. 불러오면 그 칸을 저장한 시점의 사건·게시물·댓글·믿음도·시간·수사 기록으로 돌아간다.
    ///   자동 저장  slot_0 은 지금 판의 숨겨진 임시 기록이다. 칸을 덮어쓰지 않는다. 칸을 불러오면 그 진행이 지금 판이 된다.
    /// 튜토리얼 도중에는 저장하지 않는다(튜토리얼은 이어 하는 저장을 쓰지 않는다).
    /// </summary>
    public partial class CaseDirector
    {
        [Tooltip("수동 저장 칸 화면.")]
        [SerializeField] private SaveSlotScreen _saveSlotScreen;

        private const string SaveAppId = "saveslot";

        /// <summary>타이틀에서 열었는가. 타이틀에서는 불러오기만 한다.</summary>
        private bool _slotsFromTitle;
        private bool _slotsSaveMode;
        private string _slotsNotice;

        // ------------------------------------------------------------- 열기

        /// <summary>타이틀의 「불러오기」.</summary>
        public void OnOpenLoadClicked()
        {
            OpenSlots(fromTitle: true, saveMode: false);
        }

        /// <summary>
        /// 타이틀의 「이어하기」. 가장 최근에 정상적으로 수동 저장한 칸을 불러온다.
        /// 수동 저장이 하나도 없으면 처음 하는 것이면 새 게임을 시작하고, 아니면 알리기만 한다(자동 저장을 섞지 않는다).
        /// </summary>
        public void OnContinueClicked()
        {
            int latest = LatestSlot(out _);
            if (latest > 0)
            {
                LoadSlot(latest);
                return;
            }
            if (_tutorial != null && !TutorialDirector.HasSeenTutorial())
            {
                OnStartClicked();
                return;
            }
            ShowMainToast(_loc.Get("ui.slot.no_save"));
        }

        private void OpenSlots(bool fromTitle, bool saveMode)
        {
            if (_saveSlotScreen == null) { Debug.LogError("[CaseDirector] 저장 칸 화면이 연결되지 않았다. 씬을 다시 빌드할 것."); return; }
            _slotsFromTitle = fromTitle;
            _slotsSaveMode = saveMode && !fromTitle && CanSaveNow();
            _slotsNotice = null;
            RefreshSlots();
            if (!_ui.Contains(_saveSlotScreen)) _ui.Push(_saveSlotScreen);
            GamePointer.SetVisible(true);
        }

        private void CloseSlots()
        {
            if (_saveSlotScreen != null && _ui.Contains(_saveSlotScreen)) _ui.Close(_saveSlotScreen);
        }

        /// <summary>지금 수동 저장을 해도 되는가. 본편에서, 숙소에 있고, 이동 중이 아니고, 게임 오버가 아닐 때.</summary>
        private bool CanSaveNow() => InMain && _inRoom && !_mainTravelling && !_save.Current.work.gameOver;

        private void RefreshSlots()
        {
            int latest = LatestSlot(out _);
            var view = new SaveSlotScreen.View
            {
                Title = L(_slotsSaveMode ? "ui.slot.title_save" : "ui.slot.title_load"),
                Sub = L(_slotsSaveMode ? "ui.slot.sub_save" : "ui.slot.sub_load"),
                SaveMode = _slotsSaveMode,
                AllowSave = !_slotsFromTitle && CanSaveNow(),
                SaveTabLabel = L("ui.slot.tab_save"),
                LoadTabLabel = L("ui.slot.tab_load"),
                EmptyLabel = L("ui.slot.empty"),
                LatestLabel = L("ui.slot.latest"),
                CloseLabel = L("ui.slot.close"),
                Notice = _slotsNotice,
                OnTab = save => { _slotsSaveMode = save; _slotsNotice = null; RefreshSlots(); },
                OnPick = OnSlotPicked,
                OnClose = CloseSlots,
            };

            for (int i = 1; i <= SaveService.ManualSlotCount; i++)
            {
                var data = _save.SlotExists(i) ? _save.ReadSlot(i) : null;
                var sv = new SaveSlotScreen.SlotView { Number = L("ui.slot.number", i.ToString("00")), Empty = data == null, Latest = i == latest };
                if (data != null)
                {
                    var m = data.meta ?? new SaveSlotMeta();
                    sv.Line1 = L("ui.slot.line1", m.dateText, Clock(m.minuteOfDay), m.workday, PlayTime(m.playSeconds));
                    string caseName = string.IsNullOrEmpty(m.caseNameTextId) ? L("ui.slot.no_case") : _loc.Get(m.caseNameTextId);
                    sv.Line2 = L("ui.slot.line2", m.placeText, caseName, m.solvedCount, m.savedAtLocal);
                }
                view.Slots.Add(sv);
            }
            _saveSlotScreen.Show(view);
        }

        private static string Clock(int minuteOfDay) => (minuteOfDay / 60).ToString("00") + ":" + (minuteOfDay % 60).ToString("00");

        private static string PlayTime(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (s / 3600) + ":" + (s / 60 % 60).ToString("00") + ":" + (s % 60).ToString("00");
        }

        /// <summary>가장 최근에 수동 저장한 칸(저장한 실제 시각이 가장 늦은 칸). 없으면 0.</summary>
        private int LatestSlot(out long ticks)
        {
            ticks = 0;
            int best = 0;
            for (int i = 1; i <= SaveService.ManualSlotCount; i++)
            {
                if (!_save.SlotExists(i)) continue;
                var data = _save.ReadSlot(i);
                long t = data != null && data.meta != null ? data.meta.savedAtUtcTicks : 0;
                if (data != null && t >= ticks) { ticks = t; best = i; }
            }
            return best;
        }

        // ------------------------------------------------------------- 고르기

        private void OnSlotPicked(int slot)
        {
            bool used = _save.SlotExists(slot);
            if (_slotsSaveMode)
            {
                if (!CanSaveNow()) return;
                if (!used) { SaveSlot(slot); return; }
                ConfirmDialog.Show(_saveSlotScreen.Root, _saveSlotScreen.RegularFont, _saveSlotScreen.BoldFont,
                    L("ui.slot.confirm_overwrite", slot.ToString("00")), L("ui.slot.overwrite"), L("ui.slot.cancel"),
                    () => SaveSlot(slot), null);
                return;
            }

            if (!used) return;   // 빈 칸은 불러올 수 없다
            if (_slotsFromTitle) { LoadSlot(slot); return; }
            ConfirmDialog.Show(_saveSlotScreen.Root, _saveSlotScreen.RegularFont, _saveSlotScreen.BoldFont,
                L("ui.slot.confirm_load", slot.ToString("00")), L("ui.slot.load"), L("ui.slot.cancel"),
                () => LoadSlot(slot), null);
        }

        // ------------------------------------------------------------- 저장

        private void SaveSlot(int slot)
        {
            if (!CanSaveNow()) return;
            FillSlotMeta();
            bool ok = _save.SaveToSlot(slot);
            _slotsNotice = L(ok ? "ui.slot.saved" : "ui.slot.save_failed", slot.ToString("00"));
            RefreshSlots();
            Debug.Log($"[CaseDirector] 수동 저장 | SAVE {slot:00} | {(ok ? "성공" : "실패")} | {WorkdayDirector.FormatClock24()} 근무 {_workday.Workday}일차");
        }

        /// <summary>저장 칸에 보일 요약을 지금 진행에 적는다.</summary>
        private void FillSlotMeta()
        {
            var data = _save.Current;
            data.meta ??= new SaveSlotMeta();
            var m = data.meta;
            m.savedAtUtcTicks = DateTime.UtcNow.Ticks;
            m.savedAtLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            m.calendarDay = WorkdayDirector.CalendarDay;
            m.dateText = _workday.FormatDate(_loc);
            m.workday = _workday.Workday;
            m.minuteOfDay = WorkdayDirector.MinuteOfDay;
            m.placeText = _inRoom ? L(RoomPlaceTextId) : L("ui.slot.place_field", _case != null ? _loc.Get(_case.CaseNameTextId) : "-");
            m.caseNameTextId = HoldingMainCase ? _case.CaseNameTextId : null;
            m.solvedCount = _workday.SolvedCount;
            m.playSeconds = data.playSeconds;
        }

        // ------------------------------------------------------------- 불러오기

        /// <summary>
        /// 칸을 불러온다. 그 칸의 파일을 새로 읽어 지금 진행으로 삼고, 화면과 게시판·시계를 그 진행으로 다시 세운다.
        /// 씬을 다시 읽지 않는다. 지금 걸린 글과 화면 상태는 모두 버리고 저장본에서 다시 짓는다.
        /// </summary>
        private void LoadSlot(int slot)
        {
            var data = _save.ReadSlot(slot);
            if (data == null) { _slotsNotice = L("ui.slot.load_failed", slot.ToString("00")); RefreshSlots(); return; }

            StopAllCoroutines();
            _mainTravelling = false;
            _shiftEndPending = false;
            _gameOverPending = null;
            _ui.CloseAll();
            if (_travelScreen != null) _ui.HideDetached(_travelScreen);
            FieldHudScreen.IsCutscene = false;

            _save.Adopt(data);
            if (_postWriting != null) _postWriting.ResetProgress();

            // 시계와 본편 상태. 본편 저장본만 칸에 들어간다.
            _workday.RestoreFromSave();
            // 게시판을 이 저장본으로 다시 짓는다(글 상태, 질문 글, 플레이어 글, 댓글).
            if (_tutorial != null) _tutorial.RebuildBoardFromSave();
            if (_boardActivity != null) _boardActivity.RestoreFromSave();

            _case = null;
            _workFocusCaseId = null;
            ResumeFromLoadedSave();
            _save.Save();   // 숨겨진 임시 기록(slot_0)도 이 진행으로 맞춘다. 수동 칸은 건드리지 않는다
            Debug.Log($"[CaseDirector] 불러오기 | SAVE {slot:00} | {WorkdayDirector.CalendarDay}일째 {WorkdayDirector.FormatClock24()} 근무 {_workday.Workday}일차 | 장소 {_save.Current.work.place}");
        }

        /// <summary>불러온 진행에서 다시 시작한다. 저장할 때 있던 곳(숙소 / 맡은 사건 현장)으로 간다.</summary>
        private void ResumeFromLoadedSave()
        {
            HoldSavedMainCase();
            if (_case == null && _cases != null && _cases.TryGetCase(TutorialDirector.TutorialCaseId, out var tutorialCase)) _case = tutorialCase;

            if (_save.Current.work.place == "field" && HoldingMainCase)
            {
                EnterMainField();
                return;
            }
            EnterRoom(false);
            var room = GetRoomNight();
            if (room != null) room.SetHanyoungPresent(false);
            ShowWorkBoard();
        }

        /// <summary>지금 있는 곳을 적어 둔다. 불러오면 그 자리로 돌아간다.</summary>
        private void SetPlace(string place)
        {
            if (!InMain || _save == null || _save.Current == null) return;
            _save.Current.work.place = place;
        }

        /// <summary>타이틀 밖에 있는 동안 플레이 시간을 센다.</summary>
        private void TickPlayTime()
        {
            // 타이틀(과 그 위의 설명·설정)이 떠 있는 동안은 세지 않는다.
            if (_save == null || _save.Current == null || (_titleScreen != null && _ui.Contains(_titleScreen))) return;
            _save.Current.playSeconds += Time.unscaledDeltaTime;
        }
    }
}
