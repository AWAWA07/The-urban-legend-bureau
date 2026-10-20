using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UrbanLegendBureau.Core;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 현장의 짧은 무서운 연출 하나. 언제 터질지(Trigger)와 무엇을 차례로 할지(Steps)를 들고 있다.
    ///
    ///   OnShow      그 장소가 켜지고 Delay 초 뒤
    ///   ReachX      걷는 사람이 X 근처에 닿았을 때
    ///   AfterPoint  그 조사 지점(PointId)을 살피고 돌아왔을 때(CaseDirector 가 NotifyPoint 로 알린다)
    ///
    /// 차례(Step)는 기다리기, 소리, 보이기·숨기기, 깜빡임, 스르르 나타남·사라짐, 옮기기, 흔들기, 대사다.
    /// 한 판(저장본)에 한 번만 터진다. 터졌다는 표시는 진행 담당이 저장본에 적는다(Done / Mark).
    /// 연출 동안은 걷지도 조사하지도 않는다. 보이기로 띄운 것은 연출이 끝나면 처음 모습으로 돌려놓는다.
    /// </summary>
    public class FieldScare : MonoBehaviour
    {
        public enum TriggerKind { OnShow, ReachX, AfterPoint }
        public enum StepKind { Wait, Sound, Show, Hide, Flicker, FadeIn, FadeOut, Move, Shake, Lines }

        [Serializable]
        public class Step
        {
            public StepKind kind;
            [Tooltip("보이기·숨기기·깜빡임·나타남·옮기기·흔들기의 대상.")]
            public GameObject target;
            [Tooltip("소리 이름(Sound).")]
            public string sound;
            [Tooltip("기다리는 초 / 소리 크기 / 깜빡임 횟수 / 나타나는 초 / 옮기는 초 / 흔드는 초.")]
            public float a = 1f;
            [Tooltip("소리 높낮이 / 깜빡임 한 번의 초 / 흔드는 세기.")]
            public float b = 1f;
            [Tooltip("옮기는 거리.")]
            public Vector2 offset;
            [Tooltip("대사 글 ID 들(Lines).")]
            public string[] lines;
            [Tooltip("그 대사를 한영이 하는가. 비면 모두 차지한.")]
            public bool[] hanyoung;
            [Tooltip("옮기기를 시작만 하고 기다리지 않는다. 다음 차례와 함께 움직인다.")]
            public bool noWait;
        }

        public string ScareId;
        public TriggerKind Trigger;
        [Tooltip("ReachX: 닿으면 터지는 자리(장소 안 x).")]
        public float X;
        [Tooltip("ReachX: 그 자리에서 이만큼 안에 들면 닿은 것으로 본다.")]
        public float Range = 0.8f;
        [Tooltip("AfterPoint: 살피고 돌아오면 터지는 지점 ID.")]
        public string PointId;
        [Tooltip("이 표시가 저장본에 있어야 터진다. 비우면 조건 없다.")]
        public string RequireFlag;
        [Tooltip("OnShow: 장소가 켜지고 기다리는 초. 다른 방식도 터지기 전에 이만큼 기다린다.")]
        public float Delay;
        public Step[] Steps = Array.Empty<Step>();

        // ------------------------------------------------------------- 진행 담당이 거는 것

        /// <summary>이미 터졌는가. 저장본의 표시를 본다.</summary>
        public static Func<string, bool> Done;
        /// <summary>터졌다고 적는다.</summary>
        public static Action<string> Mark;
        /// <summary>대사를 띄운다. 다 넘기면 done.</summary>
        public static Action<string[], bool[], Action> Say;
        /// <summary>저장본에 그 표시가 있는가(RequireFlag).</summary>
        public static Func<string, bool> HasFlag;
        /// <summary>지금 터져도 되는가(누가 말하는 중, 다른 화면이 위에 떠 있는 동안은 미룬다).</summary>
        public static Func<bool> CanFire;

        /// <summary>연출이 돌고 있는 동안 true. 걷기와 조사를 막는 쪽이 본다.</summary>
        public static bool Playing { get; private set; }

        private static readonly List<FieldScare> Active = new List<FieldScare>();

        /// <summary>그 지점을 살피고 돌아왔다. 걸린 연출이 있으면 터뜨린다.</summary>
        public static void NotifyPoint(string pointId)
        {
            if (string.IsNullOrEmpty(pointId)) return;
            foreach (var s in Active.ToArray())
                if (s != null && s.Trigger == TriggerKind.AfterPoint && s.PointId == pointId) s.Fire();
        }

        // ------------------------------------------------------------- 터지기

        private bool _running;
        private bool _pending;
        private float _shownAt;
        private FieldWalker _walker;

        private void OnEnable()
        {
            Active.Add(this);
            _shownAt = Time.time;
            _pending = Trigger == TriggerKind.OnShow;
        }

        private void OnDisable()
        {
            Active.Remove(this);
            if (_running)
            {
                StopAllCoroutines();
                Restore();
                _running = false;
                Playing = false;
                UrbanLegendBureau.UI.FieldHudScreen.IsCutscene = false;
            }
        }

        private void Update()
        {
            if (_running || IsDone()) return;
            if (Time.time - _shownAt < Delay) return;

            if (Trigger == TriggerKind.ReachX)
            {
                if (_walker == null || !_walker.isActiveAndEnabled) _walker = FindWalker();
                if (_walker != null && Mathf.Abs(_walker.transform.localPosition.x - X) <= Range) _pending = true;
            }
            if (_pending) Fire();
        }

        /// <summary>같은 장소(부모) 안에서 걷는 사람을 찾는다.</summary>
        private FieldWalker FindWalker()
        {
            var parent = transform.parent;
            return parent != null ? parent.GetComponentInChildren<FieldWalker>(false) : null;
        }

        private bool IsDone() => !string.IsNullOrEmpty(ScareId) && Done != null && Done(ScareId);

        /// <summary>터뜨린다. 이미 터졌거나 조건이 안 되면 아무 일도 없다. 지금 못 터지면 다음 틈에 터진다.</summary>
        public void Fire()
        {
            if (_running || IsDone()) return;
            if (!string.IsNullOrEmpty(RequireFlag) && (HasFlag == null || !HasFlag(RequireFlag))) return;
            if (Playing || (CanFire != null && !CanFire())) { _pending = true; return; }

            _pending = false;
            Mark?.Invoke(ScareId);
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            _running = true;
            Playing = true;
            UrbanLegendBureau.UI.FieldHudScreen.IsCutscene = true;
            Debug.Log($"[FieldScare] {ScareId}");

            foreach (var step in Steps)
            {
                if (step == null) continue;
                switch (step.kind)
                {
                    case StepKind.Wait:
                        yield return new WaitForSeconds(step.a);
                        break;
                    case StepKind.Sound:
                        if (!string.IsNullOrEmpty(step.sound)) Sfx.Play(step.sound, step.a, step.b <= 0f ? 1f : step.b);
                        break;
                    case StepKind.Show:
                        Remember(step.target);
                        if (step.target != null) step.target.SetActive(true);
                        break;
                    case StepKind.Hide:
                        Remember(step.target);
                        if (step.target != null) step.target.SetActive(false);
                        break;
                    case StepKind.Flicker:
                        yield return Flicker(step.target, Mathf.Max(1, Mathf.RoundToInt(step.a)), Mathf.Max(0.03f, step.b));
                        break;
                    case StepKind.FadeIn:
                        yield return Fade(step.target, 0f, 1f, step.a);
                        break;
                    case StepKind.FadeOut:
                        yield return Fade(step.target, 1f, 0f, step.a);
                        break;
                    case StepKind.Move:
                        if (step.noWait) StartCoroutine(Move(step.target, step.offset, step.a));
                        else yield return Move(step.target, step.offset, step.a);
                        break;
                    case StepKind.Shake:
                        yield return Shake(step.target, step.a, step.b);
                        break;
                    case StepKind.Lines:
                        if (step.lines != null && step.lines.Length > 0 && Say != null)
                        {
                            bool said = false;
                            Say(step.lines, step.hanyoung, () => said = true);
                            while (!said) yield return null;
                        }
                        break;
                }
            }

            Restore();
            _running = false;
            Playing = false;
            UrbanLegendBureau.UI.FieldHudScreen.IsCutscene = false;
        }

        // ------------------------------------------------------------- 되돌리기

        private readonly Dictionary<GameObject, bool> _active = new Dictionary<GameObject, bool>();
        private readonly Dictionary<GameObject, Vector3> _positions = new Dictionary<GameObject, Vector3>();
        private readonly Dictionary<UnityEngine.Object, float> _alphas = new Dictionary<UnityEngine.Object, float>();

        private void Remember(GameObject go)
        {
            if (go == null) return;
            if (!_active.ContainsKey(go)) _active[go] = go.activeSelf;
            if (!_positions.ContainsKey(go)) _positions[go] = go.transform.localPosition;
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
                if (!_alphas.ContainsKey(sr)) _alphas[sr] = sr.color.a;
            foreach (var tx in go.GetComponentsInChildren<TMP_Text>(true))
                if (!_alphas.ContainsKey(tx)) _alphas[tx] = tx.alpha;
        }

        /// <summary>연출로 바꾼 것을 처음 모습으로 돌린다.</summary>
        private void Restore()
        {
            foreach (var kv in _alphas)
            {
                if (kv.Key is SpriteRenderer sr && sr != null) { var c = sr.color; c.a = kv.Value; sr.color = c; }
                else if (kv.Key is TMP_Text tx && tx != null) tx.alpha = kv.Value;
            }
            foreach (var kv in _positions) if (kv.Key != null) kv.Key.transform.localPosition = kv.Value;
            foreach (var kv in _active) if (kv.Key != null) kv.Key.SetActive(kv.Value);
            _alphas.Clear();
            _positions.Clear();
            _active.Clear();
        }

        private IEnumerator Flicker(GameObject go, int times, float seconds)
        {
            if (go == null) yield break;
            Remember(go);
            bool start = go.activeSelf;
            for (int i = 0; i < times; i++)
            {
                go.SetActive(!start);
                yield return new WaitForSeconds(seconds * UnityEngine.Random.Range(0.6f, 1.4f));
                go.SetActive(start);
                yield return new WaitForSeconds(seconds * UnityEngine.Random.Range(0.6f, 1.6f));
            }
        }

        private IEnumerator Fade(GameObject go, float from, float to, float seconds)
        {
            if (go == null) yield break;
            Remember(go);
            go.SetActive(true);
            var renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
            var texts = go.GetComponentsInChildren<TMP_Text>(true);
            seconds = Mathf.Max(0.01f, seconds);
            for (float e = 0f; e <= seconds; e += Time.deltaTime)
            {
                SetAlpha(renderers, texts, Mathf.Lerp(from, to, e / seconds));
                yield return null;
            }
            SetAlpha(renderers, texts, to);
        }

        /// <summary>처음 알파에 k 를 곱해 칠한다. 0 이면 보이지 않고 1 이면 처음 모습이다.</summary>
        private void SetAlpha(SpriteRenderer[] renderers, TMP_Text[] texts, float k)
        {
            foreach (var sr in renderers)
            {
                if (sr == null) continue;
                var c = sr.color;
                c.a = (_alphas.TryGetValue(sr, out var a0) ? a0 : 1f) * k;
                sr.color = c;
            }
            foreach (var tx in texts)
                if (tx != null) tx.alpha = (_alphas.TryGetValue(tx, out var a0) ? a0 : 1f) * k;
        }

        private IEnumerator Move(GameObject go, Vector2 offset, float seconds)
        {
            if (go == null) yield break;
            Remember(go);
            var t = go.transform;
            Vector3 from = t.localPosition;
            Vector3 to = from + (Vector3)offset;
            seconds = Mathf.Max(0.01f, seconds);
            for (float e = 0f; e <= seconds; e += Time.deltaTime)
            {
                t.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, e / seconds));
                yield return null;
            }
            t.localPosition = to;
        }

        private IEnumerator Shake(GameObject go, float seconds, float strength)
        {
            if (go == null) yield break;
            Remember(go);
            var t = go.transform;
            Vector3 start = t.localPosition;
            float end = Time.time + seconds;
            while (Time.time < end)
            {
                t.localPosition = start + (Vector3)(UnityEngine.Random.insideUnitCircle * strength);
                yield return null;
            }
            t.localPosition = start;
        }
    }
}
