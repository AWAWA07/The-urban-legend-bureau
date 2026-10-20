using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 진짜 괴담을 한영이 직접 정리하는 의식. 사건마다 자리·물건·순서·대사가 다르다(RiteDef).
    ///
    ///   #003 7호실의 노크        퇴마. 문지방(소금) → 머리맡(부적) → 창틀(초). 동명고시원 7호실.
    ///   #005 혼자 흔들리는 그네   배웅. 그네 아래(운동화) → 그네 사슬(이름표) → 입구 가로등(초). 엄마를 기다리던 아이를 집으로 보낸다.
    ///
    ///   단서     현장에서 순서를 알려 주는 단서를 모두 모아야 시작할 수 있다.
    ///   진행     자리를 누르면 한영이 무엇을 놓을지 묻고, 한영이 그 자리로 걸어가 놓는다. 차지한은 지켜본다.
    ///   틀리면   그것이 반응한다. 놓은 것이 모두 흩어지고 믿음도가 오른다.
    ///   다 하면  한영이 마무리한다. 믿음도가 35%(해결 목표 바로 위)로 떨어진다.
    ///   그 뒤    다른 사건처럼 수사 노트(분석·추론·보고서)와 정정글·댓글로 남은 소문을 30% 아래로 내린다.
    ///            의식 전에는 수사 노트를 열 수 없다(ExorcismBlocksCaseFile).
    /// </summary>
    public partial class CaseDirector
    {
        private class RiteDef
        {
            public string CaseId, LegendId, Key;           // Key: 글 ID 머리(field.gosi / field.park)
            public string RoomPath;                        // 의식을 치르는 묶음(현장 루트 아래 경로)
            public (string point, string item, string obj, float x)[] Steps;
            public string[] Items;                         // 고를 물건(순서대로 보기)
            public string[] Clues;                         // 시작에 필요한 단서
            public string FigureName;                      // 틀리거나 마칠 때 드러나는 그것
            public string WrongSound, FinaleSound;
            public string[] FinaleA; public bool[] FinaleAHan;  // 마무리 앞 대사
            public string[] FinaleB; public bool[] FinaleBHan;  // 마무리 뒤 대사
        }

        private static readonly RiteDef[] Rites =
        {
            new RiteDef
            {
                CaseId = "case_test_003", LegendId = "legend_test_003", Key = "field.gosi", RoomPath = "GosiRoom",
                Steps = new[] { ("point_rite_sill", "salt", "Rite_Salt", -3.3f), ("point_rite_head", "charm", "Rite_Charm", 2.9f), ("point_rite_window", "candle", "Rite_Candle", 0.1f) },
                Items = new[] { "salt", "charm", "candle" },
                Clues = new[] { "clue_test_010", "clue_test_011", "clue_test_012" },
                FigureName = "WindowFace", WrongSound = "door_bang_big", FinaleSound = "ghost_whisper_1",
                FinaleA = new[] { "field.gosi.rite.final.001", "field.gosi.rite.final.002" }, FinaleAHan = new[] { true, true },
                FinaleB = new[] { "field.gosi.rite.final.003", "field.gosi.rite.final.004", "field.gosi.rite.final.005" }, FinaleBHan = new[] { false, true, false },
            },
            new RiteDef
            {
                CaseId = "case_test_005", LegendId = "legend_test_005", Key = "field.park", RoomPath = "Park",
                Steps = new[] { ("point_prite_swing", "shoe", "Rite_Shoe", 0.9f), ("point_prite_chain", "tag", "Rite_Tag", 2.1f), ("point_prite_lamp", "candle", "Rite_Candle", -3.0f) },
                Items = new[] { "shoe", "tag", "candle" },
                Clues = new[] { "clue_test_017", "clue_test_019", "clue_test_020" },
                FigureName = "ChildFigure", WrongSound = "ghost_whisper_0", FinaleSound = "ghost_whisper_1",
                FinaleA = new[] { "field.park.rite.final.001", "field.park.rite.final.002", "field.park.rite.final.003" }, FinaleAHan = new[] { true, true, true },
                FinaleB = new[] { "field.park.rite.final.004", "field.park.rite.final.005", "field.park.rite.final.006", "field.park.rite.final.007" }, FinaleBHan = new[] { false, true, false, true },
            },
        };

        private const string ExorcismCaseId = "case_test_003";
        private const string GosiDoorPointId = "point_gosi_door7";
        private const string GosiExitPointId = "point_gosi_exit";

        /// <summary>의식이 틀릴 때마다 오르는 믿음도(%p).</summary>
        private const int RiteWrongBelief = 2;
        /// <summary>의식을 마친 뒤 남는 믿음도(%). 해결 목표(30%) 바로 위다.</summary>
        private const int RiteDoneBelief = 35;

        private static RiteDef RiteOf(string caseId)
        {
            foreach (var r in Rites) if (r.CaseId == caseId) return r;
            return null;
        }

        private RiteDef CurrentRite => _case != null ? RiteOf(_case.CaseId) : null;
        private bool RiteDoneFor(RiteDef r) => r != null && MainEventDone("rite_done:" + r.CaseId);

        /// <summary>의식이 있는 사건에서 의식 전에 수사 노트를 열려 하면 막고 알린다.</summary>
        private bool ExorcismBlocksCaseFile()
        {
            var r = CurrentRite;
            if (r == null || RiteDoneFor(r)) return false;
            if (_ui.Contains(_caseFileScreen)) _ui.Close(_caseFileScreen);
            ShowMainToast(_loc.Get(r.Key + ".casefile_locked"));
            return true;
        }

        private readonly List<string> _ritePlaced = new List<string>();
        private bool _riteBusy;

        /// <summary>고시원·공원 지점을 눌렀다. 맡은 일이 있으면 처리하고 true.</summary>
        private bool TryHandleGosiPoint(InvestigationPoint point)
        {
            switch (point.PointId)
            {
                case GosiDoorPointId: EnterGosiRoom(); return true;
                case GosiExitPointId: SwapGosi(false); return true;
            }
            var r = CurrentRite;
            if (r == null) return false;
            foreach (var s in r.Steps)
                if (s.point == point.PointId) { OnRitePoint(r, point); return true; }
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
            foreach (var f in root.GetComponentsInChildren<FieldFollower>(true)) { f.Following = true; if (f.isActiveAndEnabled) f.ResetToStart(); }
            foreach (var walker in root.GetComponentsInChildren<FieldWalker>(true)) if (walker.isActiveAndEnabled) walker.ResetToStart();
            if (inside) ResetRiteVisuals();
        }

        private Transform RiteRoom(RiteDef r)
        {
            var root = _field != null ? _field.ActiveRoot : null;
            return root != null && r != null ? root.transform.Find(r.RoomPath) : null;
        }

        /// <summary>현장에 들어올 때마다 의식의 모습을 저장본에 맞춘다. 마친 뒤면 놓은 것이 그대로 있다.</summary>
        private void ResetRiteVisuals()
        {
            _ritePlaced.Clear();
            _riteBusy = false;
            foreach (var r in Rites)
            {
                // 현장이 아직 꺼져 있을 수 있어 장면 전체에서 찾는다.
                Transform room = null;
                foreach (var t0 in Resources.FindObjectsOfTypeAll<Transform>())
                    if (t0.name == r.RoomPath && t0.gameObject.scene.IsValid() && t0.parent != null && t0.parent.name.StartsWith("FieldRoot_Legend")) { room = t0; break; }
                if (room == null) continue;
                bool done = RiteDoneFor(r);
                foreach (var s in r.Steps) { var t = room.Find(s.obj); if (t != null) t.gameObject.SetActive(done); }
                var fig = room.Find(r.FigureName);
                if (fig != null) fig.gameObject.SetActive(false);
                var swing = room.Find("SwingMiddle");
                var sway = swing != null ? swing.GetComponent<SwingSway>() : null;
                if (sway != null) { sway.enabled = !done; if (done) swing.localRotation = Quaternion.identity; }
            }
        }

        private void OnRitePoint(RiteDef r, InvestigationPoint point)
        {
            if (_riteBusy) return;
            if (RiteDoneFor(r)) { SayLines("!" + r.Key + ".rite.after"); return; }

            // 고시원 창틀: 옛 촛농을 아직 못 봤으면 먼저 살핀다(단서).
            if (point.PointId == "point_rite_window" && !CaseFlow.HasClue(_save, "clue_test_012"))
            {
                PerformFieldAction(point, "action_test_015");
                SayLines("field.gosi.rite.wax.001", "!field.gosi.rite.wax.002");
                return;
            }
            foreach (var c in r.Clues)
                if (!CaseFlow.HasClue(_save, c)) { SayLines("!" + r.Key + ".rite.not_yet"); return; }

            var labels = new List<System.Func<string>>();
            foreach (var item in r.Items) { var id = r.Key + ".item." + item; labels.Add(() => _loc.Get(id)); }
            labels.Add(() => _loc.Get(r.Key + ".item.cancel"));
            _fieldHudScreen.ShowLine(HanyoungNameTextId, () => _loc.Get(r.Key + ".rite.ask"), () =>
                _fieldHudScreen.ShowChoices(HanyoungNameTextId, labels, pick =>
                {
                    _fieldHudScreen.ClearSpeech();
                    if (pick < 0 || pick >= r.Items.Length) return;
                    PlaceRite(r, point.PointId, r.Items[pick]);
                }));
        }

        private void PlaceRite(RiteDef r, string pointId, string item)
        {
            int step = _ritePlaced.Count;
            var want = r.Steps[step];
            if (want.point != pointId || want.item != item) { StartCoroutine(RiteWrong(r)); return; }
            _ritePlaced.Add(item);
            StartCoroutine(HanyoungPlaces(r, step, want.obj, want.x, item));
        }

        private FieldFollower RiteHanyoung(RiteDef r)
        {
            var room = RiteRoom(r);
            return room != null ? room.GetComponentInChildren<FieldFollower>(true) : null;
        }

        /// <summary>한영이 그 자리로 걸어가 직접 놓는다. 차지한은 제자리에서 지켜본다.</summary>
        private IEnumerator HanyoungPlaces(RiteDef r, int step, string objName, float x, string item)
        {
            _riteBusy = true;
            FieldHudScreen.IsCutscene = true;
            var hy = RiteHanyoung(r);
            if (hy != null)
            {
                hy.Following = false;
                bool arrived = false;
                hy.WalkTo(x, 0f, () => arrived = true);
                float until = Time.time + 5f;
                while (!arrived && Time.time < until) yield return null;
            }
            yield return new WaitForSeconds(0.35f);
            var t = RiteRoom(r)?.Find(objName);
            if (t != null) t.gameObject.SetActive(true);
            Sfx.Play(item == "candle" ? "light_switch" : item == "salt" || item == "shoe" ? "sheet_rustle" : "paper_0", 0.45f);
            Debug.Log($"[CaseDirector] 의식 | {r.CaseId} {step + 1}/{r.Steps.Length} {item} — 한영이 놓는다");
            yield return new WaitForSeconds(0.5f);
            FieldHudScreen.IsCutscene = false;
            _riteBusy = false;
            if (_ritePlaced.Count < r.Steps.Length) { SayLines("!" + r.Key + ".rite.ok_" + _ritePlaced.Count); yield break; }
            StartCoroutine(RiteFinale(r));
        }

        private void HanyoungRejoin(RiteDef r)
        {
            var hy = RiteHanyoung(r);
            if (hy != null) hy.Following = true;
        }

        /// <summary>틀렸다. 그것이 반응하고 놓은 것이 흩어진다. 믿음도가 오른다.</summary>
        private IEnumerator RiteWrong(RiteDef r)
        {
            _riteBusy = true;
            FieldHudScreen.IsCutscene = true;
            var room = RiteRoom(r);
            var fig = room != null ? room.Find(r.FigureName)?.gameObject : null;
            Sfx.Play(r.WrongSound, 0.6f);
            yield return new WaitForSeconds(0.5f);
            if (fig != null) fig.SetActive(true);
            Sfx.Play("ghost_sting", 0.45f);
            yield return new WaitForSeconds(0.6f);
            if (fig != null) fig.SetActive(false);

            HanyoungRejoin(r);
            _ritePlaced.Clear();
            if (room != null) foreach (var s in r.Steps) { var t = room.Find(s.obj); if (t != null) t.gameObject.SetActive(false); }

            float before = _workday.DisplayBelief(r.LegendId);
            if (_tutorial != null) _tutorial.ShiftLegendBelief(r.LegendId, RiteWrongBelief);
            float after = _workday.DisplayBelief(r.LegendId);
            PushStatus();
            ShowMainToast(_loc.Get(r.Key + ".rite.wrong_toast", Num(before), Num(after)));
            Debug.Log($"[CaseDirector] 의식 실패 | {r.CaseId} {before:0.0}% -> {after:0.0}%");

            bool said = false;
            CctvLines(new[] { r.Key + ".rite.wrong.001", r.Key + ".rite.wrong.002" }, new[] { false, true }, 0, () => said = true);
            while (!said) yield return null;
            _fieldHudScreen.ClearSpeech();
            FieldHudScreen.IsCutscene = false;
            _riteBusy = false;
        }

        /// <summary>다 놓았다. 한영이 마무리하고 그것이 떠난다. 믿음도가 35% 로 떨어진다.</summary>
        private IEnumerator RiteFinale(RiteDef r)
        {
            _riteBusy = true;
            FieldHudScreen.IsCutscene = true;
            var room = RiteRoom(r);
            var fig = room != null ? room.Find(r.FigureName)?.gameObject : null;
            var candle = room != null ? room.Find("Rite_Candle")?.gameObject : null;

            bool said = false;
            CctvLines(r.FinaleA, r.FinaleAHan, 0, () => said = true);
            while (!said) yield return null;
            _fieldHudScreen.ClearSpeech();

            if (r.CaseId == ExorcismCaseId)
            {
                // 문이 미친 듯이 두드려지다가 창에 얼굴이 떴다가 촛불이 크게 흔들리고 사라진다.
                for (int i = 0; i < 6; i++) { Sfx.Play("door_bang_" + (i % 3), 0.4f + i * 0.06f, 1f + i * 0.05f); yield return new WaitForSeconds(0.32f - i * 0.03f); }
                if (fig != null) fig.SetActive(true);
                Sfx.Play(r.FinaleSound, 0.55f);
                for (int i = 0; i < 8; i++) { if (candle != null) candle.SetActive(i % 2 == 0); yield return new WaitForSeconds(0.08f); }
                if (candle != null) candle.SetActive(true);
                yield return new WaitForSeconds(0.6f);
                Sfx.Play("ghost_vanish", 0.6f);
                if (fig != null) fig.SetActive(false);
            }
            else
            {
                // 그네 위에 아이가 천천히 드러났다가, 가로등 쪽으로 걸어가며 옅어진다. 그네가 멈춘다.
                if (fig != null)
                {
                    fig.SetActive(true);
                    yield return FadeFigure(fig, 0f, 1f, 1.6f);
                }
                Sfx.Play(r.FinaleSound, 0.35f, 1.3f);
                yield return new WaitForSeconds(1.2f);
                if (fig != null)
                {
                    var t = fig.transform;
                    Vector3 from = t.localPosition, to = from + new Vector3(-4.2f, 0f, 0f);
                    for (float e = 0f; e < 3f; e += Time.deltaTime)
                    {
                        float k = e / 3f;
                        t.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, k));
                        SetFigureAlpha(fig, 1f - k);
                        yield return null;
                    }
                    fig.SetActive(false);
                    t.localPosition = from;
                    SetFigureAlpha(fig, 1f);
                }
                var swing = room != null ? room.Find("SwingMiddle") : null;
                var sway = swing != null ? swing.GetComponent<SwingSway>() : null;
                if (sway != null) sway.enabled = false;
                if (swing != null) swing.localRotation = Quaternion.identity;
                Sfx.Play("ghost_vanish", 0.35f, 1.2f);
            }
            yield return new WaitForSeconds(1.0f);

            float before = _workday.DisplayBelief(r.LegendId);
            // 진짜 원인은 떠났다. 남은 믿음도는 퍼진 소문이 붙든 몫이라 목표 바로 위(35%)에 남긴다.
            MarkMainEvent("rite_done:" + r.CaseId);
            int shift = Mathf.RoundToInt(RiteDoneBelief - before);
            if (_tutorial != null && shift < 0) _tutorial.ShiftLegendBelief(r.LegendId, shift);
            float after = _workday.DisplayBelief(r.LegendId);
            PushStatus();
            Debug.Log($"[CaseDirector] 의식 완료 | {r.CaseId} {before:0.0}% -> {after:0.0}%");

            said = false;
            CctvLines(r.FinaleB, r.FinaleBHan, 0, () => said = true);
            while (!said) yield return null;
            _fieldHudScreen.ClearSpeech();
            FieldHudScreen.IsCutscene = false;
            _riteBusy = false;
            HanyoungRejoin(r);
            ShowMainToast(_loc.Get(r.Key + ".rite.done_toast", Num(before), Num(after)) + "\n" + _loc.Get(r.Key + ".rite.next"));
            RefreshRoomDesktop();
        }

        private static IEnumerator FadeFigure(GameObject fig, float from, float to, float seconds)
        {
            for (float e = 0f; e < seconds; e += Time.deltaTime) { SetFigureAlpha(fig, Mathf.Lerp(from, to, e / seconds)); yield return null; }
            SetFigureAlpha(fig, to);
        }

        /// <summary>그것의 그림 알파. 만들 때의 알파를 1 로 보고 곱한다.</summary>
        private static void SetFigureAlpha(GameObject fig, float k)
        {
            foreach (var sr in fig.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var c = sr.color;
                if (!_figureBaseAlpha.TryGetValue(sr, out var a0)) { a0 = c.a; _figureBaseAlpha[sr] = a0; }
                c.a = a0 * k;
                sr.color = c;
            }
        }

        private static readonly Dictionary<SpriteRenderer, float> _figureBaseAlpha = new Dictionary<SpriteRenderer, float>();

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
