using UnityEngine;
using UrbanLegendBureau.Systems;

namespace UrbanLegendBureau.EditorTools
{
    /// <summary>
    /// 본편 현장의 무서운 연출(FieldScare)을 짓는 도구. 장소마다 두세 개씩 건다.
    /// 연출은 한 판에 한 번만 터지고, 끝나면 띄운 것을 처음 모습으로 돌려놓는다.
    /// </summary>
    public static partial class SliceSceneBuilder
    {
        private static FieldScare AddScare(Transform root, string id, FieldScare.TriggerKind trigger, float x, string pointId,
            float delay, params FieldScare.Step[] steps)
        {
            var go = new GameObject("Scare_" + id);
            go.transform.SetParent(root, false);
            var scare = go.AddComponent<FieldScare>();
            scare.ScareId = id;
            scare.Trigger = trigger;
            scare.X = x;
            scare.PointId = pointId;
            scare.Delay = delay;
            scare.Steps = steps;
            return scare;
        }

        private static FieldScare.Step SWait(float seconds) => new FieldScare.Step { kind = FieldScare.StepKind.Wait, a = seconds };

        private static FieldScare.Step SSound(string name, float volume = 0.5f, float pitch = 1f) =>
            new FieldScare.Step { kind = FieldScare.StepKind.Sound, sound = name, a = volume, b = pitch };

        private static FieldScare.Step SShow(GameObject target) => new FieldScare.Step { kind = FieldScare.StepKind.Show, target = target };

        private static FieldScare.Step SHide(GameObject target) => new FieldScare.Step { kind = FieldScare.StepKind.Hide, target = target };

        private static FieldScare.Step SFlicker(GameObject target, int times, float seconds) =>
            new FieldScare.Step { kind = FieldScare.StepKind.Flicker, target = target, a = times, b = seconds };

        private static FieldScare.Step SFadeIn(GameObject target, float seconds) => new FieldScare.Step { kind = FieldScare.StepKind.FadeIn, target = target, a = seconds };

        private static FieldScare.Step SFadeOut(GameObject target, float seconds) => new FieldScare.Step { kind = FieldScare.StepKind.FadeOut, target = target, a = seconds };

        private static FieldScare.Step SMove(GameObject target, Vector2 offset, float seconds) =>
            new FieldScare.Step { kind = FieldScare.StepKind.Move, target = target, offset = offset, a = seconds };

        /// <summary>옮기기를 시작만 하고 기다리지 않는다. 다음 차례와 함께 움직인다(양쪽 문짝).</summary>
        private static FieldScare.Step SMoveAsync(GameObject target, Vector2 offset, float seconds) =>
            new FieldScare.Step { kind = FieldScare.StepKind.Move, target = target, offset = offset, a = seconds, noWait = true };

        private static FieldScare.Step SShake(GameObject target, float seconds, float strength) =>
            new FieldScare.Step { kind = FieldScare.StepKind.Shake, target = target, a = seconds, b = strength };

        /// <summary>대사 여러 줄. 이름 앞에 '!' 를 붙이면 한영이 한다.</summary>
        private static FieldScare.Step SLines(params string[] ids)
        {
            var lines = new string[ids.Length];
            var hanyoung = new bool[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                hanyoung[i] = ids[i].StartsWith("!");
                lines[i] = hanyoung[i] ? ids[i].Substring(1) : ids[i];
            }
            return new FieldScare.Step { kind = FieldScare.StepKind.Lines, lines = lines, hanyoung = hanyoung };
        }

        /// <summary>장소 전체를 덮는 어둠. 처음에는 꺼져 있고 연출이 스르르 띄운다. 사람보다 앞에 그린다.</summary>
        private static GameObject AddBlackout(Transform root, string name, Color color)
        {
            var go = AddFieldRect(root, name, new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), color, 12);
            go.SetActive(false);
            return go;
        }
    }
}
