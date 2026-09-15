using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.UI
{
    /// <summary>화면에 붙는 댓글 한 줄. 판정에 쓰이는 데이터는 들고 있지 않다.</summary>
    public class CommunityComment
    {
        public string AuthorTextId;
        public string BodyTextId;

        /// <summary>플레이어가 단 댓글인가. 표시를 다르게 한다.</summary>
        public bool IsPlayer;
    }

    /// <summary>
    /// 인터넷 커뮤니티 게시글 화면.
    ///
    /// 보여주는 내용은 기존 WebPageSO 그대로다. 새 인터넷 데이터 구조를 만들지 않았다.
    /// 게시판 이름 / 조회수 / 작성 시간처럼 커뮤니티답게 보이기 위한 곁가지만 이 화면이 붙인다.
    ///
    /// 댓글 선택지는 목록 화면들과 같은 방식으로 템플릿을 복제해 만든다.
    /// 무엇을 보여줄지, 고르면 무슨 일이 생기는지는 전부 밖에서 정한다.
    /// </summary>
    public class CommunityPageScreen : UIScreen
    {
        [Header("머리말")]
        [SerializeField] private TMP_Text _siteText;
        [SerializeField] private TMP_Text _boardText;

        [Header("게시글")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _metaText;
        [SerializeField] private TMP_Text _bodyText;

        [Header("댓글")]
        [SerializeField] private TMP_Text _commentHeaderText;
        [SerializeField] private RectTransform _commentRoot;
        [SerializeField] private TMP_Text _commentTemplate;

        [Header("댓글 선택지")]
        [SerializeField] private TMP_Text _choiceHeaderText;
        [SerializeField] private RectTransform _choiceRoot;
        [SerializeField] private Button _choiceTemplate;

        [Tooltip("선택 결과 안내.")]
        [SerializeField] private TMP_Text _noticeText;

        private readonly List<GameObject> _spawnedComments = new List<GameObject>();
        private readonly List<GameObject> _spawnedChoices = new List<GameObject>();

        private const string SiteTextId = "ui.net.site_name";
        private const string BoardTextId = "ui.net.board_free";
        private const string MetaTextId = "ui.net.post_meta";
        private const string CommentHeaderTextId = "ui.net.comment_header";
        private const string ChoiceHeaderTextId = "ui.net.choice_header";

        private WebPageSO _page;
        private int _views;
        private string _postTimeTextId;
        private List<CommunityComment> _comments = new List<CommunityComment>();
        private IReadOnlyList<TutorialCommentChoice> _choices;
        private Func<TutorialCommentChoice, string> _choiceLabelProvider;
        private Action<TutorialCommentChoice> _onChoice;
        private Func<string> _noticeProvider;

        /// <summary>게시글을 건다. 조회수와 작성 시각은 화면에 보이기 위한 값이다.</summary>
        public void BindPage(WebPageSO page, int views, string postTimeTextId)
        {
            _page = page;
            _views = views;
            _postTimeTextId = postTimeTextId;
            Refresh();
        }

        /// <summary>댓글 목록을 갈아 끼운다.</summary>
        public void BindComments(List<CommunityComment> comments)
        {
            _comments = comments ?? new List<CommunityComment>();
            Refresh();
        }

        /// <summary>댓글 선택지를 건다. null이나 빈 목록을 주면 선택 영역이 사라진다.</summary>
        public void BindChoices(IReadOnlyList<TutorialCommentChoice> choices,
            Func<TutorialCommentChoice, string> labelProvider,
            Action<TutorialCommentChoice> onChoice)
        {
            _choices = choices;
            _choiceLabelProvider = labelProvider;
            _onChoice = onChoice;
            Refresh();
        }

        /// <summary>선택 결과 안내. 언어가 바뀌어도 다시 조립되도록 만드는 방법을 받는다.</summary>
        public void ShowNotice(Func<string> provider)
        {
            _noticeProvider = provider;
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

        public void Refresh()
        {
            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc)) return;

            if (_siteText != null) _siteText.text = loc.Get(SiteTextId);
            if (_boardText != null) _boardText.text = loc.Get(BoardTextId);

            if (_titleText != null) _titleText.text = _page != null ? loc.Get(_page.TitleTextId) : string.Empty;
            if (_bodyText != null) _bodyText.text = _page != null ? loc.Get(_page.BodyTextId) : string.Empty;

            if (_metaText != null)
            {
                _metaText.text = _page == null
                    ? string.Empty
                    : loc.Get(MetaTextId, loc.Get("ui.net.author_anon"), _views, _comments.Count,
                        string.IsNullOrEmpty(_postTimeTextId) ? string.Empty : loc.Get(_postTimeTextId));
            }

            if (_commentHeaderText != null) _commentHeaderText.text = loc.Get(CommentHeaderTextId, _comments.Count);
            if (_choiceHeaderText != null)
            {
                bool hasChoices = _choices != null && _choices.Count > 0;
                _choiceHeaderText.text = hasChoices ? loc.Get(ChoiceHeaderTextId) : string.Empty;
            }

            if (_noticeText != null) _noticeText.text = _noticeProvider != null ? _noticeProvider() : string.Empty;

            RebuildComments(loc);
            RebuildChoices();
        }

        // ------------------------------------------------------------- 목록

        private void RebuildComments(LocalizationService loc)
        {
            if (_commentRoot == null || _commentTemplate == null) return;

            ClearSpawned(_spawnedComments);

            for (int i = 0; i < _comments.Count; i++)
            {
                var comment = _comments[i];
                if (comment == null) continue;

                var item = Instantiate(_commentTemplate, _commentRoot);
                item.gameObject.name = "Comment_" + i;
                item.gameObject.SetActive(true);
                item.text = loc.Get(comment.AuthorTextId) + "\n" + loc.Get(comment.BodyTextId);

                _spawnedComments.Add(item.gameObject);
            }
        }

        private void RebuildChoices()
        {
            if (_choiceRoot == null || _choiceTemplate == null) return;

            ClearSpawned(_spawnedChoices);
            if (_choices == null) return;

            for (int i = 0; i < _choices.Count; i++)
            {
                var choice = _choices[i];
                if (choice == null) continue;

                var item = Instantiate(_choiceTemplate, _choiceRoot);
                item.gameObject.name = "Choice_" + choice.ChoiceId;
                item.gameObject.SetActive(true);

                var label = item.GetComponentInChildren<TMP_Text>(true);
                if (label != null && _choiceLabelProvider != null) label.text = _choiceLabelProvider(choice);

                var captured = choice;
                item.onClick.RemoveAllListeners();
                item.onClick.AddListener(() => _onChoice?.Invoke(captured));

                _spawnedChoices.Add(item.gameObject);
            }
        }

        /// <summary>
        /// 이전에 만든 항목을 치운다.
        /// Destroy는 프레임 끝에 처리되므로, 목록에서 떼어 내고 꺼 둔 뒤 파괴한다.
        /// 같은 프레임에 다시 그려도 옛 항목이 남지 않는다.
        /// </summary>
        private void ClearSpawned(List<GameObject> spawned)
        {
            for (int i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] == null) continue;

                spawned[i].transform.SetParent(null, false);
                spawned[i].SetActive(false);
                Destroy(spawned[i]);
            }
            spawned.Clear();
        }
    }
}
