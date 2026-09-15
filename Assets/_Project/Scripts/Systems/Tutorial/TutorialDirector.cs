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
            "tutorial.line.004",
            "tutorial.line.005",
        };

        private static readonly bool[] LineIsHanyoung = { true, true, true, false, true };
        private static readonly float[] LineBrightness = { 1f, 0.55f, 1f, 1f, 1f };

        /// <summary>
        /// 한영이 화면에 있는가.
        /// 첫 대사("...")는 어둠 속에서 목소리만 들린다. 모습은 다음 대사부터 드러난다.
        /// </summary>
        private static readonly bool[] LineHanyoungVisible = { false, true, true, true, true };

        /// <summary>
        /// 차지한이 화면에 있는가.
        /// 처음 세 대사는 한영만 나온다. 차지한은 자기 첫 대사에서 처음 모습을 드러낸다.
        /// </summary>
        private static readonly bool[] LineChajihanVisible = { false, false, false, true, true };

        // --- 커뮤니티 ---
        private const string TutorialPostViews = "1284";
        private const string PostTimeTextId = "ui.net.post_time_tutorial";
        private const string ChoiceHintTextId = "tutorial.comment.hint";
        private const string WrongTextId = "tutorial.comment.wrong";
        private const string NeedFieldTextId = "tutorial.comment.need_field";
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

            OpenCommunity();
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
            Finish
        }

        private AfterTalk _afterTalk;

        private void OpenCommunity()
        {
            if (_communityScreen == null)
            {
                Debug.LogError("[TutorialDirector] 커뮤니티 화면이 연결되지 않았다.");
                return;
            }

            _communityScreen.BindPage(_tutorialPage, int.Parse(TutorialPostViews), PostTimeTextId);
            _communityScreen.BindComments(_comments);
            _communityScreen.BindChoices(_choices, BuildChoiceLabel, OnChoiceSelected);
            _communityScreen.ShowNotice(null);

            // 대화 화면을 확실히 닫는다. 위에 팝업이 떠 있어도 스택에 남지 않게 한다.
            if (_ui.Contains(_dialogueScreen)) _ui.Close(_dialogueScreen);

            if (_ui.Count == 0) _ui.Push(_communityScreen);
            else _ui.Replace(_communityScreen);

            Debug.Log("[TutorialDirector] 커뮤니티 화면 | 댓글 선택 " + _choices.Count + "개");

            // 설명은 일반 문구가 아니라 한영이 직접 말한다.
            ShowTalk(ChoiceHintTextId, AfterTalk.BackToChoices);
        }

        // ------------------------------------------------------------- 한영의 말

        /// <summary>
        /// 커뮤니티 화면 위에 한영을 띄우고 한 줄 말하게 한다.
        ///
        /// 대화 화면을 새로 만들지 않고 같은 DialogueScreen을 쓴다.
        /// 이 화면은 아래를 가리지 않는 팝업이라 커뮤니티가 그대로 보이고,
        /// 화면 전체를 덮는 진행 버튼이 있어 말하는 동안에는 선택지를 누를 수 없다.
        /// </summary>
        private void ShowTalk(string lineTextId, AfterTalk after)
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

            _talkScreen.SetSoloLayout(false);   // 겹침 대화는 정해진 자리를 그대로 쓴다
            _talkScreen.ShowLine(true,
                () => _loc.Get(HanyoungNameTextId),
                () => _loc.Get(lineTextId),
                1f,
                leftVisible: true,
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

            _talkScreen.ShowNarration(() => _loc.Get(lineTextId), leftVisible: true, rightVisible: false);

            Debug.Log("[TutorialDirector] 나레이션 | " + lineTextId + " -> 끝나면 " + after);
        }

        /// <summary>한영의 말을 넘겼을 때. 말이 끝나야 다음 상태로 간다.</summary>
        private void OnTalkAdvanced()
        {
            if (_ui.Contains(_talkScreen)) _ui.Close(_talkScreen);

            switch (_afterTalk)
            {
                case AfterTalk.PostComment:
                    PostCorrectComment();
                    break;

                case AfterTalk.Finish:
                    FinishTutorial();
                    break;

                default:
                    // 다시 고를 수 있는 상태로 돌아간다. 선택지는 그대로 남아 있다.
                    break;
            }
        }

        private void BuildComments()
        {
            _comments = new List<CommunityComment>
            {
                new CommunityComment { AuthorTextId = "ui.net.author_anon", BodyTextId = "tutorial.comment.existing_1" },
                new CommunityComment { AuthorTextId = "ui.net.author_anon", BodyTextId = "tutorial.comment.existing_2" },
                new CommunityComment { AuthorTextId = "ui.net.author_anon", BodyTextId = "tutorial.comment.existing_3" },
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

        /// <summary>선택지 문구. 조건이 모자란 것은 표시를 붙인다.</summary>
        private string BuildChoiceLabel(TutorialCommentChoice choice)
        {
            if (choice == null) return string.Empty;

            string text = _loc.Get(choice.SummaryTextId);
            if (!CanUseChoice(choice)) text += "  [" + _loc.Get("ui.action.locked") + "]";

            return text + "\n" + _loc.Get(choice.BodyTextId);
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
                // 화면에는 이유를 적어 두고, 설명은 한영이 직접 한다.
                _communityScreen.ShowNotice(() => _loc.Get(NeedFieldTextId));
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

            AcceptCorrectComment(choice);
            _pendingChoice = null;

            // 마무리는 한영의 말이 아니라 상황 설명이다. 나레이션으로 띄운다.
            ShowNarration(DoneTextId, AfterTalk.Finish);
        }

        private void AcceptCorrectComment(TutorialCommentChoice choice)
        {
            if (_censored) return;    // 믿음도가 두 번 깎이지 않게 한다
            _censored = true;

            float before = _belief != null ? _belief.GetBeliefLevel(_sandbox.Current) : 0f;

            // 검열은 기존 시스템이 한다. 플레이어가 고른 결과로 실제 검열이 일어난다.
            var censorResult = CensorResult.NotFound;
            if (_internet != null && _tutorialPage != null)
            {
                censorResult = _internet.TryCensorPage(_sandbox, _tutorialPage.PageId);
            }

            // 플레이어의 댓글이 실제로 달린다.
            _comments.Add(new CommunityComment
            {
                AuthorTextId = PlayerNameTextId,
                BodyTextId = choice.BodyTextId,
                IsPlayer = true,
            });

            // 사람들이 반응한다. 이 반응이 믿음을 깎는 이유다.
            _comments.Add(new CommunityComment { AuthorTextId = "ui.net.author_anon", BodyTextId = "tutorial.reaction.1" });
            _comments.Add(new CommunityComment { AuthorTextId = "ui.net.author_anon", BodyTextId = "tutorial.reaction.2" });
            _comments.Add(new CommunityComment { AuthorTextId = "ui.net.author_anon", BodyTextId = "tutorial.reaction.3" });

            if (_belief != null) _belief.TryReduceBelief(_sandbox.Current, TutorialBeliefDrop);
            float after = _belief != null ? _belief.GetBeliefLevel(_sandbox.Current) : 0f;

            _communityScreen.BindComments(_comments);
            _communityScreen.BindChoices(null, null, null);      // 고를 것이 없어진다

            _finished = true;

            Debug.Log($"[TutorialDirector] 정답 댓글 | 검열={censorResult} | " +
                      $"믿음 {before:F0} -> {after:F0} (튜토리얼 전용 저장본)");
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

            // 튜토리얼 전용 저장본은 그냥 버린다. 파일로 쓴 적이 없다.
            _sandbox = null;

            if (_ui.Contains(_talkScreen)) _ui.Close(_talkScreen);
            if (_ui.Contains(_communityScreen)) _ui.Close(_communityScreen);
            if (_ui.Contains(_dialogueScreen)) _ui.Close(_dialogueScreen);

            MarkTutorialSeen();

            if (_caseDirector != null) _caseDirector.ShowTitleScreen();
            Debug.Log("[TutorialDirector] 튜토리얼 종료 | 화면 스택 정리 완료");
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
