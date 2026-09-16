using System;
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
    ///   위 - 방을 옆에서 본 장면. 조사 지점이 그 안에 놓인다.
    ///   아래 - 검은 띠. 왼쪽에 인물 초상, 오른쪽에 지금 할 말 한 줄.
    /// 장면 맨 위 가운데에는 지금 상황(시간 / 확산 / 단서)이 한 줄로 흘러간다.
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
        [Tooltip("초상 자리. 실제 그림이 생기면 이 Image만 갈아 끼우면 된다.")]
        [SerializeField] private Image _portrait;

        [SerializeField] private TMP_Text _speakerText;
        [SerializeField] private TMP_Text _lineText;

        [Tooltip("튜토리얼 대사를 넘기는 버튼. 화면 전체를 덮는다. 평소에는 꺼 둔다.")]
        [SerializeField] private GameObject _advanceRoot;

        [SerializeField] private Button _advanceButton;

        private Func<string> _tickerProvider;
        private Func<string> _lineProvider;
        private string _speakerTextId;

        private Camera _camera;
        private Rect _cameraRectBefore;
        private bool _cameraChanged;

        /// <summary>
        /// 화면에 걸 것을 정한다.
        /// 문구는 만드는 방법(Func)으로 받는다. 언어가 바뀌거나 값이 변해도 다시 조립된다.
        /// </summary>
        public void Bind(Func<string> ticker, string speakerTextId, Func<string> line)
        {
            _tickerProvider = ticker;
            _speakerTextId = speakerTextId;
            _lineProvider = line;
            Refresh();
        }

        /// <summary>장면 맨 위 한 줄만 갈아 끼운다. 열차가 들어오는 것 같은 알림에 쓴다.</summary>
        public void SetTicker(Func<string> ticker)
        {
            _tickerProvider = ticker;
            Refresh();
        }

        /// <summary>
        /// 튜토리얼이 현장에서 한 줄 말하게 한다.
        ///
        /// 화면 전체를 덮는 버튼이 함께 켜진다. 그 동안에는 조사 지점도 버튼도 눌리지 않는다.
        /// 현장 입력은 "UI 위를 눌렀는가"를 보고 걸러지므로 이 버튼 하나로 둘 다 막힌다.
        /// </summary>
        public void ShowTutorialLine(string speakerTextId, Func<string> line, Action onAdvance)
        {
            _speakerTextId = speakerTextId;
            _lineProvider = line;

            if (_advanceButton != null)
            {
                _advanceButton.onClick.RemoveAllListeners();
                if (onAdvance != null) _advanceButton.onClick.AddListener(() => onAdvance());
            }

            if (_advanceRoot != null) _advanceRoot.SetActive(true);
            Refresh();
        }

        /// <summary>튜토리얼 대사를 끝내고 평소 현장으로 돌려놓는다.</summary>
        public void EndTutorialLines()
        {
            if (_advanceButton != null) _advanceButton.onClick.RemoveAllListeners();
            if (_advanceRoot != null) _advanceRoot.SetActive(false);
        }

        protected override void OnOpen()
        {
            EventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);
            ShrinkCamera();
            Refresh();
        }

        protected override void OnClose()
        {
            EventBus.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);
            EndTutorialLines();
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
