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

        /// <summary>
        /// 이 글이 괴담의 믿음에 얼마나 보태고 있는가(%).
        /// 0이면 괴담과 무관한 글이라 표시하지 않는다.
        /// </summary>
        public int BeliefPercent;
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

        [Tooltip("제목 오른쪽의 믿음 기여도.")]
        [SerializeField] private TMP_Text _postBeliefText;

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

        [Tooltip("글 화면 굴림판. 글을 열면 맨 위로, 새 댓글이 붙으면 그 자리로 따라간다.")]
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

        /// <summary>목록 줄 오른쪽의 믿음도를 맡은 글자의 이름.</summary>
        private const string BoardBeliefName = "Text_Belief";
        /// <summary>목록 줄의 곁가지(닉네임 / 조회 / 댓글 / 시간)를 제목보다 얼마나 작게 둘 것인가.</summary>
        private const int MetaSizePercent = 72;

        private const string BeliefPercentTextId = "ui.net.belief_percent";
        private const string BeliefPercentShortTextId = "ui.net.belief_percent_short";
        private const string PlayerAuthorTextId = "ui.net.author_player";

        private const string SiteTextId = "ui.net.site_name";
        private const string BoardPostTextId = "ui.net.board_free";
        private const string BoardListTextId = "ui.net.board_all";
        private const string MetaTextId = "ui.net.post_meta";
        private const string CommentHeaderTextId = "ui.net.comment_header";
        private const string ChoiceHeaderTextId = "ui.net.choice_header";
        private const string LikeTextId = "ui.net.like";
        private const string DislikeTextId = "ui.net.dislike";

        // ------------------------------------------------------------- 생김새

        [Header("생김새 (컴퓨터 창 / 휴대폰)")]
        [Tooltip("괴담넷은 하나뿐이다. 컴퓨터로 보든 휴대폰으로 보든 같은 화면을 모양만 바꿔 쓴다.")]
        [SerializeField] private RectTransform _window;

        [Tooltip("휴대폰으로 볼 때만 뒤에 깔리는 껍데기. 컴퓨터로 볼 때는 꺼진다.")]
        [SerializeField] private GameObject _phoneShell;

        [Tooltip("휴대폰으로 볼 때 맨 윗줄 오른쪽에 뜨는 시각. 컴퓨터로 볼 때는 꺼진다.")]
        [SerializeField] private TMP_Text _statusClockText;

        [Tooltip("그 시각 왼쪽에 붙는 전체 믿음도. 함께 켜지고 함께 꺼진다.")]
        [SerializeField] private TMP_Text _statusBeliefText;

        [Tooltip("휴대폰 카메라 구멍. 괴담넷이 휴대폰 화면을 덮으므로 여기에도 같은 자리에 하나 둔다.")]
        [SerializeField] private GameObject _statusCamera;

        [Tooltip("사이트 이름이 앉은 파란 머리말의 키. 목록용과 글용 둘이다.")]
        [SerializeField] private LayoutElement[] _headerElements;

        [Tooltip("좌우 여백을 padding 으로 쓰는 칸들. 세로 화면에서는 여백을 줄인다.")]
        [SerializeField] private LayoutGroup[] _insetGroups;

        [Tooltip("좌우 여백을 offset 으로 쓰는 줄들. 머리말이 여기에 든다.")]
        [SerializeField] private RectTransform[] _insetRows;

        [Tooltip("세로 화면에서 함께 줄어드는 글자들.")]
        [SerializeField] private TMP_Text[] _scaledTexts;

        [Tooltip("세로 화면의 폭. 실제 휴대폰처럼 가운데에 세워 둔다.")]
        [SerializeField] private float _phoneWidth = 620f;

        [Tooltip("이미 떠 있는 휴대폰 칸에 맞출 때 짜는 폭. 이 폭으로 짜 놓고 통째로 줄인다. "
               + "좁게 짤수록 줄어든 뒤의 글자가 커진다. 실제 휴대폰처럼 한 줄에 열대여섯 자가 들어가는 폭이다.")]
        [SerializeField] private float _phoneFitWidth = 380f;

        [Tooltip("세로 화면의 위아래 여백.")]
        [SerializeField] private float _phoneMargin = 28f;

        [Tooltip("오른쪽 굴림 막대가 차지하는 폭. 세로 화면에서 글이 막대에 물리지 않게 한다.")]
        [SerializeField] private float _phoneScrollbarWidth = 16f;

        [Tooltip("컴퓨터 창이 아래에 비워 두는 만큼. 작업 표시줄 자리다.")]
        [SerializeField] private float _taskbarHeight = 64f;

        /// <summary>지금 휴대폰으로 보고 있는가.</summary>
        public bool IsPhone { get; private set; }

        /// <summary>지금 목록을 보고 있는가. 돌아가기가 목록으로 갈지 창을 닫을지 가른다.</summary>
        public bool IsShowingBoard => _showingBoard;

        /// <summary>세로 화면의 좌우 여백은 폭에 비례한다.</summary>
        private const float PhoneInsetRatio = 0.042f;
        private const int MinPhoneInset = 10;

        /// <summary>이 폭이면 글자를 줄이지 않는다. 좁아지는 만큼 함께 줄어든다.</summary>
        private const float PhoneFontRefWidth = 838f;

        /// <summary>아무리 좁아도 이보다 더 줄이지는 않는다. 읽을 수 없게 된다.</summary>
        private const float MinPhoneFontScale = 0.75f;

        /// <summary>
        /// 창을 다른 칸에 딱 맞춘다. 캔버스가 달라도 되도록 화면 좌표로 옮긴다.
        /// 맞출 수 없으면 false 를 돌려주고 부르는 쪽이 원래 방식으로 돌아간다.
        /// </summary>
        private bool FitTo(RectTransform frame)
        {
            var parent = _window != null ? _window.parent as RectTransform : null;
            if (parent == null || frame == null) return false;

            // 꺼져 있는 동안에는 캔버스 배율이 아직 실리지 않아 자리가 엉뚱하게 나온다.
            // 켠 뒤에 맞춰야 한다.
            if (!parent.gameObject.activeInHierarchy || !frame.gameObject.activeInHierarchy) return false;

            var corners = new Vector3[4];
            frame.GetWorldCorners(corners);

            var min = parent.InverseTransformPoint(corners[0]);
            var max = parent.InverseTransformPoint(corners[2]);

            float width = max.x - min.x;
            float height = max.y - min.y;
            if (width <= 1f || height <= 1f) return false;

            // 칸에 맞춰 잘게 다시 짜지 않는다. 늘 같은 폭으로 짜 놓고 통째로 줄인다.
            // 그래야 여백과 줄 높이와 글자가 한꺼번에 같은 비율로 줄어 배치가 무너지지 않는다.
            float scale = width / _phoneFitWidth;

            _window.anchorMin = new Vector2(0.5f, 0.5f);
            _window.anchorMax = new Vector2(0.5f, 0.5f);
            _window.pivot = new Vector2(0.5f, 0.5f);
            _window.localScale = new Vector3(scale, scale, 1f);
            _window.sizeDelta = new Vector2(_phoneFitWidth, height / scale);
            _window.anchoredPosition = new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
            return true;
        }

        private float[] _deskFontSizes;
        private int _deskInset = -1;
        private bool _shapeReady;

        /// <summary>
        /// 컴퓨터 창과 휴대폰 화면을 오간다.
        ///
        /// 내용과 동작은 건드리지 않는다. 창 크기와 좌우 여백과 글자 크기만 바꾼다.
        /// 괴담넷을 하나만 두는 이유가 이것이다. 무엇을 고치든 양쪽에 함께 반영된다.
        ///
        /// frame 을 주면 그 자리에 딱 맞춘다. 현장에 이미 떠 있는 휴대폰 화면이 그것이다.
        /// 그러면 휴대폰이 커지지 않고, 들고 있던 그 화면에 괴담넷이 켜진다.
        /// </summary>
        public void SetShape(bool phone, RectTransform frame = null)
        {
            CaptureShape();
            IsPhone = phone;

            // 이미 껍데기가 있는 자리에 맞출 때는 우리 껍데기를 깔지 않는다.
            if (_phoneShell != null) _phoneShell.SetActive(phone && frame == null);

            float phoneWidth = _phoneWidth;

            if (_window != null)
            {
                if (phone && frame != null && FitTo(frame))
                {
                    // 늘 같은 폭으로 짜 두고 통째로 줄인 것이라, 여백과 글자는 그 폭 기준이다.
                    phoneWidth = _phoneFitWidth;
                }
                else if (phone)
                {
                    _window.localScale = Vector3.one;
                    // 가운데에 세로로 세운다. 위아래는 화면 끝에서 조금씩 띄운다.
                    _window.anchorMin = new Vector2(0.5f, 0f);
                    _window.anchorMax = new Vector2(0.5f, 1f);
                    _window.pivot = new Vector2(0.5f, 0.5f);
                    _window.anchoredPosition = Vector2.zero;
                    _window.sizeDelta = new Vector2(_phoneWidth, -_phoneMargin * 2f);
                }
                else
                {
                    // 바탕화면 위에 뜬 창. 아래쪽 작업 표시줄 자리는 비워 둔다.
                    _window.localScale = Vector3.one;
                    _window.anchorMin = Vector2.zero;
                    _window.anchorMax = Vector2.one;
                    _window.pivot = new Vector2(0.5f, 0.5f);
                    _window.offsetMin = new Vector2(0f, _taskbarHeight);
                    _window.offsetMax = Vector2.zero;
                }
            }

            // 좁을수록 여백과 글자를 함께 줄인다. 폭 하나로 정해 두면 어떤 크기에 맞춰도 읽힌다.
            int inset = phone
                ? Mathf.Clamp(Mathf.RoundToInt(phoneWidth * PhoneInsetRatio), MinPhoneInset, _deskInset)
                : _deskInset;

            // 좌우뿐 아니라 위아래 여백도 함께 줄인다.
            // 1920 폭 기준으로 잡힌 값이라 좁은 화면에서는 그 자리만 휑하게 벌어진다.
            if (_insetGroups != null && _deskPads != null)
            {
                float vertical = phone ? PhoneVerticalPadScale : 1f;

                for (int i = 0; i < _insetGroups.Length && i < _deskPads.Length; i++)
                {
                    var group = _insetGroups[i];
                    if (group == null) continue;

                    var desk = _deskPads[i];
                    group.padding = new RectOffset(inset, inset,
                        Mathf.RoundToInt(desk.top * vertical),
                        Mathf.RoundToInt(desk.bottom * vertical));
                    LayoutRebuilder.MarkLayoutForRebuild((RectTransform)group.transform);
                }
            }

            if (_insetRows != null)
            {
                foreach (var row in _insetRows)
                {
                    if (row == null) continue;
                    row.offsetMin = new Vector2(inset, row.offsetMin.y);
                    row.offsetMax = new Vector2(-inset, row.offsetMax.y);
                }
            }

            // 믿음도는 배치에서 빠져 있고 오른쪽 위에 직접 붙는다. 여백과 높이를 따로 맞춰 준다.
            // 세로 화면에서는 여백이 좁아 굴림 막대에 물리므로 그 폭만큼 더 들인다.
            if (_postBeliefText != null)
            {
                var rt = _postBeliefText.rectTransform;
                float right = inset + (phone ? _phoneScrollbarWidth : 0f);
                float top = phone ? PhoneBeliefTop : DeskBeliefTop;
                rt.anchoredPosition = new Vector2(-right, -top);
                _postBeliefText.alignment = TextAlignmentOptions.TopRight;
            }

            if (_scaledTexts != null && _deskFontSizes != null)
            {
                float scale = phone
                    ? Mathf.Clamp(phoneWidth / PhoneFontRefWidth, MinPhoneFontScale, 1f)
                    : 1f;
                for (int i = 0; i < _scaledTexts.Length && i < _deskFontSizes.Length; i++)
                {
                    if (_scaledTexts[i] != null) _scaledTexts[i].fontSize = _deskFontSizes[i] * scale;
                }
            }

            ApplyChrome(phone, inset);

            if (_window != null) LayoutRebuilder.MarkLayoutForRebuild(_window);
        }

        /// <summary>
        /// 자리를 숫자로 박아 둔 것들. 1920 폭 창을 기준으로 잡혀 있어 좁은 화면에서는 겹친다.
        /// 창 제목 줄과 게시판 한 줄이 그렇다. 여기서만 따로 맞춘다.
        /// </summary>
        private void ApplyChrome(bool phone, int inset)
        {
            // --- 맨 윗줄 ---
            // 컴퓨터에서는 창 제목 표시줄이고, 휴대폰에서는 상태 줄이다.
            // 휴대폰에서는 제목과 빨간 X 를 치우고, 왼쪽에 돌아가기, 오른쪽에 시각만 남긴다.
            if (_windowTitleText != null)
            {
                _windowTitleText.gameObject.SetActive(!phone);

                var rt = _windowTitleText.rectTransform;
                rt.offsetMin = new Vector2(DeskTitleLeft, rt.offsetMin.y);
                rt.offsetMax = new Vector2(-DeskCloseButtonRoom, rt.offsetMax.y);
            }

            if (_statusClockText != null) _statusClockText.gameObject.SetActive(phone);
            if (_statusBeliefText != null) _statusBeliefText.gameObject.SetActive(phone);
            if (_statusCamera != null) _statusCamera.SetActive(phone);

            if (_closeButton != null)
            {
                var rt = (RectTransform)_closeButton.transform;
                var image = _closeButton.GetComponent<Image>();

                if (phone)
                {
                    // 왼쪽 끝의 돌아가기. 실제 휴대폰처럼 글자 하나로 둔다.
                    rt.anchorMin = new Vector2(0f, 0.5f);
                    rt.anchorMax = new Vector2(0f, 0.5f);
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.anchoredPosition = new Vector2(8f, 0f);
                    rt.sizeDelta = new Vector2(40f, 36f);
                    if (image != null) image.color = new Color(1f, 1f, 1f, 0f);
                }
                else
                {
                    rt.anchorMin = new Vector2(1f, 0.5f);
                    rt.anchorMax = new Vector2(1f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(-70f, 0f);
                    rt.sizeDelta = new Vector2(56f, 40f);
                    if (image != null) image.color = DeskCloseColor;
                }

                foreach (var label in _closeButton.GetComponentsInChildren<TMP_Text>(true))
                {
                    label.text = phone ? "‹" : "X";
                    label.rectTransform.sizeDelta = rt.sizeDelta;
                }
            }

            // --- 게시판 한 줄 ---
            // 믿음도가 오른쪽 끝에 얹히므로, 제목이 그 밑으로 들어가지 않게 자리를 비워 준다.
            if (_boardEntryTemplate != null)
            {
                float beliefWidth = phone ? PhoneBoardBeliefWidth : DeskBoardBeliefWidth;

                foreach (var text in _boardEntryTemplate.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.name == BoardBeliefName)
                    {
                        var brt = text.rectTransform;
                        brt.sizeDelta = new Vector2(beliefWidth, brt.sizeDelta.y);

                        // 세로 화면에서는 제목이 두 줄로 넘어간다. 가운데가 아니라 첫 줄에 맞춰 위에 건다.
                        // 오른쪽 끝은 굴림 막대가 덮으므로 그만큼 안으로 들인다.
                        if (phone)
                        {
                            brt.anchorMin = new Vector2(1f, 1f);
                            brt.anchorMax = new Vector2(1f, 1f);
                            brt.pivot = new Vector2(1f, 1f);
                            brt.anchoredPosition = new Vector2(-(10f + _phoneScrollbarWidth), -14f);
                            text.alignment = TextAlignmentOptions.TopRight;
                        }
                        else
                        {
                            brt.anchorMin = new Vector2(1f, 0.5f);
                            brt.anchorMax = new Vector2(1f, 0.5f);
                            brt.pivot = new Vector2(1f, 0.5f);
                            brt.anchoredPosition = new Vector2(-16f, 6f);
                            text.alignment = TextAlignmentOptions.Right;
                        }
                    }
                }

                // 줄 높이는 글마다 제 내용에 맞춰 자란다. 여기서 정하는 것은 안쪽 여백뿐이다.
                // 오른쪽은 믿음도가 앉을 자리만큼 비워 둔다. 그래야 제목이 그 밑으로 들어가지 않는다.
                if (_boardEntryTemplate.GetComponent<VerticalLayoutGroup>() is VerticalLayoutGroup rowLayout)
                {
                    int pad = phone ? PhoneBoardRowPad : DeskBoardRowPad;
                    rowLayout.padding = new RectOffset(
                        12, Mathf.RoundToInt(beliefWidth) + 22, pad, pad + 6);
                }

                // 줄을 가르는 선. 좁은 화면에서는 줄 간격이 촘촘해 옅은 회색이 묻힌다.
                var ruleImage = _boardEntryTemplate.transform.Find("Rule");
                if (ruleImage != null && ruleImage.GetComponent<Image>() is Image img)
                {
                    img.color = phone ? PhoneRuleColor : DeskRuleColor;
                }
            }

            // --- 파란 머리말 ---
            // "괴담넷 전체게시판" 이 앉은 띠. 좁은 화면에서는 이 띠가 자리를 너무 많이 차지한다.
            if (_headerElements != null)
            {
                float height = phone ? PhoneHeaderHeight : DeskHeaderHeight;
                foreach (var element in _headerElements)
                {
                    if (element == null) continue;
                    element.minHeight = height;
                    element.preferredHeight = height;
                }
            }

            if (phone)
            {
                foreach (var site in _siteTexts)
                {
                    if (site != null) site.fontSize = PhoneSiteFontSize;
                }
                if (_boardListText != null) _boardListText.fontSize = PhoneBoardFontSize;
                if (_boardPostText != null) _boardPostText.fontSize = PhoneBoardFontSize;
            }

            // --- 본문 ---
            // 줄이 서로 붙어 빽빽해 보인다. 줄 사이를 띄우고 문단 사이도 벌린다.
            // 좁은 화면에서는 글자도 한 급 더 줄인다. 한 줄에 들어가는 글자가 너무 적으면 읽기 나쁘다.
            if (_bodyText != null)
            {
                _bodyText.lineSpacing = phone ? PhoneBodyLineSpacing : DeskBodyLineSpacing;
                _bodyText.paragraphSpacing = phone ? PhoneBodyParagraphSpacing : DeskBodyParagraphSpacing;

                if (phone) _bodyText.fontSize = PhoneBodyFontSize;
            }

            // --- 댓글 카드 ---
            // 두 줄(작성자 / 내용)이 들어가는데 세로 화면에서는 내용이 두 줄로 넘어가기도 한다.
            if (_commentTemplate != null)
            {
                // 칸 높이는 댓글마다 제 내용에 맞춰 자란다. 여기서 정하는 것은 안쪽 여백뿐이다.
                if (_commentTemplate.GetComponent<VerticalLayoutGroup>() is VerticalLayoutGroup comLayout)
                {
                    int pad = phone ? PhoneCommentPad : DeskCommentPad;
                    comLayout.padding = new RectOffset(12, 12, pad, pad + 4);
                }

                // 작성자와 내용은 한 덩어리로 붙어 있어야 한 사람의 말로 읽힌다. 줄 사이를 좁힌다.
                foreach (var label in _commentTemplate.GetComponentsInChildren<TMP_Text>(true))
                {
                    label.lineSpacing = phone ? PhoneCommentLineSpacing : DeskCommentLineSpacing;
                }

                if (_commentTemplate.transform.Find("Rule") is Transform rule
                    && rule.GetComponent<Image>() is Image ruleImage)
                {
                    ruleImage.color = phone ? PhoneRuleColor : DeskRuleColor;
                }
            }

            // --- 좋아요 / 싫어요 칸 ---
            // 폭이 숫자로 박혀 있어, 좁은 화면에서는 이 둘이 글 칸 전체를 제 폭만큼 벌려 버린다.
            // 배치는 자식의 최소 폭보다 좁게 줄이지 못하기 때문이다. 칸부터 줄여야 글이 창 안에 든다.
            ApplyReactionChip(_likeButton, phone);
            ApplyReactionChip(_dislikeButton, phone);

            // --- 굴림 막대 ---
            ApplyScrollbar(_boardScroll, phone);
            ApplyScrollbar(_commentScroll, phone);
        }

        private void ApplyReactionChip(Button chip, bool phone)
        {
            if (chip == null) return;

            // 본문과 칸 사이를 벌려 두는 자리. 컴퓨터 기준으로 잡혀 있어 좁은 화면에서는 허전하다.
            if (chip.transform.parent != null
                && chip.transform.parent.GetComponent<HorizontalLayoutGroup>() is HorizontalLayoutGroup row)
            {
                var p = row.padding;
                row.padding = new RectOffset(p.left, p.right,
                    phone ? PhoneReactionGap : DeskReactionGap, p.bottom);
            }

            var rt = (RectTransform)chip.transform;
            rt.sizeDelta = phone
                ? new Vector2(PhoneReactionWidth, PhoneReactionHeight)
                : new Vector2(DeskReactionWidth, DeskReactionHeight);

            foreach (var label in chip.GetComponentsInChildren<TMP_Text>(true))
            {
                label.rectTransform.sizeDelta = rt.sizeDelta;
            }
        }

        private const float DeskReactionWidth = 250f;
        private const float DeskReactionHeight = 76f;
        private const float PhoneReactionWidth = 148f;
        private const float PhoneReactionHeight = 60f;

        /// <summary>
        /// 굴림 막대. 컴퓨터 창에서는 화살표가 들어간 굵은 막대지만,
        /// 휴대폰에서는 손가락으로 밀어 넘기는 것이라 가는 선 하나로 줄인다.
        /// </summary>
        private void ApplyScrollbar(ScrollRect scroll, bool phone)
        {
            var bar = scroll != null ? scroll.verticalScrollbar : null;
            if (bar == null) return;

            var rt = (RectTransform)bar.transform;
            rt.sizeDelta = new Vector2(phone ? PhoneScrollbarWidth : DeskScrollbarWidth, rt.sizeDelta.y);

            var up = rt.Find("Btn_ScrollUp");
            var down = rt.Find("Btn_ScrollDown");
            if (up != null) up.gameObject.SetActive(!phone);
            if (down != null) down.gameObject.SetActive(!phone);

            // 화살표를 감추면 손잡이가 오르내릴 자리도 그만큼 넓어진다.
            if (rt.Find("SlidingArea") is RectTransform slide)
            {
                float endPad = phone ? 3f : DeskScrollbarWidth + 4f;
                float sidePad = phone ? 2f : 6f;
                slide.offsetMin = new Vector2(sidePad, endPad);
                slide.offsetMax = new Vector2(-sidePad, -endPad);
            }
        }

        private const float DeskScrollbarWidth = 34f;
        private const float PhoneScrollbarWidth = 10f;

        private const float DeskTitleLeft = 110f;
        private const float DeskCloseButtonRoom = 260f;
        private static readonly Color DeskCloseColor = new Color(0.62f, 0.22f, 0.24f, 1f);
        private const float DeskBoardBeliefWidth = 320f;
        private const float PhoneBoardBeliefWidth = 58f;
        /// <summary>줄 안쪽 위아래 여백. 줄 높이 자체는 글마다 제 내용에 맞춰 자란다.</summary>
        private const int DeskBoardRowPad = 14;
        private const int PhoneBoardRowPad = 22;

        private static readonly Color DeskRuleColor = new Color(0.86f, 0.87f, 0.90f, 1f);
        private static readonly Color PhoneRuleColor = new Color(0.78f, 0.79f, 0.84f, 1f);

        /// <summary>파란 머리말의 키와 그 안의 글자 크기.</summary>
        private const float DeskHeaderHeight = 90f;
        private const float PhoneHeaderHeight = 54f;
        private const float PhoneSiteFontSize = 25f;
        private const float PhoneBoardFontSize = 17f;

        /// <summary>본문 줄 사이와 문단 사이. 빽빽해 보이지 않게 띄운다.</summary>
        private const float DeskBodyLineSpacing = 8f;
        private const float DeskBodyParagraphSpacing = 18f;
        private const float PhoneBodyLineSpacing = 16f;
        private const float PhoneBodyParagraphSpacing = 26f;

        /// <summary>좁은 화면의 본문 글자 크기. 다른 글자처럼 비율로 줄이지 않고 따로 정한다.</summary>
        private const float PhoneBodyFontSize = 19f;
        /// <summary>댓글 칸 안쪽 위아래 여백. 칸 높이 자체는 댓글마다 제 내용에 맞춰 자란다.</summary>
        private const int DeskCommentPad = 12;
        private const int PhoneCommentPad = 10;

        /// <summary>댓글 안의 줄 사이. 작성자와 내용이 한 덩어리로 보이게 좁힌다.</summary>
        private const float DeskCommentLineSpacing = -6f;
        private const float PhoneCommentLineSpacing = -12f;

        /// <summary>세로 화면에서 위아래 여백에 곱하는 비율. 1920 폭 기준으로 잡힌 값이라 그대로 두면 너무 넓다.</summary>
        private const float PhoneVerticalPadScale = 0.55f;

        /// <summary>본문과 좋아요 칸 사이. 컴퓨터에서는 넉넉하지만 좁은 화면에서는 허전하다.</summary>
        private const int DeskReactionGap = 140;
        private const int PhoneReactionGap = 46;

        /// <summary>제목 띠 안에서 믿음도가 내려앉는 높이. 작성자 정보 줄과 같은 선에 선다.</summary>
        private const float DeskBeliefTop = 112f;
        private const float PhoneBeliefTop = 68f;

        /// <summary>컴퓨터 창일 때의 값을 한 번만 적어 둔다. 되돌릴 때 쓴다.</summary>
        private void CaptureShape()
        {
            if (_shapeReady) return;
            _shapeReady = true;

            if (_insetGroups != null && _insetGroups.Length > 0 && _insetGroups[0] != null)
            {
                _deskInset = _insetGroups[0].padding.left;
            }
            if (_deskInset < 0) _deskInset = 150;

            if (_scaledTexts != null)
            {
                _deskFontSizes = new float[_scaledTexts.Length];
                for (int i = 0; i < _scaledTexts.Length; i++)
                {
                    _deskFontSizes[i] = _scaledTexts[i] != null ? _scaledTexts[i].fontSize : 24f;
                }
            }

            if (_insetGroups != null)
            {
                _deskPads = new RectOffset[_insetGroups.Length];
                for (int i = 0; i < _insetGroups.Length; i++)
                {
                    var p = _insetGroups[i] != null ? _insetGroups[i].padding : new RectOffset();
                    _deskPads[i] = new RectOffset(p.left, p.right, p.top, p.bottom);
                }
            }
        }

        private RectOffset[] _deskPads;

        private WebPageSO _page;
        private int _views;
        private int _likes;
        private int _beliefPercent;
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

            // 꺼져 있는 동안 창 크기가 바뀌었으면 그 사이의 배치는 밀려 있다.
            // 켜는 김에 한 번 다시 재운다. 그러지 않으면 예전 폭 그대로 남아 창 밖으로 삐져나온다.
            if (!showBoard && _commentScroll != null)
            {
                Canvas.ForceUpdateCanvases();
                if (_commentScroll.content != null) LayoutRebuilder.ForceRebuildLayoutImmediate(_commentScroll.content);
                _commentScroll.verticalNormalizedPosition = 1f;
            }
        }

        /// <summary>게시글을 건다. 조회수와 작성 시각은 화면에 보이기 위한 값이다.</summary>
        public void BindPage(WebPageSO page, int views, string postTimeTextId,
            int likes = 0, int dislikes = 0, int beliefPercent = 0)
        {
            _page = page;
            _views = views;
            _beliefPercent = beliefPercent;
            _likes = likes;
            _dislikes = dislikes;
            _likePressed = false;
            _dislikePressed = false;
            _postTimeTextId = postTimeTextId;

            // 새 글이므로 맨 위부터 보여준다.
            _shownCommentCount = -1;
            Refresh();
        }

        /// <summary>
        /// 이 글의 믿음 몫만 바꾼다.
        /// 글을 다시 걸지 않는다. 다시 걸면 "새 글"로 보고 스크롤이 맨 위로 튄다.
        /// </summary>
        public void SetPostBelief(int beliefPercent)
        {
            _beliefPercent = beliefPercent;
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

        /// <summary>
        /// 맨 윗줄의 전체 믿음도.
        ///
        /// 글 하나의 믿음도가 아니라 게시판 전체의 값이다. 휴대폰 상태 줄과 같은 숫자다.
        /// 글 하나의 믿음도는 그 글 안, 작성자 정보 옆에 따로 적힌다.
        /// </summary>
        private void RefreshStatusBelief(LocalizationService loc)
        {
            if (_statusBeliefText == null) return;
            _statusBeliefText.text = loc.Get(StatusBeliefTextId, GameStatus.Belief);
        }

        private const string StatusBeliefTextId = "ui.desktop.belief";

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
            RefreshStatusBelief(loc);
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

            string belief = _beliefPercent > 0 ? loc.Get(BeliefPercentTextId, _beliefPercent) : string.Empty;

            // 믿음도는 컴퓨터에서는 작성자 정보 줄 오른쪽 끝에 따로 선다.
            // 좁은 화면에서는 그 자리가 없어 글자끼리 겹치므로, 작성자 정보 줄에 이어 붙인다.
            bool beliefInMeta = IsPhone && !string.IsNullOrEmpty(belief);

            if (_metaText != null)
            {
                string meta = _page == null
                    ? string.Empty
                    : loc.Get(MetaTextId, loc.Get("ui.net.author_anon"), _views, _comments.Count,
                        string.IsNullOrEmpty(_postTimeTextId) ? string.Empty : loc.Get(_postTimeTextId));

                // 좁은 화면에서는 한 줄에 다 들어가지 않는다. 다음 줄에 오른쪽으로 붙여 세운다.
                if (beliefInMeta && !string.IsNullOrEmpty(meta))
                {
                    meta += "\n<align=right><color=#" + ColorUtility.ToHtmlStringRGB(_postBeliefText != null
                        ? _postBeliefText.color : Color.red) + ">" + belief + "</color></align>";
                }

                _metaText.text = meta;
            }

            if (_postBeliefText != null)
            {
                // 괴담과 무관한 글에는 아무것도 적지 않는다.
                _postBeliefText.text = beliefInMeta ? string.Empty : belief;
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

                    // 오른쪽의 믿음도. 괴담과 무관한 글에는 붙이지 않는다.
                    if (text.name == BoardBeliefName)
                    {
                        // 좁은 화면에서는 "믿음도 :" 를 떼고 숫자만 남긴다. 붉은 숫자면 그것으로 안다.
                        bool has = entry.BeliefPercent > 0;
                        if (has)
                        {
                            text.text = loc.Get(
                                IsPhone ? BeliefPercentShortTextId : BeliefPercentTextId, entry.BeliefPercent);
                        }
                        text.gameObject.SetActive(has);
                        continue;
                    }

                    string title = loc.Get(entry.TitleTextId);
                    if (entry.IsHot) title = "[" + loc.Get(HotMarkTextId) + "] " + title;

                    // 닉네임 / 조회 / 댓글 / 시간은 곁가지다. 제목보다 작고 흐리게 둔다.
                    string meta = string.IsNullOrEmpty(entry.MetaTextId) ? string.Empty : loc.Get(entry.MetaTextId);
                    text.text = string.IsNullOrEmpty(meta)
                        ? title
                        : title + "\n<size=" + MetaSizePercent + "%><color=#6B7280>" + meta + "</color></size>";

                    text.alignment = TextAlignmentOptions.TopLeft;
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
                // 그 뒤 댓글이 새로 달리면 화면이 따라 내려가 방금 달린 것에 붙어 있는다.
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
