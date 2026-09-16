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
    /// <summary>게시판 목록에 걸리는 글 한 줄.</summary>
    public class CommunityBoardEntry
    {
        public string TitleTextId;
        public string MetaTextId;

        /// <summary>인기글 표시를 붙일 것인가.</summary>
        public bool IsHot;

        /// <summary>눌러서 열 수 있는 글인가. 배경을 채우는 줄은 열리지 않는다.</summary>
        public bool Openable;

        /// <summary>열었을 때 보여줄 글. Openable이 아니면 비어 있어도 된다.</summary>
        public WebPageSO Page;
    }

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
        [Header("창 틀")]
        [Tooltip("창 제목. 브라우저 제목 표시줄처럼 쓴다.")]
        [SerializeField] private TMP_Text _windowTitleText;

        [Tooltip("창 닫기 버튼.")]
        [SerializeField] private Button _closeButton;

        [Header("보기 전환")]
        [Tooltip("게시판 목록 영역 전체.")]
        [SerializeField] private GameObject _boardView;

        [Tooltip("글 하나를 펼친 영역 전체.")]
        [SerializeField] private GameObject _postView;

        [Header("게시판 목록")]
        [SerializeField] private RectTransform _boardRoot;

        [Tooltip("목록 굴림판. 목록을 열 때마다 맨 위로 돌린다.")]
        [SerializeField] private ScrollRect _boardScroll;

        [SerializeField] private Button _boardEntryTemplate;

        [Header("머리말")]
        [Tooltip("사이트 이름. 목록용과 글용 머리말이 따로 있어 여러 개다.")]
        [SerializeField] private TMP_Text[] _siteTexts;

        [Tooltip("목록 화면의 게시판 이름. 여러 글이 섞여 있으므로 전체 게시판이다.")]
        [SerializeField] private TMP_Text _boardListText;

        [Tooltip("글 화면의 게시판 이름. 그 글이 올라온 게시판이다.")]
        [SerializeField] private TMP_Text _boardPostText;

        [Header("게시글")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _metaText;
        [SerializeField] private TMP_Text _bodyText;

        [Tooltip("본문 아래 반응. 누르면 눌린 상태가 되고 한 번 더 누르면 풀린다.")]
        [SerializeField] private TMP_Text _likeText;
        [SerializeField] private TMP_Text _dislikeText;
        [SerializeField] private Button _likeButton;
        [SerializeField] private Button _dislikeButton;

        [Tooltip("누르지 않은 반응 칸의 배경색.")]
        [SerializeField] private Color _reactionIdleColor = new Color(0.955f, 0.958f, 0.97f, 1f);

        [Tooltip("누른 반응 칸의 배경색.")]
        [SerializeField] private Color _reactionPressedColor = new Color(0.82f, 0.87f, 0.97f, 1f);

        [Header("댓글")]
        [SerializeField] private TMP_Text _commentHeaderText;
        [SerializeField] private RectTransform _commentRoot;

        [Tooltip("복제할 댓글 카드. 안에 글자 하나를 두고 배경으로 줄을 구분한다.")]
        [SerializeField] private GameObject _commentTemplate;

        [Tooltip("내가 쓴 댓글의 배경색. 남의 댓글과 구분한다.")]
        [SerializeField] private Color _playerCommentColor = new Color(0.90f, 0.94f, 1f, 1f);

        [Tooltip("댓글 스크롤. 새 댓글이 붙으면 맨 아래로 내린다.")]
        [SerializeField] private ScrollRect _commentScroll;

        [Header("댓글 선택지")]
        [SerializeField] private TMP_Text _choiceHeaderText;
        [SerializeField] private RectTransform _choiceRoot;
        [SerializeField] private Button _choiceTemplate;

        [Tooltip("선택 결과 안내.")]
        [SerializeField] private TMP_Text _noticeText;

        [Header("잠금")]
        [Tooltip("글 화면 전체를 한꺼번에 잠그는 무리. 누르는 것도 끄는 것도 함께 막힌다.")]
        [SerializeField] private CanvasGroup _postControls;

        [Tooltip("목록 화면 쪽 무리.")]
        [SerializeField] private CanvasGroup _boardControls;

        private readonly List<GameObject> _spawnedComments = new List<GameObject>();
        private readonly List<GameObject> _spawnedChoices = new List<GameObject>();
        private readonly List<GameObject> _spawnedEntries = new List<GameObject>();

        private IReadOnlyList<CommunityBoardEntry> _entries;
        private Action<CommunityBoardEntry> _onEntry;
        private Action _onClose;
        private bool _showingBoard = true;

        private const string WindowTitleTextId = "ui.net.window_title";
        private const string HotMarkTextId = "ui.net.hot_mark";

        /// <summary>목록 줄 왼쪽의 세모를 맡은 글자의 이름. 제목과 가르는 기준이다.</summary>
        private const string BoardMarkName = "Text_Mark";
        private const string PlayerAuthorTextId = "ui.net.author_player";

        private const string SiteTextId = "ui.net.site_name";
        private const string BoardPostTextId = "ui.net.board_free";
        private const string BoardListTextId = "ui.net.board_all";
        private const string MetaTextId = "ui.net.post_meta";
        private const string CommentHeaderTextId = "ui.net.comment_header";
        private const string ChoiceHeaderTextId = "ui.net.choice_header";
        private const string LikeTextId = "ui.net.like";
        private const string DislikeTextId = "ui.net.dislike";

        private WebPageSO _page;
        private int _views;
        private int _likes;
        private int _dislikes;
        private bool _likePressed;
        private bool _dislikePressed;
        private Action<bool> _onLike;
        private Action<bool> _onDislike;
        private string _postTimeTextId;
        private List<CommunityComment> _comments = new List<CommunityComment>();
        private IReadOnlyList<TutorialCommentChoice> _choices;
        private Func<TutorialCommentChoice, string> _choiceLabelProvider;
        private Func<TutorialCommentChoice, string> _choiceNoteProvider;

        /// <summary>선택지 카드 안 오른쪽 아래 글자의 이름. 본문과 가르는 기준이다.</summary>
        private const string ChoiceNoteName = "Text_Note";
        private Action<TutorialCommentChoice> _onChoice;
        private Func<string> _noticeProvider;

        /// <summary>
        /// 누를 수 있는 것들을 한꺼번에 열고 잠근다.
        ///
        /// 대사 상자가 떠 있는 동안에는 잠근다. 말이 끝나기 전에 만지면 순서가 엉킨다.
        /// 누르는 것뿐 아니라 끌어서 내리는 것도 함께 막는다.
        /// </summary>
        public void SetControlsEnabled(bool enabled)
        {
            _controlsEnabled = enabled;

            Apply(_postControls, enabled);
            Apply(_boardControls, enabled);
            if (_closeButton != null) _closeButton.interactable = enabled && _onClose != null;

            void Apply(CanvasGroup group, bool on)
            {
                if (group == null) return;
                group.interactable = on;
                group.blocksRaycasts = on;   // 끌기까지 막으려면 이것도 꺼야 한다
            }
        }

        private bool _controlsEnabled = true;

        /// <summary>창 닫기 버튼이 할 일을 정한다. null을 주면 버튼이 꺼진다.</summary>
        public void BindWindow(Action onClose)
        {
            _onClose = onClose;

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(() => _onClose?.Invoke());
                _closeButton.interactable = onClose != null && _controlsEnabled;
            }
        }

        /// <summary>게시판 목록을 건다.</summary>
        public void BindBoard(IReadOnlyList<CommunityBoardEntry> entries, Action<CommunityBoardEntry> onEntry)
        {
            _entries = entries;
            _onEntry = onEntry;
            Refresh();
        }

        /// <summary>목록 보기와 글 보기를 바꾼다.</summary>
        public void ShowBoard(bool showBoard)
        {
            _showingBoard = showBoard;

            if (_boardView != null) _boardView.SetActive(showBoard);
            if (_postView != null) _postView.SetActive(!showBoard);

            Refresh();

            // 목록은 언제나 맨 위부터 보여준다. 글 줄이 늘면서 자리가 밀리는 것을 막는다.
            if (showBoard && _boardScroll != null)
            {
                Canvas.ForceUpdateCanvases();
                if (_boardScroll.content != null) LayoutRebuilder.ForceRebuildLayoutImmediate(_boardScroll.content);
                _boardScroll.verticalNormalizedPosition = 1f;
            }
        }

        /// <summary>게시글을 건다. 조회수와 작성 시각은 화면에 보이기 위한 값이다.</summary>
        public void BindPage(WebPageSO page, int views, string postTimeTextId, int likes = 0, int dislikes = 0)
        {
            _page = page;
            _views = views;
            _likes = likes;
            _dislikes = dislikes;
            _likePressed = false;
            _dislikePressed = false;
            _postTimeTextId = postTimeTextId;

            // 새 글이므로 맨 위부터 보여준다.
            _shownCommentCount = -1;
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
            Action<TutorialCommentChoice> onChoice,
            Func<TutorialCommentChoice, string> noteProvider = null)
        {
            _choices = choices;
            _choiceLabelProvider = labelProvider;
            _choiceNoteProvider = noteProvider;
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

        /// <summary>
        /// 좋아요와 싫어요를 눌렀을 때 할 일을 건다.
        /// 넘겨주는 값은 "지금 눌린 상태인가"다. 취소일 때는 false 가 간다.
        /// </summary>
        public void BindReactions(Action<bool> onLike, Action<bool> onDislike)
        {
            _onLike = onLike;
            _onDislike = onDislike;

            if (_likeButton != null)
            {
                _likeButton.onClick.RemoveAllListeners();
                _likeButton.onClick.AddListener(OnLikeClicked);
            }
            if (_dislikeButton != null)
            {
                _dislikeButton.onClick.RemoveAllListeners();
                _dislikeButton.onClick.AddListener(OnDislikeClicked);
            }

            ApplyReactionColors();
        }

        // 좋아요와 싫어요는 같이 눌린 상태가 될 수 없다.
        // 한쪽을 누르면 다른 쪽은 알아서 풀린다. 실제 커뮤니티가 그렇게 동작한다.
        private void OnLikeClicked()
        {
            _likePressed = !_likePressed;
            if (_likePressed) _dislikePressed = false;
            Refresh();
            _onLike?.Invoke(_likePressed);
        }

        private void OnDislikeClicked()
        {
            _dislikePressed = !_dislikePressed;
            if (_dislikePressed) _likePressed = false;
            Refresh();
            _onDislike?.Invoke(_dislikePressed);
        }

        private void ApplyReactionColors()
        {
            if (_likeButton != null)
            {
                var image = _likeButton.GetComponent<Image>();
                if (image != null) image.color = _likePressed ? _reactionPressedColor : _reactionIdleColor;
            }
            if (_dislikeButton != null)
            {
                var image = _dislikeButton.GetComponent<Image>();
                if (image != null) image.color = _dislikePressed ? _reactionPressedColor : _reactionIdleColor;
            }
        }

        /// <summary>같은 문구를 쓰는 칸이 여럿이라 한 번에 채운다.</summary>
        private static void SetAll(TMP_Text[] targets, string text)
        {
            if (targets == null) return;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null) targets[i].text = text;
            }
        }

        public void Refresh()
        {
            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc)) return;

            if (_windowTitleText != null) _windowTitleText.text = loc.Get(WindowTitleTextId);
            SetAll(_siteTexts, loc.Get(SiteTextId));
            if (_boardListText != null) _boardListText.text = loc.Get(BoardListTextId);
            if (_boardPostText != null) _boardPostText.text = loc.Get(BoardPostTextId);

            if (_showingBoard)
            {
                RebuildBoard(loc);
                return;     // 목록을 보는 중에는 글 내용을 그릴 것이 없다
            }

            if (_titleText != null)
            {
                // 실제 커뮤니티처럼 제목 옆에 댓글 수를 붙인다.
                _titleText.text = _page == null
                    ? string.Empty
                    : loc.Get(_page.TitleTextId) +
                      " <size=72%><color=#C0392B>[" + _comments.Count + "]</color></size>";
            }
            if (_bodyText != null) _bodyText.text = _page != null ? loc.Get(_page.BodyTextId) : string.Empty;

            if (_metaText != null)
            {
                _metaText.text = _page == null
                    ? string.Empty
                    : loc.Get(MetaTextId, loc.Get("ui.net.author_anon"), _views, _comments.Count,
                        string.IsNullOrEmpty(_postTimeTextId) ? string.Empty : loc.Get(_postTimeTextId));
            }

            if (_likeText != null) _likeText.text = loc.Get(LikeTextId, _likes + (_likePressed ? 1 : 0));
            if (_dislikeText != null) _dislikeText.text = loc.Get(DislikeTextId, _dislikes + (_dislikePressed ? 1 : 0));
            ApplyReactionColors();

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

        private void RebuildBoard(LocalizationService loc)
        {
            if (_boardRoot == null || _boardEntryTemplate == null) return;

            ClearSpawned(_spawnedEntries);
            if (_entries == null) return;

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry == null) continue;

                var item = Instantiate(_boardEntryTemplate, _boardRoot);
                item.gameObject.name = "Post_" + i;
                item.gameObject.SetActive(true);

                foreach (var text in item.GetComponentsInChildren<TMP_Text>(true))
                {
                    // 왼쪽의 세모는 들어가야 하는 글에만 켠다.
                    if (text.name == BoardMarkName)
                    {
                        text.gameObject.SetActive(entry.Openable);
                        continue;
                    }

                    string title = loc.Get(entry.TitleTextId);
                    if (entry.IsHot) title = "[" + loc.Get(HotMarkTextId) + "] " + title;

                    string meta = string.IsNullOrEmpty(entry.MetaTextId) ? string.Empty : loc.Get(entry.MetaTextId);
                    text.text = string.IsNullOrEmpty(meta) ? title : title + "\n" + meta;
                }

                // 모든 줄이 마우스를 올리면 옅은 회색이 된다. 실제 게시판이 그렇다.
                // 열리지 않는 글은 눌러도 아무 일이 없다. 그 판단은 누른 뒤에 한다.
                item.interactable = true;

                var captured = entry;
                item.onClick.RemoveAllListeners();
                item.onClick.AddListener(() => _onEntry?.Invoke(captured));

                _spawnedEntries.Add(item.gameObject);
            }
        }

        private void RebuildComments(LocalizationService loc)
        {
            if (_commentRoot == null || _commentTemplate == null) return;

            ClearSpawned(_spawnedComments);

            for (int i = 0; i < _comments.Count; i++)
            {
                var comment = _comments[i];
                if (comment == null) continue;

                var item = Instantiate(_commentTemplate, _commentRoot);
                item.name = "Comment_" + i;
                item.SetActive(true);

                var label = item.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    // 커뮤니티는 다들 익명이다. 내가 쓴 글에만 누구인지 괄호로 덧붙는다.
                    string author = comment.IsPlayer
                        ? loc.Get(PlayerAuthorTextId, loc.Get(comment.AuthorTextId))
                        : loc.Get(comment.AuthorTextId);

                    // 작성자는 작고 흐리게, 내용은 그대로. 실제 커뮤니티 댓글처럼 두 줄로 둔다.
                    label.text = "<size=82%><color=#6B7280>" + author + "</color></size>\n"
                                 + loc.Get(comment.BodyTextId);
                }

                // 내가 쓴 댓글은 배경색으로 구분한다.
                if (comment.IsPlayer)
                {
                    var background = item.GetComponent<Image>();
                    if (background != null) background.color = _playerCommentColor;
                }

                _spawnedComments.Add(item);
            }

            // 굴러가는 것은 화면 한 장 전체다.
            // 높이가 아직 갱신되지 않은 채로 위치를 잡으면 엉뚱한 곳에 멈추므로 배치를 먼저 확정한다.
            if (_commentScroll != null)
            {
                Canvas.ForceUpdateCanvases();

                var content = _commentScroll.content;
                if (content != null) LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                else if (_commentRoot != null) LayoutRebuilder.ForceRebuildLayoutImmediate(_commentRoot);

                // 글을 막 열었으면 맨 위, 즉 제목부터 보여준다.
                // 그 뒤에 댓글이 새로 달렸을 때만 맨 아래로 내려 방금 달린 것을 보여준다.
                if (_shownCommentCount < 0) _commentScroll.verticalNormalizedPosition = 1f;
                else if (_comments.Count > _shownCommentCount) _commentScroll.verticalNormalizedPosition = 0f;
            }

            // 글을 걸고 댓글을 거는 것이 두 번에 나뉘어 들어온다.
            // 댓글이 아직 하나도 없는 사이 단계를 "처음"으로 계속 두어야
            // 뒤따라 들어오는 첫 댓글 묶음을 새 댓글로 잘못 보지 않는다.
            if (_comments.Count > 0) _shownCommentCount = _comments.Count;
        }

        /// <summary>마지막으로 그린 댓글 수. 새로 달렸는지 가리는 데만 쓴다. 음수면 글을 막 열었다는 뜻이다.</summary>
        private int _shownCommentCount = -1;

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

                // 카드 안에는 글자가 둘이다. 본문과 오른쪽 아래의 단서다.
                // 이름으로 갈라야 순서가 바뀌어도 엉뚱한 칸에 들어가지 않는다.
                foreach (var text in item.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.name == ChoiceNoteName)
                    {
                        string note = _choiceNoteProvider != null ? _choiceNoteProvider(choice) : string.Empty;
                        text.text = note;
                        text.gameObject.SetActive(!string.IsNullOrEmpty(note));
                    }
                    else if (_choiceLabelProvider != null)
                    {
                        text.text = _choiceLabelProvider(choice);
                    }
                }

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
