using System.Collections.Generic;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 현장에서 만나는 사람과의 대화.
    ///
    ///   처음 말을 걸면 인사(Intro)를 하고, 차지한이 물을 것을 고른다. 물은 것은 다시 나오지 않는다.
    ///   다 물었거나 그만두면 작별(Bye). 다시 말을 걸면 인사 없이 남은 물음부터.
    ///   사건의 원인을 정리한 뒤(의식이 있으면 의식을 마친 뒤)에는 다른 인사(After)를 한다.
    ///
    /// 대사 ID 앞의 '@' 는 그 사람, '!' 는 한영, 아무것도 없으면 차지한이 말한다.
    /// 초상은 이름 ID(npc.*)로 찾는다(CharacterArt.NpcFace).
    /// </summary>
    public partial class CaseDirector
    {
        private sealed class NpcDef
        {
            public string PointId, NameTextId, Key, CaseId;
            public string[] Intro, Topics, Bye, After;
            public string[][] TopicLines;   // Topics 와 같은 순서
        }

        /// <summary>#003 동명고시원 8호실 고시생. 벽 하나 너머로 2년째 7호실 노크를 듣고 있다.</summary>
        private static readonly NpcDef[] Npcs =
        {
            new NpcDef
            {
                PointId = "point_gosi_npc", NameTextId = "npc.gosi_student", Key = "field.gosi.npc", CaseId = "case_test_003",
                Intro = new[] { "@field.gosi.npc.intro.001", "field.gosi.npc.intro.002", "@field.gosi.npc.intro.003" },
                Topics = new[] { "knock", "room", "rumor" },
                TopicLines = new[]
                {
                    new[] { "@field.gosi.npc.knock.001", "@field.gosi.npc.knock.002", "!field.gosi.npc.knock.003" },
                    new[] { "@field.gosi.npc.room.001", "@field.gosi.npc.room.002", "!field.gosi.npc.room.003" },
                    new[] { "@field.gosi.npc.rumor.001", "@field.gosi.npc.rumor.002", "field.gosi.npc.rumor.003" },
                },
                Bye = new[] { "@field.gosi.npc.bye.001" },
                After = new[] { "@field.gosi.npc.after.001", "@field.gosi.npc.after.002", "!field.gosi.npc.after.003" },
            },
        };

        private bool TryHandleNpcPoint(InvestigationPoint point)
        {
            foreach (var n in Npcs)
                if (n.PointId == point.PointId) { TalkNpc(n); return true; }
            return false;
        }

        private void TalkNpc(NpcDef n)
        {
            var rite = RiteOf(n.CaseId);
            if (rite != null && RiteDoneFor(rite)) { NpcLines(n, n.After, 0, () => _fieldHudScreen.ClearSpeech()); return; }
            if (!MainEventDone("npc:" + n.PointId))
            {
                MarkMainEvent("npc:" + n.PointId);
                NpcLines(n, n.Intro, 0, () => NpcTopics(n));
                return;
            }
            NpcTopics(n);
        }

        /// <summary>남은 물음을 늘어놓는다. 마지막 칸은 그만 묻기.</summary>
        private void NpcTopics(NpcDef n)
        {
            var left = new List<string>();
            foreach (var t in n.Topics) if (!MainEventDone("npc:" + n.PointId + ":" + t)) left.Add(t);
            if (left.Count == 0) { NpcLines(n, n.Bye, 0, () => _fieldHudScreen.ClearSpeech()); return; }

            var labels = new List<System.Func<string>>();
            foreach (var t in left) { var id = n.Key + ".ask." + t; labels.Add(() => _loc.Get(id)); }
            labels.Add(() => _loc.Get(n.Key + ".ask.end"));
            _fieldHudScreen.ShowChoices(FieldSpeakerTextId, labels, pick =>
            {
                if (pick < 0 || pick >= left.Count) { NpcLines(n, n.Bye, 0, () => _fieldHudScreen.ClearSpeech()); return; }
                string topic = left[pick];
                MarkMainEvent("npc:" + n.PointId + ":" + topic);
                NpcLines(n, n.TopicLines[System.Array.IndexOf(n.Topics, topic)], 0, () => NpcTopics(n));
            });
        }

        private void NpcLines(NpcDef n, string[] ids, int index, System.Action onDone)
        {
            if (ids == null || index >= ids.Length) { onDone?.Invoke(); return; }
            string raw = ids[index];
            string speaker = FieldSpeakerTextId, id = raw;
            if (raw.StartsWith("@")) { speaker = n.NameTextId; id = raw.Substring(1); }
            else if (raw.StartsWith("!")) { speaker = HanyoungNameTextId; id = raw.Substring(1); }
            _fieldHudScreen.ShowLine(speaker, () => _loc.Get(id), () => NpcLines(n, ids, index + 1, onDone), id);
        }
    }
}
