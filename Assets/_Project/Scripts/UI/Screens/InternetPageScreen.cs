using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 게시글 상세 화면. 제목 / 본문 / 검열 상태를 보여주고, 검열 가능하면 검열 버튼을 노출한다.
    ///
    /// 판정 자체는 하지 않는다. "검열할 수 있는가"와 "검열하면 무슨 일이 일어나는가"는
    /// InternetService와 CaseDirector가 정하고, 이 화면은 결과를 표시만 한다.
    /// </summary>
    public class InternetPageScreen : UIScreen
    {
        [Header("표시 대상")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private TMP_Text _statusText;

        [Tooltip("확산도 / 믿음도 표시줄.")]
        [SerializeField] private TMP_Text _statsText;

        [Tooltip("검열 결과 안내. 화면을 새로 열면 지워진다.")]
        [SerializeField] private TMP_Text _feedbackText;

        [Header("버튼")]
        [SerializeField] private Button _censorButton;

        private WebPageSO _page;
        private Func<WebPageSO, string> _statusProvider;
        private Func<WebPageSO, bool> _canCensorProvider;
        private Func<string> _statsProvider;
        private string _feedbackTextId;

        /// <summary>표시할 게시글과 상태 계산 방법을 넘긴다.</summary>
        public void Bind(WebPageSO page,
            Func<WebPageSO, string> statusProvider,
            Func<WebPageSO, bool> canCensorProvider,
            Func<string> statsProvider = null)
        {
            _page = page;
            _statusProvider = statusProvider;
            _canCensorProvider = canCensorProvider;
            _statsProvider = statsProvider;
            _feedbackTextId = null;   // 새 글을 열면 이전 안내는 지운다
            Refresh();
        }

        /// <summary>검열 결과를 알린다. String ID만 받는다.</summary>
        public void ShowFeedback(string textId)
        {
            _feedbackTextId = textId;
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

        /// <summary>검열 직후처럼 상태만 바뀐 경우에도 이걸 부르면 즉시 반영된다.</summary>
        public void Refresh()
        {
            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc)) return;

            if (_page == null)
            {
                if (_titleText != null) _titleText.text = loc.Get("ui.net.page_missing");
                if (_bodyText != null) _bodyText.text = string.Empty;
                if (_statusText != null) _statusText.text = string.Empty;
                if (_statsText != null) _statsText.text = string.Empty;
                if (_feedbackText != null) _feedbackText.text = string.Empty;
                if (_censorButton != null) _censorButton.gameObject.SetActive(false);
                return;
            }

            if (_titleText != null) _titleText.text = loc.Get(_page.TitleTextId);
            if (_bodyText != null) _bodyText.text = loc.Get(_page.BodyTextId);
            if (_statusText != null && _statusProvider != null) _statusText.text = _statusProvider(_page);
            if (_statsText != null) _statsText.text = _statsProvider != null ? _statsProvider() : string.Empty;
            if (_feedbackText != null)
            {
                _feedbackText.text = string.IsNullOrEmpty(_feedbackTextId) ? string.Empty : loc.Get(_feedbackTextId);
            }

            // 이미 검열한 글에는 버튼을 숨긴다.
            // 검열할 수 없는 글에는 버튼을 남겨 둔다. 눌러 봐야 잘못된 검열이라는 것을 알 수 있다.
            if (_censorButton != null)
            {
                bool showButton = _canCensorProvider == null || _canCensorProvider(_page);
                _censorButton.gameObject.SetActive(showButton);
            }
        }

        public WebPageSO CurrentPage => _page;
    }
}
