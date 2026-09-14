using TMPro;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// TMP 텍스트를 문자열 ID에 묶는다.
    /// UI에는 문장을 직접 적지 않고 이 컴포넌트에 ID만 지정한다.
    /// 언어가 바뀌면 스스로 다시 채운다.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public class LocalizedText : MonoBehaviour
    {
        [Tooltip("Resources/Localization 테이블의 ID. 예: ui.common.ok")]
        [SerializeField] private string _textId;

        private TMP_Text _target;
        private object[] _formatArgs;

        public string TextId => _textId;

        private void Awake()
        {
            _target = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);
            Refresh();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);
        }

        /// <summary>표시할 ID를 바꾼다.</summary>
        public void SetTextId(string textId, params object[] args)
        {
            _textId = textId;
            _formatArgs = args != null && args.Length > 0 ? args : null;
            Refresh();
        }

        /// <summary>현재 언어로 다시 채운다.</summary>
        public void Refresh()
        {
            if (_target == null) _target = GetComponent<TMP_Text>();
            if (_target == null || string.IsNullOrEmpty(_textId)) return;

            if (!ServiceRegistry.TryGet<LocalizationService>(out var localization))
            {
                // 부팅 전에 활성화된 경우. 부팅 후 언어 이벤트로 다시 채워진다.
                return;
            }

            _target.text = _formatArgs == null
                ? localization.Get(_textId)
                : localization.Get(_textId, _formatArgs);
        }

        private void OnLanguageChanged(LanguageChangedEvent evt)
        {
            Refresh();
        }
    }
}
