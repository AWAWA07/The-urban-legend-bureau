using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.Localization;
using UrbanLegendBureau.Save;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 튜토리얼 진행을 이어 붙이는 컴포넌트. CaseDirector와 같은 자리의 물건이다.
    ///
    /// 판정은 하나도 하지 않는다. 검열은 InternetService, 믿음은 BeliefService,
    /// 화면은 UIService가 한다. 여기서는 순서만 정한다.
    ///
    /// 중요한 선택 하나: 튜토리얼은 <b>자기만의 SaveData</b> 위에서 돈다.
    /// 믿음도와 검열 기록이 실제 사건의 저장본을 건드리면 안 되기 때문이다.
    /// 그래서 SaveService를 하나 더 만들어 쓰고, 그 저장본은 파일로 쓰지 않는다.
    /// </summary>
    public class TutorialDirector : MonoBehaviour
    {
        [Header("화면")]
        [SerializeField] private DialogueScreen _dialogueScreen;

        [Tooltip("커뮤니티 화면 위에 겹쳐 쓰는 대화 화면. 같은 DialogueScreen을 재사용한다.")]
        [SerializeField] private DialogueScreen _talkScreen;

        [Tooltip("차지한의 컴퓨터 바탕화면.")]
        [SerializeField] private DesktopScreen _desktopScreen;

        [SerializeField] private CommunityPageScreen _communityScreen;

        [Tooltip("메모장. 컴퓨터로도 휴대폰으로도 이 화면 하나를 연다.")]
        [SerializeField] private MemoScreen _memoScreen;

        [Tooltip("잠깐 떴다 사라지는 알림 한 줄.")]
        [SerializeField] private ToastScreen _toastScreen;

        [Header("연결")]
        [Tooltip("튜토리얼에서 보여줄 게시글. 기존 WebPageSO를 그대로 쓴다.")]
        [SerializeField] private WebPageSO _tutorialPage;

        [Tooltip("튜토리얼이 끝났을 때 알릴 대상.")]
        [SerializeField] private CaseDirector _caseDirector;

        private UIService _ui;
        private LocalizationService _loc;
        private InternetService _internet;
        private BeliefService _belief;

        /// <summary>튜토리얼 전용 저장본. 실제 진행과 섞이지 않는다.</summary>
        private SaveService _sandbox;

        private int _lineIndex;
        private bool _finished;

        // --- 대사 ---
        private const string HanyoungNameTextId = "tutorial.char.hanyoung";
        private const string ChajihanNameTextId = "tutorial.char.chajihan";

        /// <summary>한영이 왼쪽, 차지한이 오른쪽. 밝기는 어둠에서 서서히 올라온다.</summary>
        private static readonly string[] LineTextIds =
        {
            "tutorial.line.001",
            "tutorial.line.002",
            "tutorial.line.003",
            "tutorial.line.006",   // 나중에 끼워 넣은 줄. ID 번호가 아니라 이 순서가 실제 순서다.
            "tutorial.line.004",
            "tutorial.line.005",
        };

        private static readonly bool[] LineIsHanyoung = { true, true, true, true, false, true };
        private static readonly float[] LineBrightness = { 1f, 0.55f, 1f, 1f, 1f, 1f };

        /// <summary>
        /// 한영이 화면에 있는가.
        /// 첫 대사("...")는 어둠 속에서 목소리만 들린다. 모습은 다음 대사부터 드러난다.
        /// </summary>
        private static readonly bool[] LineHanyoungVisible = { false, true, true, true, true, true };

        /// <summary>
        /// 차지한이 화면에 있는가.
        /// 처음 세 대사는 한영만 나온다. 차지한은 자기 첫 대사에서 처음 모습을 드러낸다.
        /// </summary>
        private static readonly bool[] LineChajihanVisible = { false, false, false, false, true, true };

        // --- 커뮤니티 ---
        private const string TutorialPostViews = "1284";
        private const int TutorialPostLikes = 12;
        private const int TutorialPostDislikes = 2;

        /// <summary>튜토리얼 글이 괴담의 믿음에 보태고 있는 몫(%). 정답 댓글을 달면 절반쯤으로 내려간다.</summary>
        private const int TutorialPostBelief = 46;
        private const int TutorialPostBeliefAfter = 23;

        /// <summary>지금 이 글이 들고 있는 몫. 댓글을 단 뒤에 바뀐다.</summary>
        private int _postBelief = TutorialPostBelief;
        private const string PostTimeTextId = "ui.net.post_time_tutorial";
        private const string ChoiceHintTextId = "tutorial.comment.hint";
        private const string PcLine1TextId = "tutorial.pc.001";
        private const string PcLine2TextId = "tutorial.pc.002";
        private const string PcLine3TextId = "tutorial.pc.003";
        private const string LikeWarnTextId = "tutorial.pc.like_warn";
        private const string DislikeGoodTextId = "tutorial.pc.dislike_good";

        /// <summary>바탕화면에서 괴담넷 아이콘을 가리키는 ID. 씬의 아이콘 설정과 같아야 한다.</summary>
        public const string NetAppId = "gwedamnet";

        /// <summary>메모장 아이콘의 ID. 컴퓨터와 휴대폰이 같은 것을 쓴다.</summary>
        public const string MemoAppId = "memo";
        private const string WrongTextId = "tutorial.comment.wrong";
        private const string NeedFieldMarkTextId = "ui.net.need_field_mark";
        private const string NeedFieldHanyoungTextId = "tutorial.comment.need_field_hanyoung";
        private const string CorrectTextId = "tutorial.comment.correct";
        private const string DoneTextId = "tutorial.comment.done";
        private const string PlayerNameTextId = "tutorial.char.chajihan";

        /// <summary>정답 댓글이 깎는 믿음도. 튜토리얼에서만 쓰는 값이다.</summary>
        private const float TutorialBeliefDrop = 12f;

        /// <summary>튜토리얼 시작 시점의 믿음도. 감소를 눈으로 보려면 0이 아니어야 한다.</summary>
        private const int TutorialStartBelief = 40;

        private List<CommunityComment> _comments;
        private List<TutorialCommentChoice> _choices;
        private bool _censored;

        public bool IsRunning { get; private set; }

        private void Awake()
        {
            ServiceRegistry.TryGet(out _ui);
            ServiceRegistry.TryGet(out _loc);
            ServiceRegistry.TryGet(out _internet);
            ServiceRegistry.TryGet(out _belief);
        }

        // ------------------------------------------------------------- 시작

        /// <summary>타이틀의 시작 버튼이 부른다.</summary>
        public void StartTutorial()
        {
            if (_ui == null) ServiceRegistry.TryGet(out _ui);
            if (_loc == null) ServiceRegistry.TryGet(out _loc);
            if (_internet == null) ServiceRegistry.TryGet(out _internet);
            if (_belief == null) ServiceRegistry.TryGet(out _belief);

            if (_dialogueScreen == null || _ui == null)
            {
                Debug.LogError("[TutorialDirector] 대화 화면이 연결되지 않았다. 씬을 다시 빌드할 것.");
                return;
            }

            PrepareSandbox();

            // 튜토리얼은 언제 돌려도 처음부터다.
            // 게시판은 적어 둔 값으로 되돌리고, 시계도 그날 밤 그 시각으로 되돌린다.
            _boardEntries = null;
            _beliefCarry = 0f;
            UrbanLegendBureau.Core.GameClock.Restart();

            // 메모장도 처음으로 되돌린다. 적어 둔 것을 비우고 다시 잠근 뒤, 등급표 한 장을 넣어 둔다.
            // 잠그지 않으면 한영이 메모하라고 이르는 마디에서 열렸다는 알림이 뜨지 않는다.
            MemoScreen.ResetAll();
            MemoScreen.SeedDefaultNotes();

            IsRunning = true;
            _finished = false;
            _censored = false;
            _lineIndex = 0;
            _postBelief = TutorialPostBelief;
            _inBriefing = false;
            _inFieldTalk = false;
            _fieldLineIndex = 0;
            _briefingIndex = 0;
            _briefingInsert.Clear();
            _dialogueScreen.ClearChoices();

            // 게시판 목록을 여기서 만들어 둔다.
            // 전체 믿음도가 이 목록에서 나오므로 컴퓨터를 켜기 전에 이미 있어야 한다.
            BuildBoardEntries();
            BuildComments();
            BuildChoices();

            _dialogueScreen.SetAdvanceHandler(OnAdvanceClicked);

            if (_ui.Count == 0) _ui.Push(_dialogueScreen);
            else _ui.Replace(_dialogueScreen);

            ShowCurrentLine();
            Debug.Log("[TutorialDirector] 튜토리얼 시작 | 전용 저장본 사용 (실제 저장과 분리)");
        }

        /// <summary>
        /// 튜토리얼 전용 저장본을 만든다.
        /// 파일로 쓰지 않으므로 실제 진행 상황이 덮이지 않는다.
        /// </summary>
        private void PrepareSandbox()
        {
            _sandbox = new SaveService();
            _sandbox.Initialize();
            _sandbox.NewGame();
            _sandbox.Current.global.beliefLevel = TutorialStartBelief;
        }

        // ------------------------------------------------------------- 대화

        private void ShowCurrentLine()
        {
            bool hanyoung = LineIsHanyoung[_lineIndex];
            string lineId = LineTextIds[_lineIndex];
            string nameId = hanyoung ? HanyoungNameTextId : ChajihanNameTextId;
            float brightness = LineBrightness[_lineIndex];

            bool chajihanVisible = LineChajihanVisible[_lineIndex];

            // 차지한이 나오기 전에는 한영이 화면 가운데에 선다. 등장한 뒤에는 원래 자리로 돌아간다.
            // 자리를 먼저 잡아야 강조 움직임이 옳은 위치에서 시작한다.
            _dialogueScreen.SetSoloLayout(!chajihanVisible);

            _dialogueScreen.ShowLine(hanyoung,
                () => _loc.Get(nameId),
                () => _loc.Get(lineId),
                brightness,
                leftVisible: LineHanyoungVisible[_lineIndex],
                rightVisible: chajihanVisible);
        }

        /// <summary>화면을 눌렀을 때. 한 번의 입력은 한 줄만 넘긴다.</summary>
        private void OnAdvanceClicked()
        {
            if (!IsRunning) return;

            // 현장 대사는 현장 화면이 제 버튼으로 넘긴다. 여기서는 다루지 않는다.
            if (_inFieldTalk) return;

            // 부서 설명 중이면 그쪽 흐름을 따른다. 첫 대화와 같은 화면을 함께 쓴다.
            if (_inBriefing)
            {
                ShowBriefingStep();
                return;
            }

            if (_lineIndex + 1 < LineTextIds.Length)
            {
                _lineIndex++;
                ShowCurrentLine();
                return;
            }

            OpenDesktop();
        }

        // ------------------------------------------------------------- 컴퓨터

        /// <summary>대화가 끝나면 차지한의 컴퓨터 앞에 앉는다.</summary>
        private void OpenDesktop()
        {
            if (_desktopScreen == null)
            {
                Debug.LogError("[TutorialDirector] 바탕화면이 연결되지 않았다. 씬을 다시 빌드할 것.");
                return;
            }

            // 여기서부터는 차지한의 컴퓨터를 들여다보는 화면이다. 화살표도 게임 안의 것으로 바꾼다.
            GamePointer.SetVisible(true);

            _desktopScreen.Bind(OnAppClicked);
            PushBeliefToTaskbar();
            _desktopScreen.LockAllApps();            // 한영의 안내가 끝나기 전에는 아무것도 못 누른다

            // 괴담넷 아이콘에 새 글 개수를 건다.
            // 튜토리얼에서는 게시판의 글이 전부 아직 안 읽은 것이라 목록 개수가 그대로 새 글 개수다.
            _desktopScreen.SetAppBadge(NetAppId, BoardEntries.Count);

            if (_ui.Contains(_dialogueScreen)) _ui.Close(_dialogueScreen);

            if (_ui.Count == 0) _ui.Push(_desktopScreen);
            else _ui.Replace(_desktopScreen);

            Debug.Log("[TutorialDirector] 컴퓨터 화면");

            // 여기서는 한영이 옆에서 안내만 한다. 인물은 세우지 않는다.
            ShowTalk(PcLine1TextId, AfterTalk.OpenDesktopNet, showCharacter: false);
        }

        /// <summary>바탕화면 아이콘을 눌렀을 때. 열리는 것은 괴담넷과 메모장이다.</summary>
        private void OnAppClicked(string appId)
        {
            if (appId == MemoAppId)
            {
                OpenDesktopMemo();
                return;
            }

            if (appId != NetAppId) return;
            OpenCommunityBoard();
        }

        /// <summary>
        /// 컴퓨터로 메모장을 연다. 바탕화면 위에 뜬 창이다.
        /// 적어 둔 것은 휴대폰으로 열어도 같다. 화면도 글도 하나뿐이기 때문이다.
        /// </summary>
        private void OpenDesktopMemo()
        {
            if (_memoScreen == null || !MemoScreen.IsUnlocked) return;

            _memoScreen.SetShape(false);
            _memoScreen.SetKeepsUnderlyingUsable(false);
            _memoScreen.Bind(CloseDesktopMemo);

            if (!_ui.Contains(_memoScreen)) _ui.Push(_memoScreen);

            Debug.Log("[TutorialDirector] 메모장을 열었다");
        }

        private void CloseDesktopMemo()
        {
            if (_memoScreen != null && _ui.Contains(_memoScreen)) _ui.Close(_memoScreen);
        }

        /// <summary>괴담넷을 열면 게시판 목록부터 보인다.</summary>
        private void OpenCommunityBoard()
        {
            if (_communityScreen == null) return;

            // 컴퓨터로 여는 것이다. 앞서 휴대폰으로 보다 나왔더라도 창 모양으로 되돌린다.
            _communityScreen.SetShape(false);
            _communityScreen.SetKeepsUnderlyingUsable(false);

            _communityScreen.BindWindow(null);        // 튜토리얼 중에는 창을 닫을 수 없다
            _communityScreen.BindBoard(_boardEntries, OnBoardEntryClicked);
            _communityScreen.ShowBoard(true);
            _communityScreen.ShowNotice(null);

            // 바탕화면 위에 얹는다. 바꿔 끼우지 않아야 아래에서 작업 표시줄이 계속 보인다.
            if (!_ui.Contains(_communityScreen)) _ui.Push(_communityScreen);

            // 목록을 열었으니 새 글 표시는 지운다. 읽은 것을 새것이라고 두면 안 된다.
            if (_desktopScreen != null) _desktopScreen.SetAppBadge(NetAppId, 0);

            Debug.Log("[TutorialDirector] 괴담넷 | 게시판 목록 " + _boardEntries.Count + "개");

            ShowTalk(PcLine2TextId, AfterTalk.OpenHotPost, showCharacter: false);
        }

        private void OnBoardEntryClicked(CommunityBoardEntry entry)
        {
            if (entry == null || !entry.Openable) return;
            OpenCommunity();
        }

        private List<CommunityBoardEntry> _boardEntries;

        /// <summary>
        /// 괴담넷 게시판 목록. 컴퓨터로 보든 휴대폰으로 보든 같은 목록이다.
        /// 목록을 고치는 곳은 아래 BuildBoardEntries 한 곳뿐이다.
        /// </summary>
        public IReadOnlyList<CommunityBoardEntry> BoardEntries
        {
            get
            {
                if (_boardEntries == null) BuildBoardEntries();
                return _boardEntries;
            }
        }

        /// <summary>튜토리얼에서 여는 그 인기글. 휴대폰에서도 같은 글을 연다.</summary>
        public UrbanLegendBureau.Data.WebPageSO HotPage => _tutorialPage;

        /// <summary>그 글의 조회수. 목록에 적힌 숫자와 어긋나지 않게 여기서만 들고 있는다.</summary>
        public int HotPageViews => int.Parse(TutorialPostViews);

        /// <summary>
        /// 게시판이 만들어 내고 있는 전체 믿음도(%).
        /// 컴퓨터 작업 표시줄과 휴대폰 상태 줄이 같은 이 숫자를 보여준다.
        /// </summary>
        public int BoardBelief
        {
            get
            {
                if (_boardEntries == null) BuildBoardEntries();
                return CalculateBoardBelief();
            }
        }

        /// <summary>1%에 못 미치는 몫. 버리지 않고 모아 두었다가 1%가 차면 올린다.</summary>
        private float _beliefCarry;
        private float _driftCarry;

        /// <summary>
        /// 괴담이 퍼진 만큼 그 괴담을 실어 나르는 글들의 믿음도 함께 오른다.
        ///
        /// 조사하고 있는 사건의 글만 오른다. 같은 게시판에 있어도 다른 괴담 이야기는 그대로다.
        /// 한 번에 오르는 몫이 작아 소수점이 대부분이므로, 모아 두었다가 1%가 차면 올린다.
        /// </summary>
        public void RaiseBoardBelief(float amount, string legendId)
        {
            if (amount <= 0f || string.IsNullOrEmpty(legendId)) return;

            int step = Accumulate(ref _beliefCarry, amount);
            if (step <= 0) return;

            int raised = Raise(step, entry => entry.LegendId == legendId);
            if (raised > 0)
            {
                PushBeliefToTaskbar();
                Debug.Log($"[TutorialDirector] 괴담이 퍼진다 | {legendId} 글 {raised}개 +{step}% | 전체 {BoardBelief}%");
            }
        }

        /// <summary>
        /// 시간이 흐르는 것만으로도 글은 조금씩 더 믿긴다.
        ///
        /// 사람들이 읽고 옮기는 데 시간이 드는 것이다. 어느 괴담이든 가리지 않는다.
        /// 지금 조사하고 있는 글도 여기에 든다. 조사로 오르는 몫과 따로 쌓인다.
        /// </summary>
        public void DriftBoardBelief(int minutes)
        {
            if (minutes <= 0) return;

            int step = Accumulate(ref _driftCarry, minutes * BeliefPerMinute);
            if (step <= 0) return;

            int raised = Raise(step, entry => true);
            if (raised > 0)
            {
                PushBeliefToTaskbar();
                Debug.Log($"[TutorialDirector] 시간이 흐른다 | 글 {raised}개 +{step}% | 전체 {BoardBelief}%");
            }
        }

        /// <summary>게임 안에서 1분이 지날 때 글이 더 믿기는 몫(%). 아주 작게 잡는다.</summary>
        private const float BeliefPerMinute = 0.01f;

        /// <summary>1%가 찰 때까지 모은다. 찬 만큼만 돌려주고 나머지는 남겨 둔다.</summary>
        private static int Accumulate(ref float carry, float amount)
        {
            carry += amount;

            int step = Mathf.FloorToInt(carry);
            if (step > 0) carry -= step;
            return step;
        }

        /// <summary>조건에 맞는 글의 믿음도를 올린다. 믿음에 보태는 것이 없는(0인) 글은 건드리지 않는다.</summary>
        private int Raise(int step, System.Func<CommunityBoardEntry, bool> match)
        {
            if (_boardEntries == null) BuildBoardEntries();

            int raised = 0;
            foreach (var entry in _boardEntries)
            {
                if (entry == null || entry.BeliefPercent <= 0 || entry.BeliefPercent >= 100) continue;
                if (!match(entry)) continue;

                entry.BeliefPercent = Mathf.Min(100, entry.BeliefPercent + step);
                raised++;
            }
            return raised;
        }

        /// <summary>
        /// 그 글에 달린 댓글. 휴대폰으로 열어도 같은 댓글이 보인다.
        /// 플레이어가 단 댓글도 이 목록에 들어 있으므로 함께 따라온다.
        /// </summary>
        public List<CommunityComment> Comments
        {
            get
            {
                if (_comments == null) BuildComments();
                return _comments;
            }
        }

        /// <summary>
        /// 게시판 목록. 인기글 하나만 열리고 나머지는 자리를 채운다.
        /// 튜토리얼이 엉뚱한 글로 새지 않게 하기 위해서다.
        /// </summary>
        private void BuildBoardEntries()
        {
            // 인기글이 시간과 상관없이 맨 위에 붙고, 나머지는 새로 올라온 것부터 내려간다.
            // 실제 게시판이 그렇게 늘어놓는다.
            // 올라온 시각은 "게임을 시작한 밤 10시 30분에서 몇 분 전인가" 로 적는다.
            // 화면에 적히는 문구는 지금 시각을 보고 만든다. 조사하는 동안 시간이 흐르면 함께 밀린다.
            //
            // LegendId 를 적은 글만 그 사건을 조사할 때 믿음이 오른다.
            // 다른 괴담 이야기도 믿음 수치를 들고 있지만, 막차 사건과는 상관이 없다.
            _boardEntries = new List<CommunityBoardEntry>
            {
                new CommunityBoardEntry
                {
                    TitleTextId = _tutorialPage != null ? _tutorialPage.TitleTextId : string.Empty,
                    MetaTextId = "board.subway.meta",
                    IsHot = true,
                    Openable = true,
                    Page = _tutorialPage,
                    BeliefPercent = TutorialPostBelief,
                    LegendId = SubwayLegendId,
                    PostedMinutesAgo = 2697,                 // 어제 01:33
                    Likes = TutorialPostLikes,
                    Dislikes = TutorialPostDislikes,
                },
                new CommunityBoardEntry { TitleTextId = "board.filler.009", MetaTextId = "board.filler.009.meta", IsHot = true, BeliefPercent = 31, PostedMinutesAgo = 1523 },

                // 여기부터 최신순. 괴담과 상관없는 글은 믿음에 보태는 것이 없어 0이다.
                //
                // 사흘 전에 문을 연 사이트다. 처음 이틀은 글이 드문드문 올라오다가
                // 요 며칠 사이 부쩍 늘었다. 괴담이 퍼지는 중이라는 것을 글 수로 보여준다.

                // --- 오늘 ---
                new CommunityBoardEntry { TitleTextId = "board.filler.006", MetaTextId = "board.filler.006.meta", BeliefPercent = 28, PostedMinutesAgo = 12 },
                new CommunityBoardEntry { TitleTextId = "board.filler.007", MetaTextId = "board.filler.007.meta", BeliefPercent = 19, PostedMinutesAgo = 34 },
                new CommunityBoardEntry { TitleTextId = "board.filler.008", MetaTextId = "board.filler.008.meta", PostedMinutesAgo = 60 },
                new CommunityBoardEntry { TitleTextId = "board.filler.004", MetaTextId = "board.filler.004.meta", BeliefPercent = 25, PostedMinutesAgo = 120 },
                new CommunityBoardEntry { TitleTextId = "board.filler.005", MetaTextId = "board.filler.005.meta", PostedMinutesAgo = 240 },
                new CommunityBoardEntry { TitleTextId = "board.filler.001", MetaTextId = "board.filler.001.meta", PostedMinutesAgo = 360 },
                new CommunityBoardEntry { TitleTextId = "board.filler.002", MetaTextId = "board.filler.002.meta", PostedMinutesAgo = 540 },
                new CommunityBoardEntry { TitleTextId = "board.filler.010", MetaTextId = "board.filler.010.meta", PostedMinutesAgo = 660 },

                // --- 어제 ---
                // 막차연구회의 글은 같은 괴담을 좇고 있다. 이 사건을 조사하면 함께 오른다.
                new CommunityBoardEntry { TitleTextId = "board.filler.003", MetaTextId = "board.filler.003.meta", LegendId = SubwayLegendId, BeliefPercent = 16, PostedMinutesAgo = 1360 },
                new CommunityBoardEntry { TitleTextId = "board.filler.011", MetaTextId = "board.filler.011.meta", BeliefPercent = 24, PostedMinutesAgo = 1578 },
                new CommunityBoardEntry { TitleTextId = "board.filler.012", MetaTextId = "board.filler.012.meta", PostedMinutesAgo = 1677 },
                new CommunityBoardEntry { TitleTextId = "board.filler.013", MetaTextId = "board.filler.013.meta", LegendId = SubwayLegendId, BeliefPercent = 30, PostedMinutesAgo = 1945 },
                new CommunityBoardEntry { TitleTextId = "board.filler.014", MetaTextId = "board.filler.014.meta", PostedMinutesAgo = 2229 },

                // --- 이틀 전 ---
                new CommunityBoardEntry { TitleTextId = "board.filler.015", MetaTextId = "board.filler.015.meta", BeliefPercent = 17, PostedMinutesAgo = 2810 },
                new CommunityBoardEntry { TitleTextId = "board.filler.016", MetaTextId = "board.filler.016.meta", PostedMinutesAgo = 3328 },
                new CommunityBoardEntry { TitleTextId = "board.filler.017", MetaTextId = "board.filler.017.meta", BeliefPercent = 21, PostedMinutesAgo = 3552 },

                // --- 사흘 전. 사이트가 문을 연 날 ---
                new CommunityBoardEntry { TitleTextId = "board.filler.018", MetaTextId = "board.filler.018.meta", BeliefPercent = 13, PostedMinutesAgo = 5023 },
                new CommunityBoardEntry { TitleTextId = "board.filler.019", MetaTextId = "board.filler.019.meta", PostedMinutesAgo = 5070 },
            };
        }

        /// <summary>막차 괴담의 ID. 이 괴담을 실어 나르는 글만 그 사건 조사에 반응한다.</summary>
        private const string SubwayLegendId = "legend_subway_last_train";

        /// <summary>
        /// 이 괴담에 묶여 있는 글들의 평균 믿음도(%).
        ///
        /// 하나의 괴담을 여러 글이 나눠 싣고 있다. 사건이 얼마나 굳어졌는지는 그 글들을 함께 봐야 안다.
        /// 조사하는 글 하나만 보면, 그 글을 반박해 두고도 곁의 글이 소문을 떠받치는 것을 놓친다.
        /// </summary>
        public int GetLegendBelief(string legendId)
        {
            if (string.IsNullOrEmpty(legendId)) return 0;
            if (_boardEntries == null) BuildBoardEntries();

            int total = 0;
            int counted = 0;

            foreach (var entry in _boardEntries)
            {
                if (entry == null || entry.LegendId != legendId) continue;

                total += entry.BeliefPercent;
                counted++;
            }

            return counted == 0 ? 0 : Mathf.RoundToInt((float)total / counted);
        }

        // ------------------------------------------------------------- 커뮤니티

        /// <summary>한영의 말이 끝난 뒤 무엇을 할 것인가.</summary>
        private enum AfterTalk
        {
            /// <summary>다시 댓글을 고르게 한다.</summary>
            BackToChoices,

            /// <summary>고른 댓글을 실제로 올린다.</summary>
            PostComment,

            /// <summary>튜토리얼을 끝낸다.</summary>
            Finish,

            /// <summary>괴담넷 아이콘만 누를 수 있게 열어 준다.</summary>
            OpenDesktopNet,

            /// <summary>게시판에서 인기글만 누를 수 있게 열어 준다.</summary>
            OpenHotPost
        }

        private AfterTalk _afterTalk;

        private void OpenCommunity()
        {
            if (_communityScreen == null)
            {
                Debug.LogError("[TutorialDirector] 커뮤니티 화면이 연결되지 않았다.");
                return;
            }

            // 이 글의 반응과 올라온 시각은 게시판 목록의 그 줄이 들고 있다.
            // 휴대폰으로 같은 글을 열어도 같은 것을 보게 하려면 한 곳에서만 들고 있어야 한다.
            var hot = HotEntry;

            _communityScreen.BindPage(_tutorialPage, int.Parse(TutorialPostViews), hot.PostedMinutesAgo,
                hot.Likes, hot.Dislikes, _postBelief, hot.LikePressed, hot.DislikePressed);
            _communityScreen.BindComments(_comments);
            _communityScreen.BindChoices(_choices, BuildChoiceLabel, OnChoiceSelected, BuildChoiceNote);
            _communityScreen.BindReactions(OnLikeToggled, OnDislikeToggled);
            _communityScreen.ShowNotice(null);
            _communityScreen.ShowBoard(false);       // 목록에서 글로 들어간다

            // 대화 화면을 확실히 닫는다. 위에 팝업이 떠 있어도 스택에 남지 않게 한다.
            if (_ui.Contains(_dialogueScreen)) _ui.Close(_dialogueScreen);

            if (!_ui.Contains(_communityScreen)) _ui.Push(_communityScreen);

            Debug.Log("[TutorialDirector] 커뮤니티 글 | 댓글 선택 " + _choices.Count + "개");

            // 설명은 일반 문구가 아니라 한영이 직접 말한다. 여기서는 모습도 함께 보인다.
            ShowTalk(PcLine3TextId, AfterTalk.BackToChoices);
        }

        /// <summary>
        /// 좋아요를 눌렀을 때. 한영이 말린다.
        /// 취소했을 때는 아무 말도 하지 않는다. 같은 말을 두 번 들을 이유가 없다.
        /// </summary>
        private void OnLikeToggled(bool pressed)
        {
            HotEntry.LikePressed = pressed;
            if (!pressed) return;
            ShowTalk(LikeWarnTextId, AfterTalk.BackToChoices);
        }

        /// <summary>싫어요를 눌렀을 때. 한영이 맞장구를 친다.</summary>
        private void OnDislikeToggled(bool pressed)
        {
            HotEntry.DislikePressed = pressed;
            if (!pressed) return;
            ShowTalk(DislikeGoodTextId, AfterTalk.BackToChoices);
        }

        /// <summary>튜토리얼이 여는 그 인기글의 목록 줄. 반응과 올라온 시각을 이 줄이 들고 있다.</summary>
        private CommunityBoardEntry HotEntry
        {
            get
            {
                if (_boardEntries == null) BuildBoardEntries();
                return _boardEntries[0];
            }
        }

        // ------------------------------------------------------------- 한영의 말

        /// <summary>
        /// 커뮤니티 화면 위에 한영을 띄우고 한 줄 말하게 한다.
        ///
        /// 대화 화면을 새로 만들지 않고 같은 DialogueScreen을 쓴다.
        /// 이 화면은 아래를 가리지 않는 팝업이라 커뮤니티가 그대로 보이고,
        /// 화면 전체를 덮는 진행 버튼이 있어 말하는 동안에는 선택지를 누를 수 없다.
        /// </summary>
        private void ShowTalk(string lineTextId, AfterTalk after, bool showCharacter = true)
        {
            if (_talkScreen == null)
            {
                Debug.LogError("[TutorialDirector] 커뮤니티용 대화 화면이 연결되지 않았다.");
                return;
            }

            _afterTalk = after;
            _talkScreen.SetAdvanceHandler(OnTalkAdvanced);

            // 화면을 먼저 올린다.
            // 꺼져 있는 화면에 대사를 넣으면 강조 움직임 코루틴이 시작되지 못한다.
            if (!_ui.Contains(_talkScreen)) _ui.Push(_talkScreen);

            // 말하는 동안에는 괴담넷에서 아무것도 고를 수 없다. 끌어서 내리는 것만 된다.
            if (_communityScreen != null) _communityScreen.SetControlsEnabled(false);

            // 겹침 대화에는 한영 혼자 나온다. 처음 대화에서처럼 화면 가운데에 세운다.
            _talkScreen.SetSoloLayout(showCharacter);
            _talkScreen.ShowLine(true,
                () => _loc.Get(HanyoungNameTextId),
                () => _loc.Get(lineTextId),
                1f,
                leftVisible: showCharacter,
                rightVisible: false);

            Debug.Log("[TutorialDirector] 한영 대사 | " + lineTextId + " -> 끝나면 " + after);
        }

        /// <summary>
        /// 인물의 말이 아닌 나레이션을 같은 상자에 띄운다.
        /// 대화 화면을 그대로 쓰되 이름을 비우고 글자색만 달리한다.
        /// </summary>
        private void ShowNarration(string lineTextId, AfterTalk after)
        {
            if (_talkScreen == null) return;

            _afterTalk = after;
            _talkScreen.SetAdvanceHandler(OnTalkAdvanced);

            if (!_ui.Contains(_talkScreen)) _ui.Push(_talkScreen);

            // 나레이션 동안에도 아무것도 고를 수 없다.
            if (_communityScreen != null) _communityScreen.SetControlsEnabled(false);

            // 나레이션에는 인물을 세우지 않는다. 대사 상자만 남는다.
            _talkScreen.ShowNarration(() => _loc.Get(lineTextId), leftVisible: false, rightVisible: false);

            Debug.Log("[TutorialDirector] 나레이션 | " + lineTextId + " -> 끝나면 " + after);
        }

        /// <summary>한영의 말을 넘겼을 때. 말이 끝나야 다음 상태로 간다.</summary>
        private void OnTalkAdvanced()
        {
            if (_ui.Contains(_talkScreen)) _ui.Close(_talkScreen);

            // 상자를 닫았으니 다시 고를 수 있다.
            if (_communityScreen != null) _communityScreen.SetControlsEnabled(true);

            switch (_afterTalk)
            {
                case AfterTalk.PostComment:
                    PostCorrectComment();
                    break;

                case AfterTalk.Finish:
                    StartBriefing();
                    break;

                case AfterTalk.OpenDesktopNet:
                    // 안내가 끝나야 아이콘을 누를 수 있다. 그것도 괴담넷 하나만.
                    // 메모장은 아직 잠겨 있다. 한영이 메모하라고 말한 뒤에 열린다.
                    if (_desktopScreen != null)
                    {
                        _desktopScreen.SetAllowedApps(NetAppId);
                        _desktopScreen.SetHintApp(NetAppId);
                    }
                    break;

                case AfterTalk.OpenHotPost:
                    // 목록은 이미 인기글만 눌리게 되어 있다. 여기서는 아무것도 더 열지 않는다.
                    break;

                default:
                    // 다시 고를 수 있는 상태로 돌아간다. 선택지는 그대로 남아 있다.
                    break;
            }
        }

        /// <summary>
        /// 작업 표시줄에 지금 전체 믿음도를 알린다.
        ///
        /// 게시판에 올라온 괴담 글 전체를 기준으로 삼는다.
        /// 글 하나하나가 이 괴담을 얼마나 믿게 만들고 있는지를 더해 그 글 수로 나눈다.
        /// 괴담과 무관한 글(0%)은 세지 않는다. 믿음과 아무 상관이 없는 글이라 평균을 흐릴 뿐이다.
        /// 그래서 괴담 글에 댓글을 달아 몫을 깎으면 이 숫자가 따라 내려간다.
        /// </summary>
        private void PushBeliefToTaskbar()
        {
            int belief = CalculateBoardBelief();

            // 휴대폰 상태 줄도 같은 숫자를 보여준다. 값은 한 곳에 둔다.
            UrbanLegendBureau.Core.GameStatus.SetBelief(belief);

            if (_desktopScreen == null) return;
            _desktopScreen.SetBelief(belief);
        }

        /// <summary>괴담 글들을 기준으로 낸 믿음도(%).</summary>
        private int CalculateBoardBelief()
        {
            if (_boardEntries == null) return 0;

            int total = 0;
            int counted = 0;

            foreach (var entry in _boardEntries)
            {
                if (entry == null || entry.BeliefPercent <= 0) continue;

                total += entry.BeliefPercent;
                counted++;
            }

            return counted == 0 ? 0 : Mathf.RoundToInt((float)total / counted);
        }

        /// <summary>이 글의 믿음 몫을 목록과 글 화면에 함께 반영한다.</summary>
        private void ApplyPostBelief()
        {
            if (_boardEntries != null)
            {
                foreach (var entry in _boardEntries)
                {
                    if (entry != null && entry.Openable) entry.BeliefPercent = _postBelief;
                }
            }

            if (_communityScreen != null) _communityScreen.SetPostBelief(_postBelief);
        }

        private void BuildComments()
        {
            // 전부 익명이면 사람이 모인 곳처럼 보이지 않는다. 닉네임을 쓰는 사람도 섞어 둔다.
            _comments = new List<CommunityComment>
            {
                new CommunityComment { AuthorTextId = "ui.net.author_nick_1", BodyTextId = "tutorial.comment.existing_1" },
                new CommunityComment { AuthorTextId = "ui.net.author_anon", BodyTextId = "tutorial.comment.existing_2" },
                new CommunityComment { AuthorTextId = "ui.net.author_nick_2", BodyTextId = "tutorial.comment.existing_3" },
                new CommunityComment { AuthorTextId = "ui.net.author_anon", BodyTextId = "tutorial.comment.existing_4" },
            };
        }

        private void BuildChoices()
        {
            _choices = new List<TutorialCommentChoice>
            {
                new TutorialCommentChoice
                {
                    ChoiceId = "amplify",
                    SummaryTextId = "tutorial.choice.1.summary",
                    BodyTextId = "tutorial.choice.1.body",
                    Kind = TutorialCommentKind.Amplify,
                },
                new TutorialCommentChoice
                {
                    ChoiceId = "debunk",
                    SummaryTextId = "tutorial.choice.2.summary",
                    BodyTextId = "tutorial.choice.2.body",
                    Kind = TutorialCommentKind.Debunk,
                },
                new TutorialCommentChoice
                {
                    ChoiceId = "remedy",
                    SummaryTextId = "tutorial.choice.3.summary",
                    BodyTextId = "tutorial.choice.3.body",
                    Kind = TutorialCommentKind.Remedy,

                    // 대처법은 현장에서 확인해야 아는 것이다.
                    // CCTV 영상은 현장 지점에서만 얻을 수 있으므로, 그 단서를 조건으로 건다.
                    RequiredClueId = "clue_subway_003",
                },
            };
        }

        /// <summary>
        /// 선택지 문구. 실제로 올라갈 댓글 그대로만 보여준다.
        /// 무엇을 노리는 댓글인지는 적지 않는다. 플레이어가 읽고 스스로 판단할 일이다.
        /// 조건이 모자란 것만 그 사실을 덧붙인다.
        /// </summary>
        private string BuildChoiceLabel(TutorialCommentChoice choice)
        {
            return choice == null ? string.Empty : _loc.Get(choice.BodyTextId);
        }

        /// <summary>
        /// 선택지 칸 오른쪽 아래의 단서. 아직 쓸 수 없는 댓글에만 붙는다.
        /// 댓글 글자에 섞지 않는다. 그건 실제로 올라갈 내용이 아니기 때문이다.
        /// </summary>
        private string BuildChoiceNote(TutorialCommentChoice choice)
        {
            if (choice == null || CanUseChoice(choice)) return string.Empty;
            return "[" + _loc.Get(NeedFieldMarkTextId) + "]";
        }

        /// <summary>
        /// 지금 이 댓글을 쓸 수 있는가.
        /// 임시 플래그가 아니라 실제 보유 단서로 판단한다. 현장 조사를 하면 저절로 풀린다.
        /// </summary>
        public bool CanUseChoice(TutorialCommentChoice choice)
        {
            if (choice == null || string.IsNullOrEmpty(choice.RequiredClueId)) return true;
            if (_sandbox == null || _sandbox.Current == null) return false;

            return _sandbox.Current.acquiredClueIds.Contains(choice.RequiredClueId);
        }

        private void OnChoiceSelected(TutorialCommentChoice choice)
        {
            if (choice == null || _finished) return;

            if (!CanUseChoice(choice))
            {
                // 조건이 모자라면 아무것도 일어나지 않는다. 선택지는 그대로 남는다.
                // 이유는 한영이 직접 말한다. 선택지 칸에도 이미 표시가 붙어 있으므로 따로 적지 않는다.
                ShowTalk(NeedFieldHanyoungTextId, AfterTalk.BackToChoices);

                Debug.Log($"[TutorialDirector] 댓글 잠김 | {choice.ChoiceId} (필요 단서 {choice.RequiredClueId})");
                return;
            }

            _communityScreen.ShowNotice(null);

            if (choice.Kind != TutorialCommentKind.Debunk)
            {
                // 틀린 선택이다. 사건 실패도, 게임오버도 아니다. 다시 고르면 된다.
                ShowTalk(WrongTextId, AfterTalk.BackToChoices);
                Debug.Log($"[TutorialDirector] 잘못된 댓글 | {choice.ChoiceId} (되돌릴 수 있는 선택)");
                return;
            }

            // 정답이다. 칭찬을 먼저 듣고, 말이 끝나면 댓글이 올라간다.
            _pendingChoice = choice;
            ShowTalk(CorrectTextId, AfterTalk.PostComment);
        }

        // ------------------------------------------------------------- 정답 처리

        private TutorialCommentChoice _pendingChoice;

        private void PostCorrectComment()
        {
            var choice = _pendingChoice;
            if (choice == null) return;

            _pendingChoice = null;
            StartCoroutine(PlayCorrectSequence(choice));
        }

        /// <summary>반응이 하나씩 붙는 간격(초).</summary>
        private const float ReactionInterval = 0.8f;

        /// <summary>마지막 반응과 나레이션 사이의 뜸. 마지막 반응을 읽을 틈을 준다.</summary>
        private const float NarrationDelay = 1.9f;

        private static readonly string[] ReactionTextIds =
        {
            "tutorial.reaction.1",
            "tutorial.reaction.2",
            "tutorial.reaction.3",
        };

        /// <summary>반응을 쓴 사람들. 기존 댓글처럼 닉네임과 익명을 섞는다.</summary>
        private static readonly string[] ReactionAuthorTextIds =
        {
            "ui.net.author_nick_3",
            "ui.net.author_anon",
            "ui.net.author_nick_4",
        };

        /// <summary>
        /// 정답을 고른 뒤의 연출.
        ///
        /// 한 번에 다 붙이지 않고 댓글 -> 반응 하나씩 -> 뜸 -> 나레이션 순으로 흘린다.
        /// 사람들이 실제로 읽고 반응하는 것처럼 보이게 하기 위해서다.
        /// </summary>
        private IEnumerator PlayCorrectSequence(TutorialCommentChoice choice)
        {
            if (_censored) yield break;    // 믿음도가 두 번 깎이지 않게 한다
            _censored = true;

            float before = _belief != null ? _belief.GetBeliefLevel(_sandbox.Current) : 0f;

            // 검열은 기존 시스템이 한다. 플레이어가 고른 결과로 실제 검열이 일어난다.
            var censorResult = CensorResult.NotFound;
            if (_internet != null && _tutorialPage != null)
            {
                censorResult = _internet.TryCensorPage(_sandbox, _tutorialPage.PageId);
            }

            // 플레이어의 댓글이 먼저 올라간다.
            // 커뮤니티에서는 다들 익명이다. 누가 썼는지는 화면이 괄호로 덧붙인다.
            _comments.Add(new CommunityComment
            {
                AuthorTextId = "ui.net.author_anon",
                BodyTextId = choice.BodyTextId,
                IsPlayer = true,
            });
            _communityScreen.BindComments(_comments);
            _communityScreen.BindChoices(null, null, null);      // 고를 것이 없어진다

            // 댓글이 하나씩 붙는 동안에는 손을 못 대게 한다.
            // 화면이 새 댓글을 따라 내려가는 중에 끌어 버리면 자리가 엇갈린다.
            // 잠금은 마지막 나레이션을 넘길 때 함께 풀린다.
            _communityScreen.SetControlsEnabled(false);

            yield return new WaitForSecondsRealtime(ReactionInterval);

            // 사람들이 하나씩 반응한다. 이 반응이 믿음을 깎는 이유다.
            for (int i = 0; i < ReactionTextIds.Length; i++)
            {
                _comments.Add(new CommunityComment
                {
                    AuthorTextId = ReactionAuthorTextIds[i],
                    BodyTextId = ReactionTextIds[i],
                });
                _communityScreen.BindComments(_comments);

                yield return new WaitForSecondsRealtime(ReactionInterval);
            }

            if (_belief != null) _belief.TryReduceBelief(_sandbox.Current, TutorialBeliefDrop);
            float after = _belief != null ? _belief.GetBeliefLevel(_sandbox.Current) : 0f;

            // 이 글이 들고 있던 몫도 함께 내려간다. 목록과 글 화면 모두 같은 값을 쓴다.
            _postBelief = TutorialPostBeliefAfter;
            ApplyPostBelief();
            PushBeliefToTaskbar();

            _finished = true;

            Debug.Log($"[TutorialDirector] 정답 댓글 | 검열={censorResult} | " +
                      $"믿음 {before:F0} -> {after:F0} (튜토리얼 전용 저장본)");

            yield return new WaitForSecondsRealtime(NarrationDelay);

            // 마무리는 한영의 말이 아니라 상황 설명이다. 인물 없이 상자만 띄운다.
            ShowNarration(DoneTextId, AfterTalk.Finish);
        }

        // ------------------------------------------------------------- 부서 설명

        /// <summary>
        /// 괴담넷 일이 끝난 뒤 이어지는 설명 대화의 한 마디.
        ///
        /// 고를 것이 있는 마디는 Choices 를 채운다.
        /// 고르면 그 자리에서 차지한이 고른 말을 하고, 딸린 대답이 이어진 뒤 다시 흐름으로 돌아온다.
        /// </summary>
        private class BriefingStep
        {
            public string TextId;
            public bool Hanyoung = true;

            /// <summary>고를 것. 둘 다 차지한의 말이다.</summary>
            public string[] Choices;

            /// <summary>고른 것에 딸려 나오는 한 영의 대답. 하나만 두면 어느 쪽을 골라도 같다.</summary>
            public string[][] Replies;

            /// <summary>
            /// 오른쪽에 펴 둘 쪽지. 비워 두면 쪽지를 접는다.
            /// 같은 등급을 설명하는 마디끼리는 같은 쪽지를 적어 둔다.
            /// </summary>
            public string NoteKey;
        }

        /// <summary>쪽지 문구의 앞부분. 뒤에 .title / .body 가 붙는다.</summary>
        private const string NotePrefix = "ui.brief.note.";

        private static readonly BriefingStep[] Briefing =
        {
            new BriefingStep { TextId = "tutorial.brief.001" },
            new BriefingStep { TextId = "tutorial.brief.002" },
            new BriefingStep { TextId = "tutorial.brief.003" },

            new BriefingStep
            {
                Choices = new[] { "tutorial.brief.q1.a", "tutorial.brief.q1.b" },
                Replies = new[]
                {
                    new[] { "tutorial.brief.q1.a.1", "tutorial.brief.q1.a.2" },
                    new[] { "tutorial.brief.q1.b.1", "tutorial.brief.q1.b.2" },
                },
            },

            new BriefingStep { TextId = "tutorial.brief.004" },
            new BriefingStep { TextId = "tutorial.brief.004b" },
            new BriefingStep { TextId = "tutorial.brief.005", Hanyoung = false },
            new BriefingStep { TextId = "tutorial.brief.006" },
            new BriefingStep { TextId = "tutorial.brief.007" },
            new BriefingStep { TextId = "tutorial.brief.008" },
            new BriefingStep { TextId = "tutorial.brief.009" },

            new BriefingStep
            {
                Choices = new[] { "tutorial.brief.q2.a", "tutorial.brief.q2.b" },

                // 스스로 알아챈 쪽에는 짧게 맞장구만 친다.
                // 아니라고 본 쪽에는 왜 그런지를 마저 설명한다.
                Replies = new[]
                {
                    new[] { "tutorial.brief.q2.1" },
                    new[] { "tutorial.brief.q2.2" },
                },
            },

            new BriefingStep { TextId = "tutorial.brief.010" },
            new BriefingStep { TextId = "tutorial.brief.011" },
            new BriefingStep { TextId = "tutorial.brief.012", Hanyoung = false },
            new BriefingStep { TextId = "tutorial.brief.013" },
            new BriefingStep { TextId = "tutorial.brief.014", Hanyoung = false },
            new BriefingStep { TextId = "tutorial.brief.015" },
            new BriefingStep { TextId = "tutorial.brief.016", Hanyoung = false },
            new BriefingStep { TextId = "tutorial.brief.017" },
            new BriefingStep { TextId = "tutorial.brief.018" },
            new BriefingStep { TextId = "tutorial.brief.018b" },
            new BriefingStep { TextId = "tutorial.brief.019" },
            new BriefingStep { TextId = "tutorial.brief.019b" },
            new BriefingStep { TextId = "tutorial.brief.020" },
            new BriefingStep { TextId = "tutorial.brief.021", Hanyoung = false },
            new BriefingStep { TextId = "tutorial.brief.022" },
            new BriefingStep { TextId = "tutorial.brief.023" },
            new BriefingStep { TextId = "tutorial.brief.024" },

            // --- 등급 설명 ---
            new BriefingStep { TextId = "tutorial.brief.025", NoteKey = "observation" },
            new BriefingStep { TextId = "tutorial.brief.026", NoteKey = "observation" },
            new BriefingStep { TextId = "tutorial.brief.027", NoteKey = "observation" },
            new BriefingStep { TextId = "tutorial.brief.028", NoteKey = "propagation" },
            new BriefingStep { TextId = "tutorial.brief.029", NoteKey = "propagation" },
            new BriefingStep { TextId = "tutorial.brief.030", NoteKey = "propagation" },
            new BriefingStep { TextId = "tutorial.brief.031", NoteKey = "erosion" },
            new BriefingStep { TextId = "tutorial.brief.032", NoteKey = "erosion" },
            new BriefingStep { TextId = "tutorial.brief.032b", NoteKey = "erosion" },
            new BriefingStep { TextId = "tutorial.brief.033", NoteKey = "erosion" },
            new BriefingStep { TextId = "tutorial.brief.034", NoteKey = "manifestation" },
            new BriefingStep { TextId = "tutorial.brief.035", NoteKey = "manifestation" },
            new BriefingStep { TextId = "tutorial.brief.035b", NoteKey = "manifestation" },
            new BriefingStep { TextId = "tutorial.brief.036", NoteKey = "manifestation" },
            new BriefingStep { TextId = "tutorial.brief.037", NoteKey = "annihilation" },
            new BriefingStep { TextId = "tutorial.brief.038", NoteKey = "annihilation" },
            new BriefingStep { TextId = "tutorial.brief.038b", NoteKey = "annihilation" },
            new BriefingStep { TextId = "tutorial.brief.038c", NoteKey = "annihilation" },
            new BriefingStep { TextId = "tutorial.brief.039", NoteKey = "annihilation" },
            new BriefingStep { TextId = "tutorial.brief.040", NoteKey = "unknown" },
            new BriefingStep { TextId = "tutorial.brief.041", NoteKey = "unknown" },

            new BriefingStep { TextId = "tutorial.brief.042", Hanyoung = false },
            new BriefingStep { TextId = "tutorial.brief.043" },
            new BriefingStep { TextId = "tutorial.brief.044" },
        };

        private int _briefingIndex;

        /// <summary>고른 뒤에 먼저 나와야 할 말들. 이것부터 다 보여주고 본 흐름으로 돌아간다.</summary>
        private readonly Queue<BriefingStep> _briefingInsert = new Queue<BriefingStep>();

        private bool _inBriefing;

        /// <summary>
        /// 괴담넷을 닫고 다시 검은 화면으로 돌아가 부서 설명을 시작한다.
        /// 튜토리얼의 마지막 대목이다. 이것이 끝나야 실제 사건으로 넘어간다.
        /// </summary>
        private void StartBriefing()
        {
            CloseTutorialScreens();
            GamePointer.SetVisible(false);

            _inBriefing = true;
            _briefingIndex = 0;
            _briefingInsert.Clear();

            _dialogueScreen.SetAdvanceHandler(OnAdvanceClicked);
            _dialogueScreen.ClearChoices();
            _dialogueScreen.HideNote();

            if (_ui.Count == 0) _ui.Push(_dialogueScreen);
            else _ui.Replace(_dialogueScreen);

            // 둘 다 화면에 있다. 한 영이 왼쪽, 차지한이 오른쪽이다.
            _dialogueScreen.SetSoloLayout(false);

            Debug.Log("[TutorialDirector] 부서 설명 시작 | " + Briefing.Length + "마디");
            ShowBriefingStep();
        }

        private void ShowBriefingStep()
        {
            // 고른 뒤에 끼워 넣은 말이 남아 있으면 그것부터 보여준다.
            if (_briefingInsert.Count > 0)
            {
                ShowBriefingLine(_briefingInsert.Dequeue());
                return;
            }

            if (_briefingIndex >= Briefing.Length)
            {
                StartFieldTutorial();
                return;
            }

            var step = Briefing[_briefingIndex++];

            if (step.Choices == null || step.Choices.Length == 0)
            {
                ShowBriefingLine(step);
                return;
            }

            AskBriefing(step);
        }

        private void ShowBriefingLine(BriefingStep step)
        {
            string nameId = step.Hanyoung ? HanyoungNameTextId : ChajihanNameTextId;

            // 쪽지가 펴지는 자리는 차지한이 서 있던 자리다. 쪽지를 펴는 동안에는 그를 숨긴다.
            bool hasNote = !string.IsNullOrEmpty(step.NoteKey);
            if (hasNote)
            {
                var key = step.NoteKey;
                _dialogueScreen.ShowNote(
                    () => _loc.Get(NotePrefix + key + ".title"),
                    () => _loc.Get(NotePrefix + key + ".body"));
            }
            else
            {
                _dialogueScreen.HideNote();
            }

            _dialogueScreen.ShowLine(step.Hanyoung,
                () => _loc.Get(nameId),
                () => _loc.Get(step.TextId),
                1f,
                leftVisible: true,
                rightVisible: !hasNote);
        }

        /// <summary>고를 것을 내놓는다. 고르면 그 말부터 차지한이 하고 대답이 이어진다.</summary>
        private void AskBriefing(BriefingStep step)
        {
            var labels = new List<System.Func<string>>();
            foreach (var id in step.Choices)
            {
                var captured = id;
                labels.Add(() => _loc.Get(captured));
            }

            _dialogueScreen.ShowChoices(labels, picked => OnBriefingPicked(step, picked));
        }

        private void OnBriefingPicked(BriefingStep step, int picked)
        {
            if (picked < 0 || picked >= step.Choices.Length) return;

            // 고른 말은 차지한이 실제로 한 말이 된다.
            _briefingInsert.Enqueue(new BriefingStep { TextId = step.Choices[picked], Hanyoung = false });

            if (step.Replies != null && step.Replies.Length > 0)
            {
                // 대답을 하나만 두었으면 어느 쪽을 골라도 같은 대답이 나온다.
                var replies = step.Replies[Mathf.Min(picked, step.Replies.Length - 1)];
                foreach (var id in replies)
                {
                    _briefingInsert.Enqueue(new BriefingStep { TextId = id });
                }
            }

            Debug.Log("[TutorialDirector] 부서 설명 선택 | " + step.Choices[picked]);
            ShowBriefingStep();
        }

        // ------------------------------------------------------------- 현장 조사

        /// <summary>튜토리얼이 곧바로 이어 가는 첫 사건. 괴담넷에서 검열한 그 막차 괴담이다.</summary>
        private const string TutorialCaseId = "case_001_subway";

        /// <summary>현장에서 주고받는 말. 앞의 넷은 열차가 오기 전, 뒤의 둘은 열차가 들어오며.</summary>
        private static readonly string[] FieldLineTextIds =
        {
            "tutorial.field.001",
            "tutorial.field.002",
            "tutorial.field.003",
            "tutorial.field.004",
            "tutorial.field.005",
            "tutorial.field.006",
        };

        private static readonly bool[] FieldLineIsHanyoung = { true, true, false, true, true, false };

        /// <summary>열차가 들어오기 시작하는 마디. 여기서부터 위쪽 알림이 바뀐다.</summary>
        private const int FieldTrainArrivesAt = 4;

        private const string TrainArrivingTextId = "ui.field.train_arriving";

        /// <summary>승강장 자리 이름. 현장 위쪽 한 줄에서 CaseDirector 와 같은 문구를 쓴다.</summary>
        private const string PlatformTextId = "ui.field.platform";

        private int _fieldLineIndex;
        private bool _inFieldTalk;

        /// <summary>
        /// 설명이 끝나면 곧바로 첫 사건의 현장으로 넘어간다.
        ///
        /// 여기서부터는 튜토리얼 전용 저장본이 아니라 실제 저장본으로 돈다.
        /// 실제 사건이 시작되는 것이므로 진행이 남아야 한다.
        /// </summary>
        private void StartFieldTutorial()
        {
            _inBriefing = false;
            _dialogueScreen.ClearChoices();
            _dialogueScreen.HideNote();

            // 여기서 튜토리얼은 제 할 일을 다 했다. 저장본에 봤다고 남긴다.
            MarkTutorialSeen();
            _sandbox = null;

            // 튜토리얼에서 여는 사건이므로 진행을 처음으로 되돌리고 시작한다.
            // 예전에 한 번 해 본 사건이어도 그때 쌓인 시간과 확산을 이어받지 않는다.
            if (_caseDirector == null || !_caseDirector.BeginCaseField(TutorialCaseId, fromScratch: true))
            {
                Debug.LogError("[TutorialDirector] 현장으로 넘어가지 못했다. 사건 데이터를 확인할 것.");
                FinishTutorial();
                return;
            }

            _fieldHud = _caseDirector.FieldHud;
            if (_fieldHud == null)
            {
                Debug.LogError("[TutorialDirector] 현장 화면이 연결되지 않았다.");
                IsRunning = false;
                return;
            }

            _inFieldTalk = true;
            _fieldLineIndex = 0;

            Debug.Log("[TutorialDirector] 현장 조사 시작 | " + FieldLineTextIds.Length + "마디");
            ShowFieldLine();
        }

        private void ShowFieldLine()
        {
            if (_fieldLineIndex >= FieldLineTextIds.Length)
            {
                EndFieldTutorial();
                return;
            }

            bool hanyoung = FieldLineIsHanyoung[_fieldLineIndex];
            string nameId = hanyoung ? HanyoungNameTextId : ChajihanNameTextId;
            string lineId = FieldLineTextIds[_fieldLineIndex];

            // 열차가 들어오는 대목에서 곧바로 열차 안으로 들어간다.
            // 승강장에 서서 문이 열리기를 기다리는 시간은 두지 않는다.
            if (_fieldLineIndex == FieldTrainArrivesAt)
            {
                // 열차를 불러들인다. 남은 대사가 흐르는 동안 들어와 서고 문이 열린다.
                // 타는 것은 대사가 끝난 뒤 플레이어가 열린 문을 누를 때다.
                if (_caseDirector != null) _caseDirector.StartTrainArrival();
                // 자리 이름은 그대로 두고 그 뒤에 알림만 붙인다. 어디에 서 있는지를 잃지 않는다.
                _fieldHud.SetTicker(() => _loc.Get(PlatformTextId) + "    " + _loc.Get(TrainArrivingTextId));
            }

            _fieldHud.ShowLine(nameId, () => _loc.Get(lineId), OnFieldLineAdvanced);
        }

        private void OnFieldLineAdvanced()
        {
            if (!_inFieldTalk) return;

            // 메모하라고 이른 그 마디를 넘겼다. 여기서부터 메모장을 쓸 수 있다.
            if (_fieldLineIndex == FieldMemoLineAt) UnlockMemo();

            _fieldLineIndex++;
            ShowFieldLine();
        }

        /// <summary>한영이 "메모하고 내용 살펴봐야 하니까" 하는 마디. 이 말이 메모장을 연다.</summary>
        private const int FieldMemoLineAt = 4;

        private const string MemoUnlockedTextId = "ui.memo.unlocked";

        /// <summary>
        /// 메모장을 열어 주고 그 사실을 화면에 알린다.
        ///
        /// 잠긴 것이 열리는 순간을 말로만 지나가면 플레이어는 무엇이 달라졌는지 모른다.
        /// 이미 열려 있으면 알리지 않는다. 다시 볼 때마다 뜨면 그저 성가시다.
        /// </summary>
        private void UnlockMemo()
        {
            if (!MemoScreen.Unlock()) return;

            ShowToast(MemoUnlockedTextId);
            Debug.Log("[TutorialDirector] 메모장이 열렸다");
        }

        /// <summary>막차가 종점에 닿았을 때 한영이 하는 말.</summary>
        private const string TerminusTextId = "tutorial.field.terminus";

        /// <summary>
        /// 막차가 종점에 닿았다고 한영이 알린다. 말이 끝나면 onDone 을 부른다.
        ///
        /// 걸 자리(현장 화면)가 없으면 아무것도 하지 않고 false 를 돌려준다.
        /// 그때는 부르는 쪽이 말 없이 다음으로 넘어간다.
        /// </summary>
        public bool ShowTerminusLine(System.Action onDone)
        {
            var hud = _caseDirector != null ? _caseDirector.FieldHud : _fieldHud;
            if (hud == null || _loc == null) return false;

            _fieldHud = hud;
            hud.ShowLine(HanyoungNameTextId, () => _loc.Get(TerminusTextId), () =>
            {
                hud.ClearSpeech();
                onDone?.Invoke();
            });

            Debug.Log("[TutorialDirector] 막차 종점 | 조사를 닫고 취합으로 넘어간다");
            return true;
        }

        /// <summary>잠깐 떴다 사라지는 알림 한 줄. 시간이 다 되면 스스로 닫힌다.</summary>
        private void ShowToast(string textId)
        {
            if (_toastScreen == null || _ui == null) return;

            _toastScreen.Closed = CloseToast;
            if (!_ui.Contains(_toastScreen)) _ui.Push(_toastScreen);

            _toastScreen.Show(_loc.Get(textId));
        }

        private void CloseToast(UrbanLegendBureau.UI.ToastScreen toast)
        {
            if (toast != null && _ui != null && _ui.Contains(toast)) _ui.Close(toast);
        }

        /// <summary>
        /// 현장 대사가 끝났다. 여기서부터는 평소 현장 조사다.
        /// 튜토리얼은 손을 떼고 사건 쪽에 맡긴다.
        /// </summary>
        /// <summary>
        /// 승강장 대사가 끝났다. 여기서 끝내지 않고 열차에 타기를 기다린다.
        /// 띠를 비워 두어야 열린 문을 누를 수 있다.
        /// </summary>
        private void EndFieldTutorial()
        {
            _inFieldTalk = false;
            _awaitingBoarding = true;

            if (_fieldHud != null) _fieldHud.ClearSpeech();
            if (_caseDirector != null) _caseDirector.RefreshFieldHud();

            Debug.Log("[TutorialDirector] 승강장 대사 끝 | 열차에 타기를 기다린다");
        }

        // ------------------------------------------------------------- 열차에 탄 뒤

        private bool _awaitingBoarding;
        private int _boardLineIndex;
        private int _boardPick = -1;

        /// <summary>고른 말을 차지한이 이미 했는가. 고른 것과 대답 사이에 한 마디가 들어간다.</summary>
        private bool _boardPickSpoken;

        /// <summary>열차에 탄 직후 주고받는 말. 가운데에서 한 번 고르는 것이 끼어든다.</summary>
        private static readonly string[] BoardLineTextIds =
        {
            "tutorial.board.001",
            "tutorial.board.002",
            "tutorial.board.003",
        };

        private static readonly string[] BoardChoiceTextIds =
        {
            "tutorial.board.choice_1",
            "tutorial.board.choice_2",
            "tutorial.board.choice_3",
        };

        private static readonly string[] BoardReplyTextIds =
        {
            "tutorial.board.reply_1",
            "tutorial.board.reply_2",
            "tutorial.board.reply_3",
        };

        /// <summary>고르고 난 뒤. 어느 것을 골랐든 같은 설명으로 이어진다.</summary>
        private static readonly string[] BoardAfterTextIds =
        {
            "tutorial.board.004",
            "tutorial.board.005",
            "tutorial.board.006",
        };

        private static readonly bool[] BoardAfterIsHanyoung = { true, false, true };

        /// <summary>열차에 올라탔다. 사건이 이 자리에 얽힌 이유를 여기서 짚는다.</summary>
        public void OnBoardedTrain()
        {
            if (!_awaitingBoarding) return;

            _awaitingBoarding = false;
            _boardLineIndex = 0;
            _boardPick = -1;
            _boardPickSpoken = false;

            _fieldHud = _caseDirector != null ? _caseDirector.FieldHud : _fieldHud;
            if (_fieldHud == null)
            {
                FinishFieldTutorial();
                return;
            }

            Debug.Log("[TutorialDirector] 열차 안 대사 시작");
            ShowBoardLine();
        }

        private void ShowBoardLine()
        {
            // 먼저 한영이 셋을 말하고, 그 뒤에 고를 것이 뜬다.
            if (_boardLineIndex < BoardLineTextIds.Length)
            {
                string id = BoardLineTextIds[_boardLineIndex];
                _fieldHud.ShowLine(HanyoungNameTextId, () => _loc.Get(id), OnBoardLineAdvanced);
                return;
            }

            // 고른 적이 없으면 여기서 고른다.
            if (_boardPick < 0)
            {
                var labels = new List<System.Func<string>>();
                foreach (var id in BoardChoiceTextIds)
                {
                    string captured = id;
                    labels.Add(() => _loc.Get(captured));
                }

                _fieldHud.ShowChoices(ChajihanNameTextId, labels, OnBoardChoicePicked);
                return;
            }

            // 고른 것을 차지한이 먼저 말한다. 눌러 놓고 아무 말도 없이 대답만 오면 무엇을 골랐는지 잃는다.
            if (!_boardPickSpoken)
            {
                string picked = BoardChoiceTextIds[_boardPick];
                _fieldHud.ShowLine(ChajihanNameTextId, () => _loc.Get(picked), OnBoardPickSpoken);
                return;
            }

            // 고른 것에 대한 대답 하나, 그 뒤로는 어느 것을 골랐든 같은 말이 이어진다.
            int after = _boardLineIndex - BoardLineTextIds.Length - 1;
            if (after < 0)
            {
                string reply = BoardReplyTextIds[_boardPick];
                _fieldHud.ShowLine(HanyoungNameTextId, () => _loc.Get(reply), OnBoardLineAdvanced);
                return;
            }

            if (after < BoardAfterTextIds.Length)
            {
                string id = BoardAfterTextIds[after];
                string name = BoardAfterIsHanyoung[after] ? HanyoungNameTextId : ChajihanNameTextId;
                _fieldHud.ShowLine(name, () => _loc.Get(id), OnBoardLineAdvanced);
                return;
            }

            FinishFieldTutorial();
        }

        /// <summary>고른 말을 마쳤다. 여기서 숫자를 올리지 않는다. 대답이 아직 남아 있다.</summary>
        private void OnBoardPickSpoken()
        {
            _boardPickSpoken = true;
            ShowBoardLine();
        }

        private void OnBoardLineAdvanced()
        {
            _boardLineIndex++;
            ShowBoardLine();
        }

        private void OnBoardChoicePicked(int index)
        {
            // 여기서 숫자를 올리지 않는다. 고른 말과 그 대답이 아직 남아 있다.
            _boardPick = Mathf.Clamp(index, 0, BoardReplyTextIds.Length - 1);
            _boardPickSpoken = false;

            Debug.Log("[TutorialDirector] 열차 안 문제 | 고른 것 " + (_boardPick + 1) +
                      (_boardPick == 1 ? " (정답)" : " (오답)"));
            ShowBoardLine();
        }

        /// <summary>현장 대사가 모두 끝났다. 여기서부터는 평소 조사다.</summary>
        private void FinishFieldTutorial()
        {
            IsRunning = false;

            if (_fieldHud != null) _fieldHud.ClearSpeech();
            if (_caseDirector != null) _caseDirector.RefreshFieldHud();

            Debug.Log("[TutorialDirector] 현장 대사 끝 | 이제부터 평소 조사");
        }

        private UrbanLegendBureau.UI.FieldHudScreen _fieldHud;

        // ------------------------------------------------------------- 종료

        /// <summary>
        /// 튜토리얼을 끝내고 타이틀로 돌아간다.
        ///
        /// 플레이어가 임의로 부를 수 있는 종료 버튼은 없다.
        /// 정해진 흐름(정답 댓글 -> 반응 -> 한영의 마무리)이 끝나야 여기로 온다.
        /// </summary>
        public void FinishTutorial()
        {
            if (!IsRunning) return;

            IsRunning = false;
            _inBriefing = false;

            // 컴퓨터 화면에서 나온다. 화살표를 운영체제에 돌려준다.
            GamePointer.SetVisible(false);
            if (_dialogueScreen != null)
            {
                _dialogueScreen.ClearChoices();
                _dialogueScreen.HideNote();
            }

            // 튜토리얼 전용 저장본은 그냥 버린다. 파일로 쓴 적이 없다.
            _sandbox = null;

            CloseTutorialScreens();
            MarkTutorialSeen();

            if (_caseDirector != null) _caseDirector.ShowTitleScreen();
            Debug.Log("[TutorialDirector] 튜토리얼 종료 | 화면 스택 정리 완료");
        }

        /// <summary>튜토리얼이 쓰던 화면을 전부 닫는다. 스택에 남기지 않는다.</summary>
        private void CloseTutorialScreens()
        {
            if (_ui == null) return;

            if (_ui.Contains(_memoScreen)) _ui.Close(_memoScreen);
            if (_ui.Contains(_talkScreen)) _ui.Close(_talkScreen);
            if (_ui.Contains(_communityScreen)) _ui.Close(_communityScreen);
            if (_ui.Contains(_desktopScreen)) _ui.Close(_desktopScreen);
            if (_ui.Contains(_dialogueScreen)) _ui.Close(_dialogueScreen);
        }

        /// <summary>
        /// 어디에 있든 튜토리얼을 처음부터 다시 돌린다.
        ///
        /// 끝난 것으로 치지 않으므로 저장본의 "봤음" 표시는 건드리지 않는다.
        /// 돌던 연출과 열려 있던 화면은 먼저 정리한다. 그러지 않으면 화면이 겹쳐 쌓인다.
        /// </summary>
        public void RestartTutorial()
        {
            StopAllCoroutines();

            GamePointer.SetVisible(false);
            CloseTutorialScreens();

            IsRunning = false;
            StartTutorial();

            Debug.Log("[TutorialDirector] 튜토리얼을 처음부터 다시 시작했다.");
        }

        /// <summary>
        /// 튜토리얼을 봤다고 실제 저장본에 남긴다.
        /// 기존 storyFlags 를 쓴다. 새 저장 필드도, 버전 상승도 필요 없다.
        /// </summary>
        private void MarkTutorialSeen()
        {
            if (!ServiceRegistry.TryGet<SaveService>(out var save) || save.Current == null) return;
            if (save.Current.storyFlags.Contains(TutorialFlag)) return;

            save.Current.storyFlags.Add(TutorialFlag);
            save.MarkDirty();
        }

        public const string TutorialFlag = "tutorial_seen";

        /// <summary>튜토리얼을 이미 봤는가. 타이틀의 시작 버튼이 물어본다.</summary>
        public static bool HasSeenTutorial()
        {
            if (!ServiceRegistry.TryGet<SaveService>(out var save) || save.Current == null) return false;
            return save.Current.storyFlags.Contains(TutorialFlag);
        }

        /// <summary>테스트와 디버그용. 지금 튜토리얼이 쓰는 저장본을 들여다본다.</summary>
        public SaveData SandboxData => _sandbox != null ? _sandbox.Current : null;
    }
}
