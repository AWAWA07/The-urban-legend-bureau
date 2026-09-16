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

            IsRunning = true;
            _finished = false;
            _censored = false;
            _lineIndex = 0;
            _postBelief = TutorialPostBelief;

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

            if (_ui.Contains(_dialogueScreen)) _ui.Close(_dialogueScreen);

            if (_ui.Count == 0) _ui.Push(_desktopScreen);
            else _ui.Replace(_desktopScreen);

            Debug.Log("[TutorialDirector] 컴퓨터 화면");

            // 여기서는 한영이 옆에서 안내만 한다. 인물은 세우지 않는다.
            ShowTalk(PcLine1TextId, AfterTalk.OpenDesktopNet, showCharacter: false);
        }

        /// <summary>바탕화면 아이콘을 눌렀을 때. 지금 열 수 있는 것은 괴담넷뿐이다.</summary>
        private void OnAppClicked(string appId)
        {
            if (appId != NetAppId) return;
            OpenCommunityBoard();
        }

        /// <summary>괴담넷을 열면 게시판 목록부터 보인다.</summary>
        private void OpenCommunityBoard()
        {
            if (_communityScreen == null) return;

            _communityScreen.BindWindow(null);        // 튜토리얼 중에는 창을 닫을 수 없다
            _communityScreen.BindBoard(_boardEntries, OnBoardEntryClicked);
            _communityScreen.ShowBoard(true);
            _communityScreen.ShowNotice(null);

            // 바탕화면 위에 얹는다. 바꿔 끼우지 않아야 아래에서 작업 표시줄이 계속 보인다.
            if (!_ui.Contains(_communityScreen)) _ui.Push(_communityScreen);

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
        /// 게시판 목록. 인기글 하나만 열리고 나머지는 자리를 채운다.
        /// 튜토리얼이 엉뚱한 글로 새지 않게 하기 위해서다.
        /// </summary>
        private void BuildBoardEntries()
        {
            // 인기글이 시간과 상관없이 맨 위에 붙고, 나머지는 새로 올라온 것부터 내려간다.
            // 실제 게시판이 그렇게 늘어놓는다.
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
                },
                new CommunityBoardEntry { TitleTextId = "board.filler.009", MetaTextId = "board.filler.009.meta", IsHot = true, BeliefPercent = 22 },

                // 여기부터 최신순. 괴담과 상관없는 글은 믿음에 보태는 것이 없어 0이다.
                new CommunityBoardEntry { TitleTextId = "board.filler.006", MetaTextId = "board.filler.006.meta", BeliefPercent = 28 },  // 12분 전
                new CommunityBoardEntry { TitleTextId = "board.filler.007", MetaTextId = "board.filler.007.meta", BeliefPercent = 19 },  // 34분 전
                new CommunityBoardEntry { TitleTextId = "board.filler.008", MetaTextId = "board.filler.008.meta" },                      // 1시간 전
                new CommunityBoardEntry { TitleTextId = "board.filler.004", MetaTextId = "board.filler.004.meta", BeliefPercent = 25 },  // 2시간 전
                new CommunityBoardEntry { TitleTextId = "board.filler.005", MetaTextId = "board.filler.005.meta" },                      // 4시간 전
                new CommunityBoardEntry { TitleTextId = "board.filler.001", MetaTextId = "board.filler.001.meta" },                      // 6시간 전
                new CommunityBoardEntry { TitleTextId = "board.filler.002", MetaTextId = "board.filler.002.meta" },                      // 9시간 전
                new CommunityBoardEntry { TitleTextId = "board.filler.003", MetaTextId = "board.filler.003.meta" },                      // 어제 23:50
            };
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

            _communityScreen.BindPage(_tutorialPage, int.Parse(TutorialPostViews), PostTimeTextId,
                TutorialPostLikes, TutorialPostDislikes, _postBelief);
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
            if (!pressed) return;
            ShowTalk(LikeWarnTextId, AfterTalk.BackToChoices);
        }

        /// <summary>싫어요를 눌렀을 때. 한영이 맞장구를 친다.</summary>
        private void OnDislikeToggled(bool pressed)
        {
            if (!pressed) return;
            ShowTalk(DislikeGoodTextId, AfterTalk.BackToChoices);
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
                    FinishTutorial();
                    break;

                case AfterTalk.OpenDesktopNet:
                    // 안내가 끝나야 아이콘을 누를 수 있다. 그것도 괴담넷 하나만.
                    if (_desktopScreen != null) _desktopScreen.SetAllowedApps(NetAppId);
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
        /// 게시판에 올라온 글 전체를 기준으로 삼는다.
        /// 글 하나하나가 이 괴담을 얼마나 믿게 만들고 있는지를 더해 글 수로 나눈다.
        /// 괴담과 무관한 글은 0으로 들어가 전체를 끌어내린다. 그것도 실제 몫이다.
        /// 그래서 괴담 글에 댓글을 달아 몫을 깎으면 이 숫자가 따라 내려간다.
        /// </summary>
        private void PushBeliefToTaskbar()
        {
            if (_desktopScreen == null) return;
            _desktopScreen.SetBelief(CalculateBoardBelief());
        }

        /// <summary>게시글 전체를 기준으로 낸 믿음도(%).</summary>
        private int CalculateBoardBelief()
        {
            if (_boardEntries == null || _boardEntries.Count == 0) return 0;

            int total = 0;
            foreach (var entry in _boardEntries)
            {
                if (entry != null) total += entry.BeliefPercent;
            }

            return Mathf.RoundToInt((float)total / _boardEntries.Count);
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

            // 컴퓨터 화면에서 나온다. 화살표를 운영체제에 돌려준다.
            GamePointer.SetVisible(false);

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
