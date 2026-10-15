using UnityEngine;
using UrbanLegendBureau.Audio;

namespace UrbanLegendBureau.Core
{
    /// <summary>
    /// 배경 음악. Resources/Audio/Bgm/{이름} 을 반복해서 튼다. 한 번에 한 곡이다.
    /// 다른 곡을 틀면 앞 곡이 사그라드는 동안 새 곡이 커지며 넘어간다(소리통 둘을 번갈아 쓴다).
    /// 크기는 설정의 배경 음악 크기(AudioService.BgmVolume)를 따른다. 설정을 바꾸면 곧바로 따라간다.
    /// </summary>
    public static class Bgm
    {
        private static GameObject _root;
        private static AudioSource[] _sources;
        private static float[] _levels;      // 소리통마다 지금 크기(설정 크기를 곱하기 전)
        private static float[] _targets;
        private static float[] _fades;       // 초
        private static int _current = -1;
        private static string _currentName;
        private static float _volume = 1f;
        private static float _duck = 1f, _duckTarget = 1f, _duckFade = 0.5f;

        /// <summary>음악을 잠깐 줄인다(1 이면 그대로). 큰 소리가 날 때 그 소리가 앞에 나오게 한다. 다시 1 을 주면 돌아온다.</summary>
        public static void Duck(float factor, float fade = 0.3f)
        {
            _duckTarget = Mathf.Clamp01(factor);
            _duckFade = Mathf.Max(0.01f, fade);
        }

        /// <summary>지금 틀고 있는 곡 이름. 없으면 null.</summary>
        public static string Current => _currentName;

        /// <summary>곡을 튼다. 이미 그 곡이면 크기만 맞춘다. fade 초 동안 커진다.</summary>
        public static void Play(string name, float volume = 1f, float fade = 1.5f)
        {
            if (string.IsNullOrEmpty(name)) return;
            Ensure();
            _volume = Mathf.Clamp01(volume);
            if (name == _currentName && _current >= 0)
            {
                _targets[_current] = _volume;
                _fades[_current] = Mathf.Max(0.01f, fade);
                return;
            }

            var clip = Resources.Load<AudioClip>("Audio/Bgm/" + name);
            if (clip == null)
            {
                Debug.LogWarning("[Bgm] 곡이 없다 | Resources/Audio/Bgm/" + name);
                return;
            }

            // 지금 곡은 사그라들게 두고, 다른 소리통에서 새 곡을 튼다.
            if (_current >= 0) { _targets[_current] = 0f; _fades[_current] = Mathf.Max(0.01f, fade); }
            int next = _current == 0 ? 1 : 0;
            var s = _sources[next];
            s.clip = clip;
            s.loop = true;
            s.volume = 0f;
            s.Play();
            _levels[next] = 0f;
            _targets[next] = _volume;
            _fades[next] = Mathf.Max(0.01f, fade);
            _current = next;
            _currentName = name;
        }

        /// <summary>곡을 끈다. fade 초 동안 작아진 뒤 멈춘다.</summary>
        public static void Stop(float fade = 1.5f)
        {
            if (_sources == null || _current < 0) return;
            _targets[_current] = 0f;
            _fades[_current] = Mathf.Max(0.01f, fade);
            _current = -1;
            _currentName = null;
        }

        /// <summary>모든 곡에 공통으로 곱하는 크기. 효과음과 대사가 묻히지 않게 음악을 전체적으로 조금 낮춘다.</summary>
        private const float Trim = 0.55f;

        private static float Master =>
            (ServiceRegistry.TryGet<AudioService>(out var audio) ? audio.BgmVolume : 1f) * Trim;

        private static void Ensure()
        {
            if (_root != null) return;
            _root = new GameObject("Bgm");
            Object.DontDestroyOnLoad(_root);
            _sources = new AudioSource[2];
            _levels = new float[2];
            _targets = new float[2];
            _fades = new float[2] { 1f, 1f };
            for (int i = 0; i < 2; i++)
            {
                _sources[i] = _root.AddComponent<AudioSource>();
                _sources[i].playOnAwake = false;
                _sources[i].loop = true;
            }
            _root.AddComponent<BgmDriver>();
        }

        private static void Tick(float dt)
        {
            _duck = Mathf.MoveTowards(_duck, _duckTarget, dt / _duckFade);
            float master = Master * _duck;
            for (int i = 0; i < 2; i++)
            {
                _levels[i] = Mathf.MoveTowards(_levels[i], _targets[i], dt / _fades[i] * Mathf.Max(_volume, 0.01f));
                _sources[i].volume = _levels[i] * master;
                if (_levels[i] <= 0f && _targets[i] <= 0f && _sources[i].isPlaying) _sources[i].Stop();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _root = null;
            _sources = null;
            _current = -1;
            _currentName = null;
            _duck = _duckTarget = 1f;
        }

        private class BgmDriver : MonoBehaviour
        {
            private void Update() => Tick(Time.unscaledDeltaTime);
        }
    }
}
