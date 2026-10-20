using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 사건 #003 7호실의 노크 — 진짜 괴담. 한영이 직접 결계를 쳐서 퇴마한다.
    ///
    ///   단서     관리인 수첩 두 쪽(문지방에 소금 → 머리맡에 부적 → 창틀에 초), 7호실 창틀의 옛 촛농.
    ///   결계     문지방·머리맡·창틀을 눌러 소금·부적·초 가운데 하나를 놓는다. 순서와 자리가 모두 맞아야 한다.
    ///   틀리면   그것이 반응한다(문 두드림, 불이 꺼졌다 켜짐, 창의 얼굴). 놓은 것이 모두 흩어지고 믿음도가 오른다.
    ///   다 치면  한영이 마무리하고 그것이 사라진다. 믿음도가 크게 떨어져 사건이 해결된다.
    /// 수사 노트와 정정글이 없는 사건이라 해결은 믿음도 하나로 판정한다(WorkdayDirector.Evaluate).
    /// </summary>
    public partial class CaseDirector
    {
        private const string ExorcismCaseId = "case_test_003";
        private const string GosiDoorPointId = "point_gosi_door7";
        private const string GosiExitPointId = "point_gosi_exit";
        private const string RiteDonePrefix = "rite:";

        /// <summary>결계 자리와 맞는 물건, 그리고 치는 순서.</summary>
        private static readonly (string point, string item, string obj)[] RiteOrder =
        {
            ("point_rite_sill", "salt", "Rite_Salt"),
            ("point_rite_head", "charm", "Rite_Charm"),
            ("point_rite_window", "candle", "Rite_Candle"),
        };
        private static readonly string[] RiteItems = { "salt", "charm", "candle" };
        private static readonly string[] RiteClues = { "clue_test_010", "clue_test_011", "clue_test_012" };

        /// <summary>결계가 틀릴 때마다 오르는 믿음도(%p).</summary>
        private const int RiteWrongBelief = 2;
        /// <summary>결계를 다 쳤을 때 떨어지는 믿음도(%p).</summary>
        private const int RiteDoneBelief = -70;

        private readonly List<string> _ritePlaced = new List<string>();
        private bool _riteBusy;

        /// <summary>고시원 지점을 눌렀다. 맡은 일이 있으면 처리하고 true.</summary>
        private bool TryHandleGosiPoint(InvestigationPoint point)
        {
            switch (point.PointId)
            {
                case GosiDoorPointId: EnterGosiRoom(); return true;
                case GosiExitPointId: SwapGosi(false); return true;
            }
            foreach (var r in RiteOrder)
                if (r.point == point.PointId) { OnRitePoint(point); return true; }
            return false;
        }

        private void EnterGosiRoom()
        {
            if (!MainEventDone("gosi_enter"))
            {
                MarkMainEvent("gosi_enter");
                CctvLines(new[] { "field.gosi.enter.001", "field.gosi.enter.002", "field.gosi.enter.003" }, new[] { false, true, true }, 0, () =>
                {
                    _fieldHudScreen.ClearSpeech();
                    SwapGosi(true);
                });
                return;
            }
            SwapGosi(true);
        }

        private void SwapGosi(bool inside)
        {
            var root = _field != null ? _field.ActiveRoot : null;
            var swap = root != null ? root.GetComponent<FieldSceneSwap>() : null;
            if (swap == null) return;
            Sfx.Play(inside ? "room_door_open" : "room_door", 0.45f);
            swap.SetAfter(inside);
            foreach (var walker in root.GetComponentsInChildren<FieldWalker>(true)) if (walker.isActiveAndEnabled) walker.ResetToStart();
            if (inside) ResetRiteVisuals();
        }

        private Transform GosiRoom()
        {
            var root = _field != null ? _field.ActiveRoot : null;
            return root != null ? root.transform.Find("GosiRoom") : null;
        }

        private void ResetRiteVisuals()
        {
            _ritePlaced.Clear();
            var room = GosiRoom();
            if (room == null) return;
            bool done = _workday != null && _workday.IsSolved(ExorcismCaseId);
            foreach (var r in RiteOrder)
            {
                var t = room.Find(r.obj);
                if (t != null) t.gameObject.SetActive(done);
            }
            var face = room.Find("WindowFace");
            if (face != null) face.gameObject.SetActive(false);
        }

        private void OnRitePoint(InvestigationPoint point)
        {
            if (_riteBusy) return;
            if (_workday != null && _workday.IsSolved(ExorcismCaseId))
            {
                SayLines("!field.gosi.rite.after");
                return;
            }

            // 창틀: 옛 촛농을 아직 못 봤으면 먼저 살핀다(단서).
            if (point.PointId == "point_rite_window" && !CaseFlow.HasClue(_save, "clue_test_012"))
            {
                PerformFieldAction(point, "action_test_015");
                SayLines("field.gosi.rite.wax.001", "!field.gosi.rite.wax.002");
                return;
            }
            foreach (var c in RiteClues)
                if (!CaseFlow.HasClue(_save, c))
                {
                    SayLines("!field.gosi.rite.not_yet");
                    return;
                }

            var labels = new List<System.Func<string>>
            {
                () => _loc.Get("field.gosi.item.salt"),
                () => _loc.Get("field.gosi.item.charm"),
                () => _loc.Get("field.gosi.item.candle"),
                () => _loc.Get("field.gosi.item.cancel"),
            };
            _fieldHudScreen.ShowLine(HanyoungNameTextId, () => _loc.Get("field.gosi.rite.ask"), () =>
                _fieldHudScreen.ShowChoices(HanyoungNameTextId, labels, pick =>
                {
                    _fieldHudScreen.ClearSpeech();
                    if (pick < 0 || pick >= RiteItems.Length) return;
                    PlaceRite(point.PointId, RiteItems[pick]);
                }));
        }

        private void PlaceRite(string pointId, string item)
        {
            int step = _ritePlaced.Count;
            var want = RiteOrder[step];
            if (want.point != pointId || want.item != item)
            {
                StartCoroutine(RiteWrong());
                return;
            }
            _ritePlaced.Add(item);
            var t = GosiRoom()?.Find(want.obj);
            if (t != null) t.gameObject.SetActive(true);
            Sfx.Play(item == "salt" ? "sheet_rustle" : item == "charm" ? "paper_0" : "light_switch", 0.45f);
            Debug.Log($"[CaseDirector] 결계 | {step + 1}/3 {pointId} {item}");
            if (_ritePlaced.Count < RiteOrder.Length)
            {
                SayLines("!field.gosi.rite.ok_" + _ritePlaced.Count);
                return;
            }
            StartCoroutine(RiteFinale());
        }

        /// <summary>틀렸다. 그것이 반응하고 놓은 것이 흩어진다. 믿음도가 오른다.</summary>
        private IEnumerator RiteWrong()
        {
            _riteBusy = true;
            FieldHudScreen.IsCutscene = true;
            var room = GosiRoom();
            var dark = room != null ? room.Find("Blackout")?.gameObject : null;
            var face = room != null ? room.Find("WindowFace")?.gameObject : null;
            Sfx.Play("door_bang_big", 0.6f);
            yield return new WaitForSeconds(0.5f);
            if (face != null) face.SetActive(true);
            Sfx.Play("ghost_sting", 0.6f);
            yield return new WaitForSeconds(0.5f);
            if (face != null) face.SetActive(false);
            Sfx.Play("door_bang_0", 0.5f);

            _ritePlaced.Clear();
            if (room != null) foreach (var r in RiteOrder) { var t = room.Find(r.obj); if (t != null) t.gameObject.SetActive(false); }

            float before = _workday.DisplayBelief("legend_test_003");
            if (_tutorial != null) _tutorial.ShiftLegendBelief("legend_test_003", RiteWrongBelief);
            float after = _workday.DisplayBelief("legend_test_003");
            PushStatus();
            ShowMainToast(_loc.Get("field.gosi.rite.wrong_toast", Num(before), Num(after)));
            Debug.Log($"[CaseDirector] 결계 실패 | {before:0.0}% -> {after:0.0}%");

            bool said = false;
            CctvLines(new[] { "field.gosi.rite.wrong.001", "field.gosi.rite.wrong.002" }, new[] { false, true }, 0, () => said = true);
            while (!said) yield return null;
            _fieldHudScreen.ClearSpeech();
            FieldHudScreen.IsCutscene = false;
            _riteBusy = false;
        }

        /// <summary>결계를 다 쳤다. 한영이 마무리하고 그것이 사라진다. 믿음도가 크게 떨어져 해결된다.</summary>
        private IEnumerator RiteFinale()
        {
            _riteBusy = true;
            FieldHudScreen.IsCutscene = true;
            var room = GosiRoom();
            var face = room != null ? room.Find("WindowFace")?.gameObject : null;
            var candle = room != null ? room.Find("Rite_Candle")?.gameObject : null;

            bool said = false;
            CctvLines(new[] { "field.gosi.rite.final.001", "field.gosi.rite.final.002" }, new[] { true, true }, 0, () => said = true);
            while (!said) yield return null;
            _fieldHudScreen.ClearSpeech();

            // 문이 미친 듯이 두드려지다가, 창에 얼굴이 떴다가, 촛불이 크게 흔들리고 사라진다.
            for (int i = 0; i < 6; i++) { Sfx.Play("door_bang_" + (i % 3), 0.4f + i * 0.06f, 1f + i * 0.05f); yield return new WaitForSeconds(0.32f - i * 0.03f); }
            if (face != null) face.SetActive(true);
            Sfx.Play("ghost_whisper_1", 0.55f);
            for (int i = 0; i < 8; i++) { if (candle != null) candle.SetActive(i % 2 == 0); yield return new WaitForSeconds(0.08f); }
            if (candle != null) candle.SetActive(true);
            yield return new WaitForSeconds(0.6f);
            Sfx.Play("ghost_vanish", 0.6f);
            if (face != null) face.SetActive(false);
            yield return new WaitForSeconds(1.2f);

            float before = _workday.DisplayBelief("legend_test_003");
            if (_tutorial != null) _tutorial.ShiftLegendBelief("legend_test_003", RiteDoneBelief);
            float after = _workday.DisplayBelief("legend_test_003");
            PushStatus();
            Debug.Log($"[CaseDirector] 결계 완성 | {before:0.0}% -> {after:0.0}%");

            said = false;
            CctvLines(new[] { "field.gosi.rite.final.003", "field.gosi.rite.final.004", "field.gosi.rite.final.005" }, new[] { false, true, false }, 0, () => said = true);
            while (!said) yield return null;
            _fieldHudScreen.ClearSpeech();
            FieldHudScreen.IsCutscene = false;
            _riteBusy = false;
            ResolveExorcism(before, after);
        }

        private void ResolveExorcism(float before, float after)
        {
            if (!_workday.TryResolve(ExorcismCaseId))
            {
                ShowMainToast(_loc.Get("field.gosi.rite.done_toast", Num(before), Num(after)));
                return;
            }
            if (_workday.CurrentCaseId == ExorcismCaseId) _workday.CurrentCaseId = null;
            _save.AutoSave();
            var opened = _workday.OpenCasesNow();
            string text = _loc.Get("field.gosi.rite.done_toast", Num(before), Num(after)) + "\n" + _loc.Get(MainSolvedTextId, CaseName(_workday.FindCase(ExorcismCaseId)));
            if (opened.Count > 0)
            {
                var names = new List<string>();
                foreach (var id in opened) names.Add(CaseName(_workday.FindCase(id)));
                text += "\n" + _loc.Get("ui.work.case_opened_now", string.Join(", ", names));
            }
            ShowMainToast(text);
            RefreshRoomDesktop();
            UpdateRoomComputerBeckon();
            Debug.Log("[CaseDirector] 본편 | 퇴마로 사건 해결 | " + ExorcismCaseId);
        }

        /// <summary>한마디. '!' 로 시작하는 글 ID 는 한영이 한다.</summary>
        private void SayLines(params string[] ids)
        {
            var r = Remark(ids);
            CctvLines(r.lines, r.hanyoung, 0, () => _fieldHudScreen.ClearSpeech());
        }

        /// <summary>가까이 보기 없이 그 지점에서 조사 행동 하나를 한다. 단서를 얻으면 알린다.</summary>
        private void PerformFieldAction(InvestigationPoint point, string actionId)
        {
            var action = _actions != null ? _actions.GetAction(actionId) : null;
            if (action == null || !_actions.CanPerform(_save, action)) return;
            var result = _actions.Perform(_save, _caseId, _legendId, action);
            if (!result.Success) return;
            AfterAction(result.MinutesAdded, result.Spread);
            MarkActionDone(point, action);
            if (result.GotNewClue) AnnounceClue(point, result.AcquiredClueId);
            if (_fieldHudScreen != null) _fieldHudScreen.Refresh();
        }
    }
}
