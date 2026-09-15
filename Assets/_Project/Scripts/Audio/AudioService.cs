using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Save;

namespace UrbanLegendBureau.Audio
{
    /// <summary>
    /// BGM / 효과음 볼륨을 다룬다.
    ///
    /// 값은 기존 SaveData.settings.bgmVolume / sfxVolume 에 그대로 들어간다.
    /// 이 필드는 3단계부터 있었지만 아무도 쓰지 않았다. 새 저장 필드를 만들 이유가 없다.
    ///
    /// 실제 음원은 아직 없다. 지금은 볼륨 값을 보관하고 AudioSource에 반영하는 것까지만 한다.
    /// 음원이 생기면 _bgmSource에 클립을 물리면 된다. 이 클래스는 그대로 쓴다.
    /// </summary>
    public class AudioService : IService
    {
        /// <summary>볼륨 범위. UI는 0~100으로 보여주고 내부는 0~1로 둔다.</summary>
        public const float MinVolume = 0f;
        public const float MaxVolume = 1f;

        private AudioSource _bgmSource;
        private AudioSource _sfxSource;
        private GameObject _root;
        private SaveService _save;

        public float BgmVolume { get; private set; } = 0.8f;
        public float SfxVolume { get; private set; } = 1.0f;

        // ------------------------------------------------------------- 수명 주기

        public void Initialize()
        {
            _root = new GameObject("AudioRoot");
            Object.DontDestroyOnLoad(_root);

            _bgmSource = _root.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;

            _sfxSource = _root.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;

            ServiceRegistry.TryGet(out _save);
            LoadFromSave();

            Debug.Log($"[AudioService] 준비 완료 | BGM {BgmVolume:P0} / 효과음 {SfxVolume:P0}");
        }

        public void Shutdown()
        {
            if (_root != null) Object.Destroy(_root);
            _root = null;
            _bgmSource = null;
            _sfxSource = null;
            _save = null;
        }

        // ------------------------------------------------------------- 볼륨

        /// <summary>저장된 설정을 읽어 반영한다. 저장을 불러온 뒤에도 부를 수 있다.</summary>
        public void LoadFromSave()
        {
            var settings = GetSettings();
            if (settings == null) return;

            BgmVolume = Mathf.Clamp(settings.bgmVolume, MinVolume, MaxVolume);
            SfxVolume = Mathf.Clamp(settings.sfxVolume, MinVolume, MaxVolume);
            Apply();
        }

        /// <summary>BGM 볼륨을 바꾼다. 0~1. 설정에 바로 기록한다.</summary>
        public void SetBgmVolume(float value)
        {
            BgmVolume = Mathf.Clamp(value, MinVolume, MaxVolume);

            var settings = GetSettings();
            if (settings != null)
            {
                settings.bgmVolume = BgmVolume;
                if (_save != null) _save.MarkDirty();
            }
            Apply();
        }

        /// <summary>효과음 볼륨을 바꾼다. BGM과 서로 영향을 주지 않는다.</summary>
        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp(value, MinVolume, MaxVolume);

            var settings = GetSettings();
            if (settings != null)
            {
                settings.sfxVolume = SfxVolume;
                if (_save != null) _save.MarkDirty();
            }
            Apply();
        }

        /// <summary>설정 화면을 닫을 때처럼, 지금 값을 파일로 남기고 싶을 때 부른다.</summary>
        public bool SaveSettings()
        {
            return _save != null && _save.Save();
        }

        /// <summary>
        /// 효과음이 실제로 들리는지 확인할 때 쓰는 임시 음.
        /// 음원 에셋이 없으므로 짧은 사인파를 그 자리에서 만든다.
        /// 실제 효과음이 생기면 이 메서드는 지워도 된다.
        /// </summary>
        public void PlayTestSfx()
        {
            if (_sfxSource == null) return;

            const int sampleRate = 44100;
            const float seconds = 0.12f;
            int samples = (int)(sampleRate * seconds);

            var clip = AudioClip.Create("TestBeep", samples, 1, sampleRate, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float fade = 1f - (float)i / samples;              // 끝에서 줄여 딱 소리를 없앤다
                data[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.3f * fade;
            }
            clip.SetData(data, 0);

            _sfxSource.PlayOneShot(clip, SfxVolume);
        }

        // ------------------------------------------------------------- 내부

        private void Apply()
        {
            if (_bgmSource != null) _bgmSource.volume = BgmVolume;
            if (_sfxSource != null) _sfxSource.volume = SfxVolume;
        }

        private SettingsData GetSettings()
        {
            if (_save == null) ServiceRegistry.TryGet(out _save);
            return _save != null && _save.Current != null ? _save.Current.settings : null;
        }
    }
}
