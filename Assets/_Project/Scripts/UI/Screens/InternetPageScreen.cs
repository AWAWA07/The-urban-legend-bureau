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
        private const string CommentHeaderTextId = "ui.net.comment_header";
        private const string NoCommentTextId = "ui.net.no_comment";
        private const string AuthorMarkTextId = "ui.net.author_mark";

        [Header("표시 대상")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _bodyText;
        [Tooltip("본문 아래에 붙는 댓글 묶음. 한 덩어리 글로 조립해 넣는다.")]
        [SerializeField] private TMP_Text _commentsText;

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
        private Func<string> _feedbackProvider;

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
            _feedbackProvider = null;   // 새 글을 열면 이전 안내는 지운다
            Refresh();
        }

        /// <summary>검열 결과를 알린다. String ID만 받는다.</summary>
        public void ShowFeedback(string textId)
        {
            if (string.IsNullOrEmpty(textId)) { _feedbackProvider = null; }
            else
            {
                var id = textId;
                _feedbackProvider = () =>
                    ServiceRegistry.TryGet<LocalizationService>(out var loc) ? loc.Get(id) : string.Empty;
            }
            Refresh();
        }

        /// <summary>
        /// 수치 변화처럼 조립이 필요한 안내를 알린다.
        /// 완성된 문자열이 아니라 만드는 방법을 받아 두어야 언어가 바뀔 때 다시 조립된다.
        ///
        /// 한 줄은 잠깐 떴다가 옅어지며 사라진다. 방금 한 일의 결과라는 것이 분명해야 한다.
        /// </summary>
        public void ShowFeedbackProvider(Func<string> provider)
        {
            _feedbackProvider = provider;
            Refresh();

            if (_feedbackFade != null) { StopCoroutine(_feedbackFade); _feedbackFade = null; }
            if (provider == null || _feedbackText == null || !isActiveAndEnabled) return;

            UiFade.Show(_feedbackText.gameObject);
            _feedbackFade = StartCoroutine(UiFade.Play(_feedbackText.gameObject, () =>
            {
                _feedbackFade = null;
                _feedbackProvider = null;
                if (_feedbackText != null) _feedbackText.text = string.Empty;
            }));
        }

        private Coroutine _feedbackFade;

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
                if (_commentsText != null) _commentsText.text = string.Empty;
                if (_statusText != null) _statusText.text = string.Empty;
                if (_statsText != null) _statsText.text = string.Empty;
                if (_feedbackText != null) _feedbackText.text = string.Empty;
                if (_censorButton != null) _censorButton.gameObject.SetActive(false);
                return;
            }

            if (_titleText != null) _titleText.text = loc.Get(_page.TitleTextId);
            if (_bodyText != null) _bodyText.text = loc.Get(_page.BodyTextId);
            if (_commentsText != null) _commentsText.text = BuildComments(loc);
            if (_statusText != null && _statusProvider != null) _statusText.text = _statusProvider(_page);
            if (_statsText != null) _statsText.text = _statsProvider != null ? _statsProvider() : string.Empty;
            if (_feedbackText != null)
            {
                _feedbackText.text = _feedbackProvider != null ? _feedbackProvider() : string.Empty;
            }

            // 이미 검열한 글에는 버튼을 숨긴다.
            // 검열할 수 없는 글에는 버튼을 남겨 둔다. 눌러 봐야 잘못된 검열이라는 것을 알 수 있다.
            if (_censorButton != null)
            {
                bool showButton = _canCensorProvider == null || _canCensorProvider(_page);
                _censorButton.gameObject.SetActive(showButton);
            }
        }


        /// <summary>
        /// 본문 아래에 붙일 댓글 덩어리를 만든다.
        ///
        /// 댓글은 게시글마다 개수가 다르고 길이도 들쭉날쭉하다. 줄마다 오브젝트를 만들어 두면
        /// 화면 하나가 글마다 다른 높이로 흔들리므로, 여기서는 글 한 덩어리로 조립해 넘긴다.
        /// 굴러가는 것은 본문과 댓글을 함께 담은 바깥 스크롤이다.
        /// </summary>
        private string BuildComments(LocalizationService loc)
        {
            var comments = _page != null ? _page.Comments : null;
            int count = 0;
            if (comments != null)
            {
                for (int i = 0; i < comments.Count; i++)
                {
                    if (comments[i] != null && !string.IsNullOrEmpty(comments[i].BodyTextId)) count++;
                }
            }

            var sb = new System.Text.StringBuilder();
            sb.Append("<color=#6B7280>").Append(loc.Get(CommentHeaderTextId, count)).Append("</color>");

            if (count == 0)
            {
                sb.Append('\n').Append("<color=#6B7280>").Append(loc.Get(NoCommentTextId)).Append("</color>");
                return sb.ToString();
            }

            for (int i = 0; i < comments.Count; i++)
            {
                var comment = comments[i];
                if (comment == null || string.IsNullOrEmpty(comment.BodyTextId)) continue;

                // 작성자는 작고 흐리게, 내용은 그대로. 커뮤니티 댓글이 보이는 모양 그대로 둔다.
                sb.Append("\n\n<size=82%><color=#6B7280>").Append(loc.Get(comment.AuthorTextId));
                if (comment.IsAuthor)
                {
                    sb.Append(" <color=#DDB86E>").Append(loc.Get(AuthorMarkTextId)).Append("</color>");
                }
                sb.Append("</color></size>\n").Append(loc.Get(comment.BodyTextId));
            }

            return sb.ToString();
        }

        public WebPageSO CurrentPage => _page;
    }
}
