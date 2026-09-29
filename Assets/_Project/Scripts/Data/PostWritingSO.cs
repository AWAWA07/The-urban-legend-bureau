using System;
using System.Collections.Generic;
using UnityEngine;

namespace UrbanLegendBureau.Data
{
    /// <summary>
    /// 게시물 문장 하나가 읽는 사람에게 주는 인상. 플레이어에게는 보이지 않는다.
    /// 판정에 쓰는 것은 BeliefDelta 다. 이 값은 데이터를 짜는 사람이 방향을 맞추라고 붙이는 이름표다.
    /// </summary>
    public enum PostTone
    {
        /// <summary>아무 쪽으로도 기울지 않는다.</summary>
        Neutral = 0,

        /// <summary>덧붙은 거짓을 근거와 함께 걷어낸다.</summary>
        Refutation = 1,

        /// <summary>괴담 말을 쓰지 않고 평소 습관처럼 안심시킨다.</summary>
        Reassurance = 2,

        /// <summary>현상이 실재한다고 인정한다. 사실이어도 믿음은 오른다.</summary>
        Confirmation = 3,

        /// <summary>겁을 준다.</summary>
        FearAmplification = 4,

        /// <summary>근거 없이 단정한다.</summary>
        UnsupportedClaim = 5,
    }

    /// <summary>게시물의 한 칸에 넣어 볼 수 있는 문장 하나.</summary>
    [Serializable]
    public class PostWritingCandidate
    {
        [Tooltip("후보 고유 ID. 로그와 저장에 쓴다.")]
        public string candidateId;

        [Tooltip("실제로 게시물에 들어갈 문장의 String ID.")]
        public string textId;

        [Tooltip("이 문장의 성격. 화면에는 보이지 않는다.")]
        public PostTone tag;

        [Tooltip("이 문장이 괴담 믿음도를 얼마나 움직이는가(%). 음수면 내려간다.")]
        public float beliefDelta;
    }

    /// <summary>게시 뒤에 달리는 댓글 한 줄. 기존 괴담넷 닉네임과 말투를 그대로 쓴다.</summary>
    [Serializable]
    public class PostWritingReaction
    {
        [Tooltip("작성자 표기의 String ID. 예: ui.net.author_nick_5")]
        public string authorTextId;

        [Tooltip("댓글 내용의 String ID.")]
        public string bodyTextId;
    }

    /// <summary>
    /// 사건 하나의 게시물 작성 데이터.
    ///
    /// 보고서가 "무엇이 사실인가"를 적는 곳이라면, 게시물은 "그 사실을 어떻게 말해야 덜 믿게 되는가"를 고르는 곳이다.
    /// 흐름(제목 → 도입 → 사실 → 행동 지침 → 미리보기 → 게시)은 모든 사건이 같다.
    /// 사건마다 달라지는 문장 후보와 수치와 반응만 여기에 둔다. 코드는 사건별로 복제하지 않는다.
    ///
    /// 사건(CaseSO)이 이 에셋을 가리킨다. 가리키지 않는 사건은 글쓰기가 열리지 않고 예전 흐름 그대로다.
    /// </summary>
    [CreateAssetMenu(fileName = "post_", menuName = "Game Data/Post Writing", order = 8)]
    public class PostWritingSO : ScriptableObject
    {
        [Header("식별")]
        [SerializeField] private string _postWritingId;

        [Tooltip("이 글쓰기가 속한 사건의 ID.")]
        [SerializeField] private string _caseId;

        [Tooltip("믿음도를 움직일 괴담의 ID. 이 괴담을 실어 나르는 글들의 믿음 몫이 바뀐다.")]
        [SerializeField] private string _legendId;

        [Tooltip("튜토리얼용인가. 켜면 글쓰기 버튼에 안내 표시가 붙고 한영이 설명한다.")]
        [SerializeField] private bool _isTutorial;

        [Header("후보 문장")]
        [SerializeField] private List<PostWritingCandidate> _titleCandidates = new List<PostWritingCandidate>();
        [SerializeField] private List<PostWritingCandidate> _introCandidates = new List<PostWritingCandidate>();
        [SerializeField] private List<PostWritingCandidate> _factCandidates = new List<PostWritingCandidate>();
        [SerializeField] private List<PostWritingCandidate> _actionCandidates = new List<PostWritingCandidate>();

        [Header("수치")]
        [Tooltip("어떤 문장을 골랐든 글 하나를 올리는 것만으로 더해지는 몫(%).")]
        [SerializeField] private float _postModifier;

        [Tooltip("게시 뒤 이 괴담의 믿음도가 이 값(%) 이하가 되면 성공이다.")]
        [SerializeField] private int _tutorialTargetBelief = 15;

        [Tooltip("목표에 못 닿아도 이만큼(%p) 이상 끌어내렸으면 성공으로 친다. 0 이면 목표만 본다.\n" +
                 "시작 믿음도는 조사를 얼마나 했는지에 따라 달라진다. 목표만 보면 잘 쓴 글도 떨어질 수 있다.")]
        [SerializeField] private int _requiredDrop = 7;

        [Header("다시 쓰기")]
        [Tooltip("실패했을 때 다시 쓸 수 있는가. 실전에서는 끈다.")]
        [SerializeField] private bool _allowRewrite = true;

        [Tooltip("올릴 수 있는 최대 횟수. 0이면 제한이 없다.")]
        [SerializeField] private int _maxAttempts;

        [Tooltip("다시 쓸 때 실패한 글을 내리고 그 글이 올린 믿음도를 되돌린다. 튜토리얼에서만 켠다.")]
        [SerializeField] private bool _takeDownFailedPost = true;

        [Header("게시되는 모습")]
        [Tooltip("작성자(차지한의 부계정) 표기의 String ID.")]
        [SerializeField] private string _authorTextId;

        [Tooltip("글이 걸리는 게시판 이름의 String ID.")]
        [SerializeField] private string _boardTextId;

        [SerializeField] private int _views = 37;

        [Header("게시 뒤 반응 - 성공")]
        [SerializeField] private int _successLikes = 14;
        [SerializeField] private int _successDislikes = 2;
        [SerializeField] private List<PostWritingReaction> _successReactions = new List<PostWritingReaction>();

        [Header("게시 뒤 반응 - 실패(믿음이 올랐을 때)")]
        [SerializeField] private int _failLikes = 9;
        [SerializeField] private int _failDislikes = 3;
        [SerializeField] private List<PostWritingReaction> _failReactions = new List<PostWritingReaction>();

        [Header("게시 뒤 반응 - 모자람(내려갔지만 목표에 못 미쳤을 때)")]
        [SerializeField] private int _weakLikes = 5;
        [SerializeField] private int _weakDislikes = 4;
        [SerializeField] private List<PostWritingReaction> _weakReactions = new List<PostWritingReaction>();

        [Header("한영의 말 (String ID)")]
        [SerializeField] private string _introLineTextId;
        [SerializeField] private string _successLineTextId;
        [SerializeField] private string _failLineTextId;
        [SerializeField] private string _weakLineTextId;
        [SerializeField] private string _takeDownLineTextId;

        public string PostWritingId => _postWritingId;
        public string CaseId => _caseId;
        public string LegendId => _legendId;
        public bool IsTutorial => _isTutorial;

        public IReadOnlyList<PostWritingCandidate> TitleCandidates => _titleCandidates;
        public IReadOnlyList<PostWritingCandidate> IntroCandidates => _introCandidates;
        public IReadOnlyList<PostWritingCandidate> FactCandidates => _factCandidates;
        public IReadOnlyList<PostWritingCandidate> ActionCandidates => _actionCandidates;

        public float PostModifier => _postModifier;
        public int TutorialTargetBelief => _tutorialTargetBelief;
        public int RequiredDrop => _requiredDrop;
        public bool AllowRewrite => _allowRewrite;
        public int MaxAttempts => _maxAttempts;
        public bool TakeDownFailedPost => _takeDownFailedPost;

        public string AuthorTextId => _authorTextId;
        public string BoardTextId => _boardTextId;
        public int Views => _views;

        public int SuccessLikes => _successLikes;
        public int SuccessDislikes => _successDislikes;
        public IReadOnlyList<PostWritingReaction> SuccessReactions => _successReactions;
        public int FailLikes => _failLikes;
        public int FailDislikes => _failDislikes;
        public IReadOnlyList<PostWritingReaction> FailReactions => _failReactions;
        public int WeakLikes => _weakLikes;
        public int WeakDislikes => _weakDislikes;
        public IReadOnlyList<PostWritingReaction> WeakReactions => _weakReactions;

        public string IntroLineTextId => _introLineTextId;
        public string SuccessLineTextId => _successLineTextId;
        public string FailLineTextId => _failLineTextId;
        public string WeakLineTextId => _weakLineTextId;
        public string TakeDownLineTextId => _takeDownLineTextId;

        /// <summary>칸 순서대로 후보 목록. 0 제목, 1 도입, 2 사실, 3 행동 지침.</summary>
        public IReadOnlyList<PostWritingCandidate> GetCandidates(int section)
        {
            switch (section)
            {
                case 0: return _titleCandidates;
                case 1: return _introCandidates;
                case 2: return _factCandidates;
                case 3: return _actionCandidates;
                default: return null;
            }
        }

        /// <summary>칸의 수. 제목 하나와 본문 셋이다.</summary>
        public const int SectionCount = 4;
    }
}
