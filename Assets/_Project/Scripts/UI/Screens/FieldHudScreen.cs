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

        [Tooltip("고를 것을 늘어놓는 자리. 대사 대신 여기에 선택지가 뜬다.")]
        [SerializeField] private RectTransform _choiceRoot;

        [Tooltip("복제할 선택지 한 칸.")]
        [SerializeField] private Button _choiceTemplate;

        [Tooltip("선택지가 떠 있는 동안 장면을 못 누르게 덮는 판. 대사 띠보다 뒤에 깔린다.")]
        [SerializeField] private GameObject _blockRoot;

        [Header("휴대폰")]
        [Tooltip("장면 오른쪽에 늘 떠 있는 작은 단추.")]
        [SerializeField] private Button _phoneButton;

        [Tooltip("눌렀을 때 오른쪽에 펴지는 휴대폰 화면.")]
        [SerializeField] private GameObject _phonePanel;

        [Tooltip("껍데기 안쪽의 화면. 앱을 열면 이 자리에 앱이 켜진다.")]
        [SerializeField] private RectTransform _phoneScreen;

        [SerializeField] private Button _phoneCloseButton;
        [SerializeField] private TMP_Text _phoneClockText;

        [Tooltip("휴대폰 단추의 이름표.")]
        [SerializeField] private TMP_Text _phoneButtonLabel;

        [Tooltip("휴대폰 안의 앱. 컴퓨터 바탕화면과 같은 것들을 세로로 늘어놓는다.")]
        [SerializeField] private List<DesktopIcon> _phoneApps = new List<DesktopIcon>();

        private Func<string> _tickerProvider;
        private Func<string> _lineProvider;
        private string _speakerTextId;

        /// <summary>
        /// 지금 현장에서 누가 말하고 있는가.
        ///
        /// 말이 오가는 동안에는 걸어 다니지 않는다. 듣는 중에 자리를 옮기면
        /// 대사가 걸려 있는 자리와 서 있는 자리가 어긋난다.
        /// 현장 화면은 한 번에 하나뿐이라 이 값도 하나로 둔다. 걷는 쪽(FieldWalker)이 여기를 본다.
        /// </summary>
        public static bool IsSpeaking { get; private set; }

        /// <summary>
        /// 지금 현장이 앞에 나와 있는가.
        ///
        /// 규칙 추론이나 조사 방법 목록이 위에 뜨면 현장은 그 뒤에 남는다.
        /// 그 동안에도 걸으면, 화면 뒤에서 사람이 움직여 돌아왔을 때 엉뚱한 자리에 서 있다.
        /// 그래서 덮이면 걸음을 멈춘다. 걷는 쪽(FieldWalker)이 여기를 본다.
        /// </summary>
        public static bool IsFront { get; private set; }

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
            IsSpeaking = true;

            // 앞서 고를 것이 떠 있었다면 치운다. 대사와 선택지가 같은 자리를 쓴다.
            ClearSpawnedChoices();
            if (_choiceRoot != null) _choiceRoot.gameObject.SetActive(false);
            if (_lineText != null) _lineText.gameObject.SetActive(true);
            if (_blockRoot != null) _blockRoot.SetActive(false);

            if (_speechRoot != null) _speechRoot.SetActive(true);

            if (_advanceButton != null)
            {
                _advanceButton.onClick.RemoveAllListeners();
                if (onAdvance != null) _advanceButton.onClick.AddListener(() => onAdvance());
            }

            if (_advanceRoot != null) _advanceRoot.SetActive(onAdvance != null);
            Refresh();
        }

        /// <summary>
        /// 고를 것을 띠에 늘어놓는다. 대사 자리에 선택지가 대신 선다.
        ///
        /// 고르는 동안에는 장면이 눌리지 않게 덮어 둔다. 그러지 않으면 답을 고르다 말고
        /// 엉뚱한 곳을 조사하게 된다.
        /// </summary>
        public void ShowChoices(string speakerTextId, IReadOnlyList<Func<string>> labels, Action<int> onPick)
        {
            _speakerTextId = speakerTextId;
            _lineProvider = null;
            IsSpeaking = true;

            ClearSpawnedChoices();

            if (_speechRoot != null) _speechRoot.SetActive(true);
            if (_advanceRoot != null) _advanceRoot.SetActive(false);
            if (_blockRoot != null) _blockRoot.SetActive(true);

            if (_choiceRoot != null) _choiceRoot.gameObject.SetActive(true);
            if (_lineText != null) _lineText.gameObject.SetActive(false);

            if (_choiceRoot == null || _choiceTemplate == null || labels == null) return;

            for (int i = 0; i < labels.Count; i++)
            {
                var item = Instantiate(_choiceTemplate, _choiceRoot);
                item.gameObject.name = "Choice_" + i;
                item.gameObject.SetActive(true);

                var label = item.GetComponentInChildren<TMP_Text>(true);
                if (label != null && labels[i] != null) label.text = labels[i]();

                int picked = i;
                item.onClick.RemoveAllListeners();
                item.onClick.AddListener(() => onPick?.Invoke(picked));

                _spawnedChoices.Add(item.gameObject);
            }

            Refresh();
        }

        private readonly List<GameObject> _spawnedChoices = new List<GameObject>();

        private void ClearSpawnedChoices()
        {
            for (int i = 0; i < _spawnedChoices.Count; i++)
            {
                if (_spawnedChoices[i] != null) Destroy(_spawnedChoices[i]);
            }
            _spawnedChoices.Clear();
        }

        /// <summary>말하는 사람을 치운다. 띠에는 버튼만 남는다.</summary>
        public void ClearSpeech()
        {
            _speakerTextId = null;
            _lineProvider = null;
            IsSpeaking = false;

            ClearSpawnedChoices();

            if (_advanceButton != null) _advanceButton.onClick.RemoveAllListeners();
            if (_advanceRoot != null) _advanceRoot.SetActive(false);
            if (_blockRoot != null) _blockRoot.SetActive(false);
            if (_speechRoot != null) _speechRoot.SetActive(false);
            if (_choiceRoot != null) _choiceRoot.gameObject.SetActive(false);
            if (_lineText != null) _lineText.gameObject.SetActive(true);
        }

        // ------------------------------------------------------------- 휴대폰

        /// <summary>앱을 눌렀을 때 부를 것. 무엇이 열리는지는 밖에서 정한다.</summary>
        private Action<string> _onApp;

        /// <summary>휴대폰을 넣을 때 부를 것. 안에 앱이 켜져 있으면 그 앱도 닫아야 한다.</summary>
        private Action _onPhonePutAway;

        private UrbanLegendBureau.InputSystemLayer.InputService _input;

        /// <summary>
        /// 휴대폰은 키로 꺼내고 넣는다.
        ///
        /// 화면에 안내를 따로 두지 않는다. 주머니에서 꺼내는 일에 이름표가 붙어 있지는 않다.
        /// 어느 키인지는 Player/Phone 액션이 들고 있다.
        /// </summary>
        private void Update()
        {
            if (!IsOpen) return;

            if (_input == null && !ServiceRegistry.TryGet(out _input)) return;
            if (!_input.IsReady || !_input.PhonePressed) return;

            TogglePhone();
        }

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

        /// <summary>
        /// 휴대폰 안의 앱을 눌렀을 때 부를 것을 정한다.
        /// onPutAway 는 휴대폰을 넣을 때 부른다. 켜 둔 앱을 그쪽에서 닫는다.
        /// </summary>
        public void BindPhoneApps(Action<string> onApp, Action onPutAway = null)
        {
            _onApp = onApp;
            _onPhonePutAway = onPutAway;
        }

        /// <summary>
        /// 껍데기 안쪽 화면의 자리. 앱은 휴대폰을 키우지 않고 이 자리에 켜진다.
        /// 여는 쪽이 이 칸에 제 화면을 맞춘다.
        /// </summary>
        public RectTransform PhoneScreenRect => _phoneScreen;

        private void TogglePhone()
        {
            SetPhoneOpen(_phonePanel == null || !_phonePanel.activeSelf);
        }

        /// <summary>휴대폰을 펴고 접는다. 접을 때는 안에 켜 둔 앱도 함께 닫는다.</summary>
        public void SetPhoneOpen(bool open)
        {
            bool wasOpen = _phonePanel != null && _phonePanel.activeSelf;

            if (_phonePanel != null) _phonePanel.SetActive(open);

            // 넣은 휴대폰 위에 앱만 떠 있으면 앱이 공중에 뜬 꼴이 된다.
            if (wasOpen && !open) _onPhonePutAway?.Invoke();
        }

        // ------------------------------------------------------------- 화면

        protected override void OnOpen()
        {
            EventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);
            IsFront = true;
            ShrinkCamera();
            Refresh();
        }

        protected override void OnClose()
        {
            EventBus.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);
            IsFront = false;
            ClearSpeech();
            SetPhoneOpen(false);
            RestoreCamera();
        }

        /// <summary>다른 화면이 위에 뜨면 현장은 뒤로 물러난다. 그 동안에는 걸음을 멈춘다.</summary>
        protected override void OnCoveredChanged(bool covered)
        {
            IsFront = !covered;
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

            // 휴대폰 시계는 ClockLabel 이 스스로 쓴다. 여기서 덮어쓰면 멈춘 시각으로 되돌아간다.
            // 휴대폰 단추에는 글자를 두지 않는다. 껍데기 모양만으로 무엇인지 알아본다.
            if (_phoneButtonLabel != null) _phoneButtonLabel.text = string.Empty;

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
