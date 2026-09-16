using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 현장 조사 화면.
    ///
    /// 화면을 둘로 나눈다.
    ///   위 - 장소를 옆에서 본 장면. 조사 지점이 그 안에 놓인다.
    ///   아래 - 검은 띠. 말하는 사람이 있을 때만 초상과 대사가 나오고, 없으면 버튼만 남는다.
    /// 장면 맨 위 가운데에는 지금 상황(시간 / 확산 / 단서)이 한 줄로 흘러간다.
    /// 장면 오른쪽에는 늘 누를 수 있는 휴대폰 단추가 있다.
    ///
    /// 장면을 위쪽만 쓰게 하려고 카메라가 그리는 자리를 줄인다.
    /// 화면을 닫을 때 원래대로 되돌린다. 다른 화면은 카메라를 그대로 쓴다.
    /// </summary>
    public class FieldHudScreen : UIScreen
    {
        [Header("위: 장면")]
        [Tooltip("장면이 화면에서 차지하는 세로 비율. 나머지는 아래 대사 띠가 쓴다.")]
        [SerializeField] private float _sceneHeightRatio = 2f / 3f;

        [Tooltip("장면 맨 위 가운데에 흘러가는 한 줄.")]
        [SerializeField] private TMP_Text _tickerText;

        [Header("아래: 대사 띠")]
        [Tooltip("초상과 이름과 대사를 묶은 것. 말하는 사람이 없으면 통째로 꺼진다.")]
        [SerializeField] private GameObject _speechRoot;

        [Tooltip("초상 자리. 실제 그림이 생기면 이 Image만 갈아 끼우면 된다.")]
        [SerializeField] private Image _portrait;

        [SerializeField] private TMP_Text _speakerText;
        [SerializeField] private TMP_Text _lineText;

        [Tooltip("대사를 넘기는 버튼. 화면 전체를 덮는다. 평소에는 꺼 둔다.")]
        [SerializeField] private GameObject _advanceRoot;

        [SerializeField] private Button _advanceButton;

        [Header("휴대폰")]
        [Tooltip("장면 오른쪽에 늘 떠 있는 작은 단추.")]
        [SerializeField] private Button _phoneButton;

        [Tooltip("눌렀을 때 오른쪽에 펴지는 휴대폰 화면.")]
        [SerializeField] private GameObject _phonePanel;

        [SerializeField] private Button _phoneCloseButton;
        [SerializeField] private TMP_Text _phoneClockText;

        [Tooltip("휴대폰 단추의 이름표.")]
        [SerializeField] private TMP_Text _phoneButtonLabel;

        [Tooltip("휴대폰 안의 앱. 컴퓨터 바탕화면과 같은 것들을 세로로 늘어놓는다.")]
        [SerializeField] private List<DesktopIcon> _phoneApps = new List<DesktopIcon>();

        private const string PhoneClockTextId = "ui.phone.clock";
        private const string PhoneButtonTextId = "ui.field.btn_phone";

        private Func<string> _tickerProvider;
        private Func<string> _lineProvider;
        private string _speakerTextId;

        private Camera _camera;
        private Rect _cameraRectBefore;
        private bool _cameraChanged;

        protected override void Awake()
        {
            base.Awake();
            SetupPhone();
        }

        /// <summary>
        /// 화면 위쪽에 거는 것만 정한다.
        /// 말하는 사람은 따로 정한다. 평소에는 아무도 말하지 않으므로 띠에 버튼만 남는다.
        /// </summary>
        public void Bind(Func<string> ticker)
        {
            _tickerProvider = ticker;
            ClearSpeech();
            Refresh();
        }

        /// <summary>장면 맨 위 한 줄만 갈아 끼운다. 열차가 들어오는 것 같은 알림에 쓴다.</summary>
        public void SetTicker(Func<string> ticker)
        {
            _tickerProvider = ticker;
            Refresh();
        }

        /// <summary>
        /// 현장에서 한 줄 말하게 한다.
        ///
        /// onAdvance 를 주면 화면 전체를 덮는 버튼이 함께 켜진다.
        /// 그 동안에는 조사 지점도 버튼도 눌리지 않는다.
        /// 현장 입력은 "UI 위를 눌렀는가"를 보고 걸러지므로 이 버튼 하나로 둘 다 막힌다.
        /// </summary>
        public void ShowLine(string speakerTextId, Func<string> line, Action onAdvance = null)
        {
            _speakerTextId = speakerTextId;
            _lineProvider = line;

            if (_speechRoot != null) _speechRoot.SetActive(true);

            if (_advanceButton != null)
            {
                _advanceButton.onClick.RemoveAllListeners();
                if (onAdvance != null) _advanceButton.onClick.AddListener(() => onAdvance());
            }

            if (_advanceRoot != null) _advanceRoot.SetActive(onAdvance != null);
            Refresh();
        }

        /// <summary>말하는 사람을 치운다. 띠에는 버튼만 남는다.</summary>
        public void ClearSpeech()
        {
            _speakerTextId = null;
            _lineProvider = null;

            if (_advanceButton != null) _advanceButton.onClick.RemoveAllListeners();
            if (_advanceRoot != null) _advanceRoot.SetActive(false);
            if (_speechRoot != null) _speechRoot.SetActive(false);
        }

        // ------------------------------------------------------------- 휴대폰

        /// <summary>앱을 눌렀을 때 부를 것. 무엇이 열리는지는 밖에서 정한다.</summary>
        private Action<string> _onApp;

        /// <summary>
        /// 휴대폰 단추를 연결한다.
        /// 앱을 눌렀을 때 무엇이 열리는지는 BindPhoneApps 로 밖에서 정한다.
        /// </summary>
        private void SetupPhone()
        {
            if (_phoneButton != null)
            {
                _phoneButton.onClick.RemoveAllListeners();
                _phoneButton.onClick.AddListener(TogglePhone);
            }

            if (_phoneCloseButton != null)
            {
                _phoneCloseButton.onClick.RemoveAllListeners();
                _phoneCloseButton.onClick.AddListener(() => SetPhoneOpen(false));
            }

            for (int i = 0; i < _phoneApps.Count; i++)
            {
                var app = _phoneApps[i];
                if (app == null || app.button == null) continue;

                var captured = app.appId;
                app.button.onClick.RemoveAllListeners();
                app.button.onClick.AddListener(() => _onApp?.Invoke(captured));
            }

            SetPhoneOpen(false);
        }

        /// <summary>휴대폰 안의 앱을 눌렀을 때 부를 것을 정한다.</summary>
        public void BindPhoneApps(Action<string> onApp)
        {
            _onApp = onApp;
        }

        private void TogglePhone()
        {
            SetPhoneOpen(_phonePanel == null || !_phonePanel.activeSelf);
        }

        /// <summary>휴대폰을 펴고 접는다.</summary>
        public void SetPhoneOpen(bool open)
        {
            if (_phonePanel != null) _phonePanel.SetActive(open);
        }

        // ------------------------------------------------------------- 화면

        protected override void OnOpen()
        {
            EventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);
            ShrinkCamera();
            Refresh();
        }

        protected override void OnClose()
        {
            EventBus.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);
            ClearSpeech();
            SetPhoneOpen(false);
            RestoreCamera();
        }

        private void OnLanguageChanged(LanguageChangedEvent evt)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc)) return;

            if (_tickerText != null) _tickerText.text = _tickerProvider != null ? _tickerProvider() : string.Empty;
            if (_lineText != null) _lineText.text = _lineProvider != null ? _lineProvider() : string.Empty;

            if (_speakerText != null)
            {
                _speakerText.text = string.IsNullOrEmpty(_speakerTextId) ? string.Empty : loc.Get(_speakerTextId);
            }

            if (_phoneClockText != null) _phoneClockText.text = loc.Get(PhoneClockTextId);
            if (_phoneButtonLabel != null) _phoneButtonLabel.text = loc.Get(PhoneButtonTextId);

            for (int i = 0; i < _phoneApps.Count; i++)
            {
                var app = _phoneApps[i];
                if (app == null || app.label == null) continue;
                app.label.text = loc.Get(app.labelTextId);
            }
        }

        // ------------------------------------------------------------- 카메라

        /// <summary>장면이 위쪽만 쓰도록 카메라가 그리는 자리를 줄인다.</summary>
        private void ShrinkCamera()
        {
            if (_cameraChanged) return;

            _camera = Camera.main;
            if (_camera == null) return;

            _cameraRectBefore = _camera.rect;

            float height = Mathf.Clamp01(_sceneHeightRatio);
            _camera.rect = new Rect(0f, 1f - height, 1f, height);
            _cameraChanged = true;
        }

        private void RestoreCamera()
        {
            if (!_cameraChanged || _camera == null) return;

            _camera.rect = _cameraRectBefore;
            _cameraChanged = false;
        }
    }
}
