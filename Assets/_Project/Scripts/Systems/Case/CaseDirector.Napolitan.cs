using System.Collections.Generic;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 사건 #006 해월장 304호 안전 수칙 — 나폴리탄(규칙 괴담). 진짜 괴담이고 초반에서 가장 어렵다.
    ///
    ///   갇힘     304호에 들어서면 문이 잠기고 시계가 03:33 에 멈춘다.
    ///   규칙서   검은 인쇄 규칙(진짜) 사이에 빨간 손글씨 규칙(가짜)이 섞여 있다. 서로 어긋난다.
    ///            이전 투숙객 쪽지("빨간 글씨는 믿지 마, 검은 글씨 순서대로")로 가려낸다.
    ///   탈출     거울을 가린다 → 노크에 똑같이 두드려 답한다(열지 않는다) → 전화는 받되 말없이 끊는다 → 커튼을 걷는다.
    ///            차지한이 직접 한다. 한영은 거의 돕지 않는다. 틀리면 03:33 으로 처음부터, 믿음도 +3%p.
    ///   그 뒤    믿음도 35%. 다른 사건처럼 수사 노트와 정정글로 남은 소문을 정리한다.
    /// 의식 틀(RiteDef)의 '갇힘' 갈래를 쓴다(StepOptions).
    /// </summary>
    public partial class CaseDirector
    {
        private const string InnDoorPointId = "point_inn_door304";
        private const string InnExitStepPoint = "point_trap_door";

        private static RiteDef NapolitanRite => new RiteDef
        {
            CaseId = "case_test_006", LegendId = "legend_test_006", Key = "field.inn", RoomPath = "InnRoom",
            Steps = new[]
            {
                ("point_trap_mirror", "", "Trap_Towel", 0f),
                ("point_trap_door", "", "Trap_Knock", 0f),
                ("point_trap_phone", "", "Trap_PhoneOff", 0f),
                ("point_trap_window", "", "Trap_CurtainOpen", 0f),
            },
            Items = new string[0],
            Clues = new[] { "clue_test_023", "clue_test_024" },
            StepOptions = new[]
            {
                new[] { "count", "cover" },
                new[] { "open", "answer", "wait" },
                new[] { "name", "ignore", "silent" },
                new[] { "leave", "curtain" },
            },
            StepCorrect = new[] { 1, 1, 2, 1 },
            StepCueSounds = new[] { "door_bang_0", "intercom_ring", "" },
            WrongBelief = 3,
            FigureName = "MirrorFace", WrongSound = "door_bang_big", FinaleSound = "ghost_vanish",
            FinaleA = new[] { "field.inn.rite.final.001" }, FinaleAHan = new[] { false },
            FinaleB = new[] { "field.inn.rite.final.002", "field.inn.rite.final.003", "field.inn.rite.final.004" }, FinaleBHan = new[] { false, true, false },
        };

        /// <summary>304호 문. 처음 들어설 때 갇힌다.</summary>
        private void EnterInnRoom()
        {
            SwapGosi(true);
            var r = CurrentRite;
            if (r == null || RiteDoneFor(r)) return;
            Sfx.Play("key_2", 0.5f);
            CctvLines(new[] { "field.inn.trap.001", "field.inn.trap.002", "field.inn.trap.003", "field.inn.trap.004" }, new[] { false, false, true, false }, 0,
                () => _fieldHudScreen.ClearSpeech());
        }

        /// <summary>갇힌 방의 자리를 눌렀다. 지금 차례면 행동을 고르고, 아니면 한마디 한다.</summary>
        private void TrapPoint(RiteDef r, InvestigationPoint point)
        {
            int idx = -1;
            for (int i = 0; i < r.Steps.Length; i++) if (r.Steps[i].point == point.PointId) idx = i;
            int step = _ritePlaced.Count;
            if (idx < 0) return;
            if (idx > step) { SayLines(r.Key + ".trap.notyet_" + idx); return; }
            if (idx < step) { SayLines(r.Key + ".trap.done_" + idx); return; }

            var opts = r.StepOptions[idx];
            var labels = new List<System.Func<string>>();
            foreach (var o in opts) { var id = r.Key + ".trap.opt." + o; labels.Add(() => _loc.Get(id)); }
            labels.Add(() => _loc.Get(r.Key + ".item.cancel"));
            _fieldHudScreen.ShowLine(FieldSpeakerTextId, () => _loc.Get(r.Key + ".trap.ask_" + idx), () =>
                _fieldHudScreen.ShowChoices(FieldSpeakerTextId, labels, pick =>
                {
                    _fieldHudScreen.ClearSpeech();
                    if (pick < 0 || pick >= opts.Length) return;
                    if (pick != r.StepCorrect[idx]) { StartCoroutine(RiteWrong(r)); return; }
                    _ritePlaced.Add(opts[pick]);
                    var t = RiteRoom(r)?.Find(r.Steps[idx].obj);
                    if (t != null) t.gameObject.SetActive(true);
                    Sfx.Play(idx == 0 ? "cloth_0" : idx == 1 ? "door_bang_2" : idx == 2 ? "phone_click" : "sheet_rustle", 0.5f);
                    UnityEngine.Debug.Log($"[CaseDirector] 갇힘 | {r.CaseId} {idx + 1}/{r.Steps.Length} {opts[pick]}");
                    if (_ritePlaced.Count >= r.Steps.Length) { StartCoroutine(RiteFinale(r)); return; }
                    // 다음 차례를 알린다: 노크가 들리고, 전화가 울리고, 커튼 너머가 밝아진다.
                    var cue = r.StepCueSounds != null && idx < r.StepCueSounds.Length ? r.StepCueSounds[idx] : "";
                    if (!string.IsNullOrEmpty(cue)) Sfx.Play(cue, 0.55f);
                    SayLines(r.Key + ".trap.cue_" + (idx + 1) + ".001", "!" + r.Key + ".trap.cue_" + (idx + 1) + ".002");
                }));
        }
    }
}
