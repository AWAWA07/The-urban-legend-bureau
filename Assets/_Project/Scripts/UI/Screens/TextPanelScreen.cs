using TMPro;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 제목 + 본문 + 보조줄로 이루어진 단순 화면.
    /// 이번 Vertical Slice의 화면들이 모두 같은 모양이라 하나로 묶었다.
    ///
    /// 표시 문자열은 전부 String ID로 받는다. 코드에 문장을 적지 않는다.
    /// </summary>
    public class TextPanelScreen : UIScreen
    {
        [Header("표시 대상")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private TMP_Text _footerText;

        private string _titleId;
        private string _bodyId;
        private string _footerId;
        private System.Func<string> _bodyProvider;

        /// <summary>String ID로 내용을 채운다.</summary>
        public void Bind(string titleTextId, string bodyTextId, string footerTextId = null)
        {
            _titleId = titleTextId;
            _bodyId = bodyTextId;
            _footerId = footerTextId;
            _bodyProvider = null;
            Refresh();
        }

        /// <summary>
        /// 본문을 여러 String ID를 조합해 만드는 경우에 사용한다.
        /// 완성된 문자열이 아니라 '만드는 방법'을 받아 두어야 언어가 바뀔 때 다시 조립할 수 있다.
        /// </summary>
        public void BindWithBodyProvider(string titleTextId, System.Func<string> bodyProvider, string footerTextId = null)
        {
            _titleId = titleTextId;
            _bodyId = null;
            _bodyProvider = bodyProvider;
            _footerId = footerTextId;
            Refresh();
        }

        protected override void OnOpen()
        {
            EventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);
            Refresh();
        }

        protected override void OnClose()
        {
            EventBus.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);
        }

        private void OnLanguageChanged(LanguageChangedEvent evt)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc)) return;

            if (_titleText != null) _titleText.text = string.IsNullOrEmpty(_titleId) ? string.Empty : loc.Get(_titleId);

            if (_bodyText != null)
            {
                if (_bodyProvider != null) _bodyText.text = _bodyProvider();
                else _bodyText.text = string.IsNullOrEmpty(_bodyId) ? string.Empty : loc.Get(_bodyId);
            }

            if (_footerText != null) _footerText.text = string.IsNullOrEmpty(_footerId) ? string.Empty : loc.Get(_footerId);
        }
    }
}
