using System;
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
    /// 괴담넷에 글을 한 편 써서 올리는 흐름.
    ///
    /// 글을 쓰는 동안에는 괴담넷 창 안에 글쓰기 화면(PostComposeView)을 덮는다.
    /// 제목 → 도입 → 사실 전달 → 행동 안내를 한 칸씩 고르고, 이전 / 다음 단계로 오갈 수 있다.
    /// 고르는 동안에는 믿음이 어느 쪽으로 움직이는지 알려 주지 않는다. 올린 뒤에 한꺼번에 평가한다.
    /// 다 고르면 게시판에 걸릴 모습 그대로 미리 보고, 올리면 실제 게시판에 걸린다.
    ///
    /// 믿음도는 기존 게시판 몫(TutorialDirector)과 기존 BeliefService 로 움직인다. 새 믿음 계산은 없다.
    /// 문장과 수치와 반응은 전부 PostWritingSO 에 있다. 사건마다 이 흐름을 복제하지 않는다.
    /// </summary>
    public class PostWritingDirector : MonoBehaviour
    {
        [SerializeField] private CommunityPageScreen _communityScreen;

        [Tooltip("게시판 목록과 믿음 몫, 한영의 대화창을 들고 있는 쪽.")]
        [SerializeField] private TutorialDirector _tutorial;

        private LocalizationService _loc;
        private SaveService _save;
        private BeliefService _belief;

        private PostWritingSO _data;
        private PostComposeView _compose;

        /// <summary>지금 고르고 있는 칸. 0 제목, 1 도입, 2 사실, 3 행동 안내, 4 미리보기.</summary>
        private int _section;

        /// <summary>칸마다 고른 후보. 다시 쓰거나 이전 단계로 가도 지우지 않는다.</summary>
        private readonly int[] _picks = { -1, -1, -1, -1 };

        /// <summary>이번에 올린 횟수. 사건을 새로 열면 처음부터 센다.</summary>
        private int _attempts;

        /// <summary>글쓰기 전 한영의 말을 들었는가. 창을 닫았다 다시 열어도 되풀이하지 않는다.</summary>
        private bool _introTalked;

        /// <summary>마지막으로 올린 글과 그 글이 옮긴 믿음 몫. 다시 쓸 때 되돌리는 데 쓴다.</summary>
        private CommunityBoardEntry _lastPost;
        private int _lastShift;

        /// <summary>올리는 중이거나 반응이 달리는 중인가. 그 사이에는 무엇도 새로 시작하지 않는다.</summary>
        private bool _busy;

        /// <summary>지금 글쓰기 화면이 무엇을 보여주는가. 언어가 바뀌면 같은 것을 다시 그린다.</summary>
        private enum View { None, Step, Preview, Result }
        private View _view;
        private Func<PostComposeView.Result> _resultBuilder;

        private const string PublishedFlagPrefix = "post_published:";

        /// <summary>이 글쓰기를 마쳤다는 이야기 표시. 사건을 처음부터 다시 할 때 지우는 쪽도 이것을 쓴다.</summary>
        public static string PublishedFlag(PostWritingSO data) => PublishedFlagPrefix + (data != null ? data.PostWritingId : string.Empty);

        private const string TitlePlaceholderTextId = "ui.post.title_placeholder";
        private static readonly string[] BodyPlaceholderTextIds =
        {
            null,
            "ui.post.placeholder_intro",
            "ui.post.placeholder_fact",
            "ui.post.placeholder_action",
        };

        private static readonly string[] SectionNameTextIds =
        {
            "ui.post.section_title",
            "ui.post.section_intro",
            "ui.post.section_fact",
            "ui.post.section_action",
        };

        private static readonly string[] PromptTextIds =
        {
            "ui.post.prompt_title",
            "ui.post.prompt_intro",
            "ui.post.prompt_fact",
            "ui.post.prompt_action",
        };

        private const string ComposeHeaderTextId = "ui.post.compose_header";
        private const string ComposeAccountTextId = "ui.post.compose_account";
        private const string GoalTextId = "ui.post.goal";
        private const string DraftLabelTextId = "ui.post.draft_label";
        private const string PrevTextId = "ui.post.btn_prev";
        private const string NextTextId = "ui.post.btn_next";
        private const string ToPreviewTextId = "ui.post.btn_to_preview";

        private const string PreviewHeaderTextId = "ui.post.header_preview";
        private const string PreviewSubTextId = "ui.post.preview_sub";
        private const string PreviewBoardTextId = "ui.post.preview_board";
        private const string PreviewMetaTextId = "ui.post.preview_meta";
        private const string PreviewNoteTextId = "ui.post.preview_note";
        private const string PublishTextId = "ui.post.btn_publish";
        private const string RewriteTextId = "ui.post.btn_rewrite";

        private const string ResultHeaderTextId = "ui.post.result_header";
        private const string ResultLegendTextId = "ui.post.result_legend";
        private const string ResultGoalTextId = "ui.post.result_goal";
        private const string ResultOverallTextId = "ui.post.result_overall";
        private const string EffectDownTextId = "ui.post.effect_down";
        private const string EffectUpTextId = "ui.post.effect_up";
        private const string EffectFlatTextId = "ui.post.effect_flat";
        private const string ConfirmTextId = "ui.post.btn_result_ok";

        private const string ResultDownTextId = "ui.post.result_down";
        private const string ResultUpTextId = "ui.post.result_up";
        private const string ResultFlatTextId = "ui.post.result_flat";
        private const string TakenDownTextId = "ui.post.taken_down";

        /// <summary>댓글이 하나씩 달리는 간격(초). 댓글 연습 때와 같은 빠르기다.</summary>
        private const float ReactionInterval = 0.8f;

        /// <summary>글쓰기를 성공으로 마쳤을 때. 사건 쪽이 이것을 받아 다음 흐름(첫날 밤)으로 넘어간다.</summary>
        public event Action<PostWritingSO> Succeeded;

        private void Start()
        {
            ServiceRegistry.TryGet(out _loc);
            ServiceRegistry.TryGet(out _save);
            ServiceRegistry.TryGet(out _belief);

            // 올린 글은 저장본에 따로 적힌다. 게시판을 새로 지으면 되살리고, 저장하기 직전에 숫자를 옮겨 적는다.
            if (_tutorial != null) _tutorial.BoardRebuilt += RestoreSavedPosts;
            if (_save != null) _save.BeforeSave += SyncSavedPosts;
        }

        private void OnDestroy()
        {
            if (_tutorial != null) _tutorial.BoardRebuilt -= RestoreSavedPosts;
            if (_save != null) _save.BeforeSave -= SyncSavedPosts;
        }

        private void RestoreSavedPosts()
        {
            if (_save == null || _save.Current == null || _tutorial == null) return;
            PlayerPostStore.Restore(_save.Current, _tutorial);
        }

        private void SyncSavedPosts()
        {
            if (_save == null || _save.Current == null || _tutorial == null) return;
            PlayerPostStore.Sync(_save.Current, _tutorial.BoardEntries);
        }

        private void OnEnable() => EventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);
        private void OnDisable() => EventBus.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);

        private void OnLanguageChanged(LanguageChangedEvent evt)
        {
            if (_compose == null || !_compose.IsShowing || _data == null) return;
            switch (_view)
            {
                case View.Step: ShowStep(); break;
                case View.Preview: ShowPreview(); break;
                case View.Result: if (_resultBuilder != null) _compose.ShowResult(_resultBuilder()); break;
            }
        }

        // ------------------------------------------------------------- 밖에서 묻는 것

        /// <summary>이 글쓰기를 이미 성공으로 마쳤는가. 저장본의 이야기 표시로 안다.</summary>
        public bool IsFinished(PostWritingSO data)
        {
            if (data == null || _save == null || _save.Current == null) return false;
            return _save.Current.storyFlags.Contains(PublishedFlagPrefix + data.PostWritingId);
        }

        /// <summary>지금 글쓰기 단추를 세워도 되는가.</summary>
        public bool CanWrite(PostWritingSO data)
        {
            if (data == null || IsFinished(data)) return false;
            if (_attempts > 0 && !data.AllowRewrite) return false;
            if (data.MaxAttempts > 0 && _attempts >= data.MaxAttempts) return false;
            return true;
        }

        /// <summary>사건을 새로 열 때 부른다. 지난번 사건의 횟수와 올린 글 기록과 고른 문장을 버린다.</summary>
        public void ResetProgress()
        {
            StopAllCoroutines();
            _data = null;
            _attempts = 0;
            _lastPost = null;
            _lastShift = 0;
            _busy = false;
            _introTalked = false;
            _takenDownNotice = null;
            _resultBuilder = null;
            ClearPicks();
            HideCompose();
        }

        private void ClearPicks()
        {
            _section = 0;
            for (int i = 0; i < _picks.Length; i++) _picks[i] = -1;
        }

        private void HideCompose()
        {
            _view = View.None;
            if (_compose != null) _compose.Hide();
        }

        // ------------------------------------------------------------- 쓰기

        /// <summary>
        /// 글쓰기 단추를 눌렀을 때. 글쓰기 화면을 먼저 펴고, 튜토리얼이면 한영이 그 위에서 한마디 한다.
        /// 보고서를 그대로 올리면 안 된다는 것, 읽는 사람이 안심하게 써야 한다는 것을 이른다.
        /// 창을 닫았다 다시 열면 고르던 칸에서 이어 쓴다.
        /// </summary>
        public void Begin(PostWritingSO data)
        {
            if (data == null || _communityScreen == null || _busy || !CanWrite(data)) return;

            if (_data != data) ClearPicks();
            _data = data;

            _compose = PostComposeView.Ensure(_communityScreen.Window, _communityScreen.BodyFont, _communityScreen.TitleFont);
            if (_compose == null) return;

            Debug.Log($"[PostWriting] 글쓰기 시작 | {data.PostWritingId} | {_attempts + 1}번째 | {_section + 1}칸부터");

            if (_section >= PostWritingSO.SectionCount) ShowPreview();
            else ShowStep();

            if (data.IsTutorial && !_introTalked && _tutorial != null)
            {
                _introTalked = _tutorial.ShowComputerTalk(data.IntroLineTextId, null);
            }
        }

        private string L(string id) => _loc != null ? _loc.Get(id) : id;
        private string L(string id, params object[] args) => _loc != null ? _loc.Get(id, args) : id;

        /// <summary>이 괴담의 지금 믿음도와 성공 / 우수 기준(%). 올리기 전 화면과 올린 뒤 평가가 같은 값을 쓴다.</summary>
        private void Lines(int before, out int success, out int excellent)
        {
            success = Mathf.Max(before - _data.RequiredDrop, _data.TutorialTargetBelief);
            excellent = before - _data.ExcellentDrop;
            if (excellent > success) excellent = success;
        }

        /// <summary>
        /// 한 칸을 고르는 화면. 위에는 쓰고 있는 글, 가운데에 후보 셋, 아래에 이전 / 다음.
        /// 고른 문장은 곧바로 위의 글에 들어가 보인다. 믿음이 어느 쪽으로 가는지는 보이지 않는다.
        /// </summary>
        private void ShowStep()
        {
            _view = View.Step;
            int section = Mathf.Clamp(_section, 0, PostWritingSO.SectionCount - 1);

            int before = _tutorial != null ? _tutorial.GetLegendBelief(_data.LegendId) : 0;
            Lines(before, out int success, out int excellent);

            var names = new string[PostWritingSO.SectionCount];
            for (int i = 0; i < names.Length; i++) names[i] = L(SectionNameTextIds[i]);

            var body = new List<string>();
            int current = -1;
            for (int s = 1; s < PostWritingSO.SectionCount; s++)
            {
                if (_picks[s] >= 0)
                {
                    if (s == section) current = body.Count;
                    body.Add(L(Picked(s).textId));
                }
                else if (s == section)
                {
                    current = body.Count;
                    body.Add(L(BodyPlaceholderTextIds[s]));
                }
            }

            var candidates = new List<string>();
            var list = _data.GetCandidates(section);
            for (int i = 0; list != null && i < list.Count; i++) candidates.Add(list[i] != null ? L(list[i].textId) : string.Empty);

            _compose.ShowStep(new PostComposeView.Step
            {
                Header = L(ComposeHeaderTextId, section + 1, PostWritingSO.SectionCount, names[section]),
                Notice = _takenDownNotice != null ? _takenDownNotice() : null,
                Account = L(ComposeAccountTextId, L(_data.BoardTextId), L(_data.AuthorTextId)),
                Goal = before > 0 ? L(GoalTextId, before, success, excellent) : null,
                StepNames = names,
                StepIndex = section,
                DraftLabel = L(DraftLabelTextId),
                DraftTitle = _picks[0] >= 0 ? L(Picked(0).textId) : L(TitlePlaceholderTextId),
                DraftTitleIsCurrent = section == 0,
                DraftBody = body,
                CurrentBodyIndex = current,
                Prompt = L(PromptTextIds[section]),
                Candidates = candidates,
                Selected = _picks[section],
                PrevLabel = L(PrevTextId),
                NextLabel = L(section == PostWritingSO.SectionCount - 1 ? ToPreviewTextId : NextTextId),
                CanPrev = section > 0,
                OnSelect = OnCandidatePicked,
                OnPrev = OnPrev,
                OnNext = OnNext,
            });
        }

        private PostWritingCandidate Picked(int section)
        {
            var list = _data.GetCandidates(section);
            int i = _picks[section];
            return list != null && i >= 0 && i < list.Count ? list[i] : null;
        }

        /// <summary>후보 하나를 골랐다. 위의 글에만 들어간다. 다음 단계로 가는 것은 플레이어가 정한다.</summary>
        private void OnCandidatePicked(int index)
        {
            if (_busy || _data == null || _section >= PostWritingSO.SectionCount) return;
            var list = _data.GetCandidates(_section);
            if (list == null || index < 0 || index >= list.Count) return;

            _picks[_section] = index;
            Debug.Log($"[PostWriting] {_section}칸 | {list[index].candidateId}");
            ShowStep();
        }

        private void OnPrev()
        {
            if (_busy || _section <= 0) return;
            _section--;
            ShowStep();
        }

        private void OnNext()
        {
            if (_busy || _section >= PostWritingSO.SectionCount || _picks[_section] < 0) return;
            _takenDownNotice = null;
            _section++;
            if (_section >= PostWritingSO.SectionCount) ShowPreview();
            else ShowStep();
        }

        /// <summary>게시판에 걸릴 모습 그대로. 다시 쓰기는 고른 것을 그대로 둔 채 첫 칸으로 돌아간다.</summary>
        private void ShowPreview()
        {
            _view = View.Preview;
            _section = PostWritingSO.SectionCount;

            var body = new List<string>();
            for (int s = 1; s < PostWritingSO.SectionCount; s++) body.Add(L(Picked(s).textId));

            _compose.ShowPreview(new PostComposeView.Preview
            {
                Header = L(PreviewHeaderTextId),
                Sub = L(PreviewSubTextId),
                Board = L(PreviewBoardTextId, L(_data.BoardTextId)),
                Title = L(Picked(0).textId),
                Meta = L(PreviewMetaTextId, L(_data.AuthorTextId)),
                Body = body,
                Note = L(PreviewNoteTextId, L(_data.AuthorTextId)),
                RewriteLabel = L(RewriteTextId),
                PublishLabel = L(PublishTextId),
                OnRewrite = () =>
                {
                    if (_busy) return;
                    _section = 0;
                    ShowStep();
                },
                OnPublish = Publish,
            });
        }

        // ------------------------------------------------------------- 올리기

        /// <summary>고른 문장들과 글 하나의 몫을 더한 값. 이 괴담을 실어 나르는 글들이 이만큼 움직인다.</summary>
        private int SumDelta()
        {
            float total = _data.PostModifier;
            for (int s = 0; s < PostWritingSO.SectionCount; s++)
            {
                var c = Picked(s);
                if (c != null) total += c.beliefDelta;
            }
            return Mathf.RoundToInt(total);
        }

        private enum Outcome { Excellent, Success, Weak, Flat, Fail }

        /// <summary>
        /// 판정. 올린 뒤 믿음도가 얼마나 내려갔는가로 가른다.
        /// 우수와 성공은 덧붙은 거짓을 걷어낸 문장(사실 칸의 반박)을 골랐을 때만 나온다.
        /// 사실이라도 "진짜 있다"만 강조하면 사람들은 더 믿는다. 거짓을 걷어내야 믿음이 내려간다.
        /// </summary>
        private Outcome Judge(int shift, int before, int after)
        {
            if (shift > 0 || after > before) return Outcome.Fail;
            if (after == before) return Outcome.Flat;

            int refuteSection = _data.RefuteSection;
            bool refuted = refuteSection < 0 || (Picked(refuteSection) != null && Picked(refuteSection).tag == PostTone.Refutation);
            if (!refuted) return Outcome.Weak;

            Lines(before, out int success, out int excellent);
            if (after <= excellent) return Outcome.Excellent;
            if (after <= success) return Outcome.Success;
            return Outcome.Weak;
        }

        /// <summary>글을 실제로 게시판에 건다. 반응이 달리고 믿음도가 움직인 뒤 평가가 뜬다.</summary>
        private void Publish()
        {
            if (_tutorial == null || _busy || _data == null) return;
            for (int s = 0; s < PostWritingSO.SectionCount; s++) if (Picked(s) == null) return;
            _busy = true;

            int shift = SumDelta();
            int before = _tutorial.GetLegendBelief(_data.LegendId);
            int overallBefore = _tutorial.BoardBelief;
            int expected = Mathf.Clamp(before + shift, 0, 100);
            var outcome = Judge(shift, before, expected);
            bool good = outcome == Outcome.Excellent || outcome == Outcome.Success;

            var body = new List<string>();
            for (int s = 1; s < PostWritingSO.SectionCount; s++) body.Add(Picked(s).textId);

            var entry = new CommunityBoardEntry
            {
                TitleTextId = Picked(0).textId,
                BodyTextIds = body,
                AuthorTextId = _data.AuthorTextId,
                BoardTextId = _data.BoardTextId,
                Views = _data.Views,
                PostedMinutesAgo = -GameClock.ElapsedMinutes,
                Comments = new List<CommunityComment>(),
                Likes = good ? _data.SuccessLikes : outcome == Outcome.Fail ? _data.FailLikes : _data.WeakLikes,
                Dislikes = good ? _data.SuccessDislikes : outcome == Outcome.Fail ? _data.FailDislikes : _data.WeakDislikes,
                IsPlayerPost = true,
                PostId = _data.PostWritingId + "_" + Guid.NewGuid().ToString("N").Substring(0, 8),
            };

            _tutorial.AddBoardPost(entry);
            _attempts++;
            _lastPost = entry;

            // 저장본에도 적는다. 게시판 목록은 저장하지 않으므로 다시 불러오면 이 기록으로 되살린다.
            if (_save != null && _save.Current != null)
            {
                PlayerPostStore.Record(_save.Current, entry, _data.CaseId, _data.PostWritingId, _data.LegendId);
                _save.MarkDirty();
            }

            // 글쓰기 화면을 걷고 올린 글을 연다. 게시판에 걸린 그 글이다. 반응은 이제부터 보인다.
            HideCompose();
            _tutorial.OpenFillerPost(entry);
            _communityScreen.SetControlsEnabled(false);

            Debug.Log($"[PostWriting] 게시 | {_data.PostWritingId} {_attempts}번째 | 몫 {shift:+0;-0;0}% | 결과 {outcome}");
            StartCoroutine(PlayReactions(entry, shift, before, overallBefore, outcome));
        }

        private IEnumerator PlayReactions(CommunityBoardEntry entry, int shift, int before, int overallBefore, Outcome outcome)
        {
            yield return new WaitForSecondsRealtime(ReactionInterval);

            // 믿음도가 움직인다. 게시판 몫이 바뀌면 작업 표시줄과 휴대폰 숫자가 따라 바뀐다.
            _tutorial.ShiftLegendBelief(_data.LegendId, shift);
            _lastShift = shift;
            if (_belief != null && _save != null) _belief.TryAddBelief(_save.Current, shift);

            // 이 글이 옮긴 몫을 기록에 남긴다. 게시판을 새로 지어 되살릴 때 이만큼만 한 번 다시 반영한다.
            var record = _save != null ? PlayerPostStore.Find(_save.Current, entry.PostId) : null;
            if (record != null) record.beliefShift = shift;

            int after = _tutorial.GetLegendBelief(_data.LegendId);
            int overallAfter = _tutorial.BoardBelief;
            string resultId = after < before ? ResultDownTextId : after > before ? ResultUpTextId : ResultFlatTextId;
            _communityScreen.ShowNotice(() => L(resultId, before, after, overallBefore, overallAfter));

            bool good = outcome == Outcome.Excellent || outcome == Outcome.Success;
            var reactions = good ? _data.SuccessReactions
                : outcome == Outcome.Fail ? _data.FailReactions
                : _data.WeakReactions;

            for (int i = 0; reactions != null && i < reactions.Count; i++)
            {
                entry.Comments.Add(new CommunityComment
                {
                    AuthorTextId = reactions[i].authorTextId,
                    BodyTextId = reactions[i].bodyTextId,
                });
                _communityScreen.BindComments(entry.Comments);

                yield return new WaitForSecondsRealtime(ReactionInterval);
            }

            yield return new WaitForSecondsRealtime(ResultButtonDelay);

            Debug.Log($"[PostWriting] 반응 끝 | 이번 괴담 {before}% -> {after}% | 전체 {overallBefore}% -> {overallAfter}% | {outcome}");
            SyncSavedPosts();

            // 결과로 저절로 넘어가지 않는다. 댓글을 다 읽고 플레이어가 누르면 넘어간다.
            // 댓글 쓰기 칸이 있던 자리에 단추 하나를 세운다. 기존 게시글 화면의 선택지 자리다.
            _resultBuilder = () => BuildResult(before, after, overallBefore, overallAfter, outcome);
            _resultOpened = false;
            _communityScreen.SetChoiceHeader(ResultReadyTextId);
            _communityScreen.BindChoices(new List<TutorialCommentChoice>
            {
                new TutorialCommentChoice { ChoiceId = ResultChoiceId, BodyTextId = SeeResultTextId },
            }, c => c != null ? L(c.BodyTextId) : string.Empty, _ => OpenResult());
            _communityScreen.SetControlsEnabled(true);
            _communityScreen.ScrollPostToBottom();
        }

        /// <summary>"게시 결과 확인"을 눌렀다. 두 번 눌러도 한 번만 연다.</summary>
        private void OpenResult()
        {
            if (_resultOpened || _resultBuilder == null) return;
            _resultOpened = true;

            _communityScreen.BindChoices(null, null, null);
            _view = View.Result;
            _compose.ShowResult(_resultBuilder());
        }

        private bool _resultOpened;
        private const string ResultChoiceId = "post_see_result";
        private const string SeeResultTextId = "ui.post.btn_see_result";
        private const string ResultReadyTextId = "ui.post.result_ready";

        /// <summary>마지막 댓글이 달리고 결과 확인 단추가 서기까지의 뜸(초). 짧게 둔다.</summary>
        private const float ResultButtonDelay = 0.6f;

        private PostComposeView.Result BuildResult(int before, int after, int overallBefore, int overallAfter, Outcome outcome)
        {
            Lines(before, out int success, out int excellent);

            // 믿음도를 가장 크게 올린 칸에 작은 경고를 붙인다. 올린 칸이 없으면 붙이지 않는다.
            int worst = WorstSection();

            var rows = new List<PostComposeView.ResultRow>();
            for (int s = 0; s < PostWritingSO.SectionCount; s++)
            {
                var c = Picked(s);
                if (c == null) continue;
                int d = Mathf.RoundToInt(c.beliefDelta);
                rows.Add(new PostComposeView.ResultRow
                {
                    Section = L(SectionNameTextIds[s]),
                    Sentence = L(c.textId),
                    Direction = Math.Sign(d),
                    Effect = d < 0 ? L(EffectDownTextId, -d) : d > 0 ? L(EffectUpTextId, d) : L(EffectFlatTextId),
                    Review = string.IsNullOrEmpty(c.reviewTextId) ? string.Empty : L(c.reviewTextId),
                    Warning = s == worst ? L(WorstWarningTextId) : null,
                });
            }

            int level = outcome == Outcome.Excellent ? 2 : outcome == Outcome.Success ? 1 : outcome == Outcome.Fail ? -1 : 0;
            return new PostComposeView.Result
            {
                Header = L(ResultHeaderTextId),
                Grade = L("ui.post.grade_" + outcome.ToString().ToLowerInvariant()),
                GradeLevel = level,
                Legend = L(ResultLegendTextId, before, after, after - before),
                Goal = L(ResultGoalTextId, success, excellent, Mathf.RoundToInt(_data.PostModifier)),
                Overall = L(ResultOverallTextId, overallBefore, overallAfter),
                Rows = rows,
                ConfirmLabel = L(ConfirmTextId),
                OnConfirm = () => OnResultConfirmed(outcome),
            };
        }

        /// <summary>평가를 읽었다. 한영이 한마디 하고, 잘 썼으면 끝내고 아니면 다시 쓴다.</summary>
        private void OnResultConfirmed(Outcome outcome)
        {
            switch (outcome)
            {
                case Outcome.Excellent: Talk(_data.ExcellentLineTextId, OnSucceeded); break;
                case Outcome.Success: Talk(_data.SuccessLineTextId, OnSucceeded); break;
                case Outcome.Fail: Talk(_data.FailLineTextId, TalkFixHint); break;
                case Outcome.Flat: Talk(_data.FlatLineTextId, TalkFixHint); break;
                default: Talk(_data.WeakLineTextId, TalkFixHint); break;
            }
        }

        private const string WorstWarningTextId = "ui.post.worst_warning";
        private static readonly string[] FixHintTextIds =
        {
            "post.fix.title",
            "post.fix.intro",
            "post.fix.fact",
            "post.fix.action",
        };

        /// <summary>믿음도를 가장 크게 올린 칸. 올린 칸이 없으면 -1.</summary>
        private int WorstSection()
        {
            int worst = -1;
            float max = 0f;
            for (int s = 0; s < PostWritingSO.SectionCount; s++)
            {
                var c = Picked(s);
                if (c != null && c.beliefDelta > max) { max = c.beliefDelta; worst = s; }
            }
            return worst;
        }

        /// <summary>
        /// 먼저 고쳐야 할 칸. 덧붙은 거짓을 걷어내지 않았으면 그 칸이 먼저다. 아니면 믿음을 가장 크게 올린 칸,
        /// 그것도 없으면 가장 적게 내린 칸이다. 어느 문장으로 바꾸라고는 말하지 않는다. 칸만 짚는다.
        /// </summary>
        private int SectionToFix()
        {
            int rs = _data.RefuteSection;
            if (rs >= 0 && rs < PostWritingSO.SectionCount && (Picked(rs) == null || Picked(rs).tag != PostTone.Refutation)) return rs;

            int worst = WorstSection();
            if (worst >= 0) return worst;

            float max = float.MinValue;
            for (int s = 0; s < PostWritingSO.SectionCount; s++)
            {
                var c = Picked(s);
                if (c != null && c.beliefDelta > max) { max = c.beliefDelta; worst = s; }
            }
            return worst;
        }

        /// <summary>못 미쳤을 때 한영이 한 칸을 짚어 준다. 정답을 골라 주지는 않는다.</summary>
        private void TalkFixHint()
        {
            int s = SectionToFix();
            if (s < 0) { AfterMiss(); return; }
            Talk(FixHintTextIds[s], AfterMiss);
        }

        /// <summary>한영이 말하게 한다. 말할 수 없으면 곧바로 다음으로 간다.</summary>
        private void Talk(string lineTextId, Action onDone)
        {
            bool talked = _tutorial != null && _tutorial.ShowComputerTalk(lineTextId, onDone);
            if (!talked) onDone?.Invoke();
        }

        /// <summary>잘 썼다. 글쓰기는 여기서 끝나고 저장본에 남는다.</summary>
        private void OnSucceeded()
        {
            _busy = false;
            HideCompose();

            if (_save != null && _save.Current != null)
            {
                string flag = PublishedFlagPrefix + _data.PostWritingId;
                if (!_save.Current.storyFlags.Contains(flag)) _save.Current.storyFlags.Add(flag);
                _save.MarkDirty();

                // 성공한 글과 그 반응을 곧바로 저장한다. 성공 표시만 남고 글이 사라지는 일이 없게 한다.
                _save.AutoSave();
            }

            _communityScreen.BindWrite(null);
            Debug.Log($"[PostWriting] 성공 | {_data.PostWritingId}");

            Succeeded?.Invoke(_data);
        }

        /// <summary>
        /// 목표에 못 미쳤다. 다시 쓸 수 있으면 다시 쓰게 한다. 고른 문장은 그대로 두고 첫 칸부터 다시 본다.
        ///
        /// 튜토리얼에서만 한영이 실패한 글을 내리고, 그 글이 옮긴 믿음도를 그대로 되돌린다.
        /// 몇 번을 틀려도 제대로 쓴 글 하나로 목표에 닿게 하려는 튜토리얼 규칙이다. 실전 글에는 적용하지 않는다.
        ///
        /// 순서: 한영이 내리겠다고 말한다 → 넘기면 실제로 내린다 → 게시판 목록이 갱신된 것을 보여 준다
        ///       → 믿음도가 돌아온 결과를 알리며 다시 쓰기 화면으로 간다. 내리기 전에 "내렸다"는 말은 보이지 않는다.
        /// </summary>
        private void AfterMiss()
        {
            if (!CanWrite(_data))
            {
                _busy = false;
                HideCompose();
                _communityScreen.BindWrite(null);
                Debug.Log($"[PostWriting] 더 쓸 수 없다 | {_data.PostWritingId} | {_attempts}번 올림");
                return;
            }

            bool takeDown = _data.IsTutorial && _data.TakeDownFailedPost && _lastPost != null;
            if (!takeDown)
            {
                _busy = false;
                _section = 0;
                ShowStep();
                return;
            }

            // 결과 화면을 띄운 채로 말한다. 뒤에 내릴 글이 보이지 않는다.
            Talk(_data.TakeDownLineTextId, () => StartCoroutine(TakeDownThenRewrite()));
        }

        /// <summary>목록이 갱신된 것을 보여 주는 시간(초).</summary>
        private const float TakenDownBoardSeconds = 1.2f;

        private IEnumerator TakeDownThenRewrite()
        {
            _communityScreen.SetControlsEnabled(false);

            int before = _tutorial.GetLegendBelief(_data.LegendId);
            int overallBefore = _tutorial.BoardBelief;

            _tutorial.RemoveBoardPost(_lastPost);
            if (_save != null) PlayerPostStore.MarkRemoved(_save.Current, _lastPost.PostId);
            _tutorial.ShiftLegendBelief(_data.LegendId, -_lastShift);
            if (_belief != null && _save != null) _belief.TryAddBelief(_save.Current, -_lastShift);
            if (_save != null) _save.MarkDirty();

            int after = _tutorial.GetLegendBelief(_data.LegendId);
            int overallAfter = _tutorial.BoardBelief;
            _lastPost = null;
            _lastShift = 0;

            Debug.Log($"[PostWriting] 실패한 글을 내림 | 이번 괴담 {before}% -> {after}% | 전체 {overallBefore}% -> {overallAfter}%");

            // 내린 뒤의 게시판 목록. 그 글이 빠진 것을 잠깐 보여 준다.
            HideCompose();
            _communityScreen.ShowNotice(null);
            _communityScreen.ShowBoard(true);
            yield return new WaitForSecondsRealtime(TakenDownBoardSeconds);

            _communityScreen.SetControlsEnabled(true);
            _busy = false;
            _section = 0;
            _takenDownNotice = () => L(TakenDownTextId, before, after, overallBefore, overallAfter);
            ShowStep();
        }

        /// <summary>글을 내린 직후 다시 쓰기 화면 맨 위에 한 번 띄우는 알림. 다음 단계로 가면 사라진다.</summary>
        private Func<string> _takenDownNotice;
    }
}
