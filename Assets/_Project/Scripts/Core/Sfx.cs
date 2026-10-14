using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UrbanLegendBureau.Audio;

namespace UrbanLegendBureau.Core
{
    /// <summary>
    /// 효과음. Resources/Audio/Sfx/{이름} 에 소리 파일을 넣으면 그 이름으로 재생된다.
    /// 아직 파일이 없으면 아무 소리도 나지 않는다. 화면 연출(글자, 흔들림)은 따로 있으므로 소리 없이도 무엇이 일어났는지 안다.
    ///
    /// 짧은 소리는 소리통 여러 개를 돌려 쓴다. 같은 소리가 겹쳐도 앞 소리를 끊지 않고, 한 번마다 높낮이를 조금씩 달리할 수 있다(발소리).
    /// 길게 이어지는 소리(열차가 달리는 소리)는 이름표(key)를 붙여 켜고 끈다. 켜고 끌 때 스르르 커지고 작아진다.
    /// 크기는 설정의 효과음 크기(AudioService.SfxVolume)를 따른다.
    ///
    /// 단추를 누르는 소리는 따로 부르지 않아도 난다. 마우스로 누를 수 있는 단추를 누르면 딸깍 한다(UiClickWatcher).
    /// </summary>
    public static class Sfx
    {
        private const int PoolSize = 16;

        private static GameObject _root;
        private static readonly List<AudioSource> _pool = new List<AudioSource>();
        private static int _next;
        private static readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private static readonly Dictionary<string, Loop> _loops = new Dictionary<string, Loop>();

        private class Loop
        {
            public AudioSource source;
            public float volume;    // 설정 크기를 곱하기 전
            public float target;    // 0 이면 사그라든 뒤 멈춘다
            public float fade;      // 초
        }

        /// <summary>한 번 울린다. pitch 는 1 이 본래 높이다.</summary>
        public static void Play(string name, float volume = 1f, float pitch = 1f)
        {
            var clip = Load(name);
            if (clip == null) return;
            Ensure();
            var source = _pool[_next];
            _next = (_next + 1) % _pool.Count;
            source.Stop();
            source.clip = clip;
            source.pitch = pitch;
            source.volume = Mathf.Clamp01(volume) * Master;
            source.Play();
        }

        /// <summary>높낮이를 조금 흔들어 울린다. 같은 소리가 되풀이될 때 덜 기계적으로 들린다.</summary>
        public static void PlayVaried(string name, float volume = 1f, float spread = 0.06f)
        {
            Play(name, volume, 1f + Random.Range(-spread, spread));
        }

        /// <summary>이어지는 소리를 켠다. 이미 켜져 있으면 크기만 맞춘다. fade 초 동안 커진다.</summary>
        public static void StartLoop(string key, string name, float volume = 1f, float fade = 1f)
        {
            var clip = Load(name);
            if (clip == null) return;
            Ensure();
            if (!_loops.TryGetValue(key, out var loop))
            {
                var source = _root.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.clip = clip;
                source.volume = 0f;
                source.Play();
                loop = new Loop { source = source };
                _loops[key] = loop;
            }
            else if (loop.source.clip != clip)
            {
                loop.source.clip = clip;
                loop.source.Play();
            }
            loop.target = Mathf.Clamp01(volume);
            loop.fade = Mathf.Max(0.01f, fade);
        }

        /// <summary>켜져 있는 이어지는 소리의 크기만 바꾼다. 꺼져 있으면 아무 일도 없다. 0 으로 줄여도 끄지 않고 숨죽여 둔다.</summary>
        public static void SetLoopVolume(string key, float volume, float fade = 1f)
        {
            if (!_loops.TryGetValue(key, out var loop) || loop.target <= 0f) return;
            loop.target = Mathf.Clamp(volume, 0.001f, 1f);
            loop.fade = Mathf.Max(0.01f, fade);
        }

        /// <summary>이어지는 소리를 끈다. fade 초 동안 작아진 뒤 멈춘다.</summary>
        public static void StopLoop(string key, float fade = 1f)
        {
            if (!_loops.TryGetValue(key, out var loop)) return;
            loop.target = 0f;
            loop.fade = Mathf.Max(0.01f, fade);
        }

        /// <summary>
        /// 이 화면 안의 단추에 마우스를 대면 작게 스윽 한다(ui_hover). 타이틀 메뉴처럼 고를 것을 훑어보는 화면에만 건다. 비우면 나지 않는다.
        /// </summary>
        public static GameObject HoverRoot;

        private static float _loopDuck = 1f, _loopDuckTarget = 1f, _loopDuckFade = 0.5f;

        /// <summary>이어지는 소리를 모두 잠깐 줄인다(1 이면 그대로). 큰 소리가 앞에 나오게 한다.</summary>
        public static void DuckLoops(float factor, float fade = 0.3f)
        {
            _loopDuckTarget = Mathf.Clamp01(factor);
            _loopDuckFade = Mathf.Max(0.01f, fade);
        }

        /// <summary>설정의 효과음 크기. 서비스가 없으면 1.</summary>
        private static float Master =>
            ServiceRegistry.TryGet<AudioService>(out var audio) ? audio.SfxVolume : 1f;

        private static AudioClip Load(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_clips.TryGetValue(name, out var clip)) return clip;
            clip = Resources.Load<AudioClip>("Audio/Sfx/" + name);
            _clips[name] = clip;
            return clip;
        }

        private static void Ensure()
        {
            if (_root != null) return;
            _root = new GameObject("Sfx");
            Object.DontDestroyOnLoad(_root);
            _pool.Clear();
            _loops.Clear();
            for (int i = 0; i < PoolSize; i++)
            {
                var s = _root.AddComponent<AudioSource>();
                s.playOnAwake = false;
                _pool.Add(s);
            }
            _root.AddComponent<SfxDriver>();
        }

        /// <summary>이어지는 소리의 크기를 매 프레임 목표로 옮긴다.</summary>
        internal static void Tick(float dt)
        {
            _loopDuck = Mathf.MoveTowards(_loopDuck, _loopDuckTarget, dt / _loopDuckFade);
            float master = Master * _loopDuck;
            List<string> done = null;
            foreach (var kv in _loops)
            {
                var loop = kv.Value;
                if (loop.source == null) continue;
                loop.volume = Mathf.MoveTowards(loop.volume, loop.target, dt / loop.fade);
                loop.source.volume = loop.volume * master;
                if (loop.target <= 0f && loop.volume <= 0f)
                {
                    loop.source.Stop();
                    Object.Destroy(loop.source);
                    (done ??= new List<string>()).Add(kv.Key);
                }
            }
            if (done != null) foreach (var k in done) _loops.Remove(k);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            _root = null;
            _loopDuck = _loopDuckTarget = 1f;
            _clips.Clear();
            Ensure();
        }

        /// <summary>
        /// Sfx 가 붙어 사는 물체. 이어지는 소리의 크기를 돌보고, 단추를 누르는 소리를 낸다.
        ///
        /// 단추 소리: 마우스를 누른 프레임에 그 자리의 UI 를 들여다보고, 누를 수 있는 단추(Selectable)가 맨 앞에 있으면 딸깍 한다.
        /// 대사를 넘기는 화면 전체 판(이름에 Advance)은 더 작고 짧게 톡 한다. 단추마다 소리를 따로 달지 않아도 모든 화면에서 난다.
        /// </summary>
        private class SfxDriver : MonoBehaviour
        {
            private readonly List<RaycastResult> _hits = new List<RaycastResult>();

            private void Update()
            {
                Tick(Time.unscaledDeltaTime);
                ClickSound();
                HoverSound();
            }

            private Selectable _hovered;

            private void HoverSound()
            {
                if (HoverRoot == null || !HoverRoot.activeInHierarchy) { _hovered = null; return; }
                var mouse = UnityEngine.InputSystem.Mouse.current;
                var events = EventSystem.current;
                if (mouse == null || events == null) return;

                var data = new PointerEventData(events) { position = mouse.position.ReadValue() };
                _hits.Clear();
                events.RaycastAll(data, _hits);
                var over = _hits.Count > 0 ? _hits[0].gameObject.GetComponentInParent<Selectable>() : null;
                if (over == _hovered) return;
                _hovered = over;
                if (over != null && over.IsInteractable() && over.transform.IsChildOf(HoverRoot.transform)) Play("ui_hover", 0.45f);
            }

            private void ClickSound()
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
                var events = EventSystem.current;
                if (events == null) return;

                var data = new PointerEventData(events) { position = mouse.position.ReadValue() };
                _hits.Clear();
                events.RaycastAll(data, _hits);
                if (_hits.Count == 0) return;

                var selectable = _hits[0].gameObject.GetComponentInParent<Selectable>();
                if (selectable == null || !selectable.IsInteractable()) return;
                if (selectable.name.StartsWith("Hotspot_")) return;   // 가까이 보기에서 살필 곳은 살피는 소리(inspect)가 따로 난다
                if (selectable.name.Contains("Advance")) Play("ui_tick", 0.45f);
                else Play("ui_click", 0.6f);
            }
        }
    }
}
