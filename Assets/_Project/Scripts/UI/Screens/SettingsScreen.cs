using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Audio;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 설정 화면. BGM과 효과음 볼륨을 따로 조절한다.
    ///
    /// 값 보관과 저장은 AudioService가 한다. 이 화면은 슬라이더를 서비스에 잇기만 한다.
    /// 표시는 0~100, 내부는 0~1이다. 플레이어에게 0.8 같은 값을 보여줄 이유가 없다.
    /// </summary>
    public class SettingsScreen : UIScreen
    {
        [Header("표시 대상")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _bgmLabel;
        [SerializeField] private TMP_Text _sfxLabel;

        [Header("조절")]
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;

        private const string TitleTextId = "ui.settings.title";
        private const string BgmTextId = "ui.settings.bgm";
        private const string SfxTextId = "ui.settings.sfx";

        private AudioService _audio;
        private bool _binding;

        protected override void OnOpen()
        {
            ServiceRegistry.TryGet(out _audio);

            EventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);

            // 서비스 값을 슬라이더에 넣는 동안에는 콜백이 다시 서비스를 건드리지 않게 막는다.
            _binding = true;
            if (_bgmSlider != null)
            {
                _bgmSlider.minValue = 0f;
                _bgmSlider.maxValue = 100f;
                _bgmSlider.wholeNumbers = true;
                _bgmSlider.value = _audio != null ? _audio.BgmVolume * 100f : 80f;
                _bgmSlider.onValueChanged.RemoveListener(OnBgmChanged);
                _bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            }
            if (_sfxSlider != null)
            {
                _sfxSlider.minValue = 0f;
                _sfxSlider.maxValue = 100f;
                _sfxSlider.wholeNumbers = true;
                _sfxSlider.value = _audio != null ? _audio.SfxVolume * 100f : 100f;
                _sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
                _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            }
            _binding = false;

            Refresh();
        }

        protected override void OnClose()
        {
            EventBus.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);

            // 화면을 닫을 때 파일로 남긴다. 다음 실행에서도 그대로 들린다.
            if (_audio != null) _audio.SaveSettings();
        }

        private void OnLanguageChanged(LanguageChangedEvent evt)
        {
            Refresh();
        }

        private void OnBgmChanged(float value)
        {
            if (_binding || _audio == null) return;
            _audio.SetBgmVolume(value / 100f);
            Refresh();
        }

        private void OnSfxChanged(float value)
        {
            if (_binding || _audio == null) return;
            _audio.SetSfxVolume(value / 100f);
            _audio.PlayTestSfx();     // 효과음은 바로 들려 주어야 조절이 된다
            Refresh();
        }

        public void Refresh()
        {
            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc)) return;

            if (_titleText != null) _titleText.text = loc.Get(TitleTextId);

            int bgm = _audio != null ? Mathf.RoundToInt(_audio.BgmVolume * 100f) : 0;
            int sfx = _audio != null ? Mathf.RoundToInt(_audio.SfxVolume * 100f) : 0;

            if (_bgmLabel != null) _bgmLabel.text = loc.Get(BgmTextId, bgm);
            if (_sfxLabel != null) _sfxLabel.text = loc.Get(SfxTextId, sfx);
        }
    }
}
