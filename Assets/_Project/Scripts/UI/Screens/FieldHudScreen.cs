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

        protected override void OnOpen()
        {
            EventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);
            ShrinkCamera();
            Refresh();
        }

        protected override void OnClose()
        {
            EventBus.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);
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
