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
    /// 따로 글쓰기 화면을 두지 않는다. 기존 괴담넷의 글 화면을 "쓰고 있는 글"로 쓴다.
    /// 고른 문장이 실제 게시글 본문에 한 줄씩 쌓이고, 아래 댓글 쓰기 칸에 다음 칸의 후보가 뜬다.
    /// 네 칸을 다 고르면 올리기 전 모습 그대로가 미리보기가 된다. 올리면 게시판에 실제로 걸린다.
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

        /// <summary>지금 고르고 있는 칸. 0 제목, 1 도입, 2 사실, 3 행동 지침, 4 미리보기.</summary>
        private int _section;

        private readonly int[] _picks = new int[PostWritingSO.SectionCount];

        /// <summary>이번에 올린 횟수. 사건을 새로 열면 처음부터 센다.</summary>
        private int _attempts;

        /// <summary>마지막으로 올린 글과 그 글이 옮긴 믿음 몫. 다시 쓸 때 되돌리는 데 쓴다.</summary>
        private CommunityBoardEntry _lastPost;
        private int _lastShift;

        /// <summary>반응이 달리는 중인가. 그 사이에는 무엇도 새로 시작하지 않는다.</summary>
        private bool _busy;

        private const string PublishedFlagPrefix = "post_published:";

        private const string TitlePlaceholderTextId = "ui.post.title_placeholder";
        private static readonly string[] BodyPlaceholderTextIds =
        {
            null,
            "ui.post.placeholder_intro",
            "ui.post.placeholder_fact",
            "ui.post.placeholder_action",
        };

        private static readonly string[] SectionHeaderTextIds =
        {
            "ui.post.header_title",
            "ui.post.header_intro",
            "ui.post.header_fact",
            "ui.post.header_action",
        };

        private static readonly string[] SectionNameTextIds =
        {
            "ui.post.section_title",
            "ui.post.section_intro",
            "ui.post.section_fact",
            "ui.post.section_action",
        };

        private const string PreviewHeaderTextId = "ui.post.header_preview";
        private const string PublishTextId = "ui.post.btn_publish";
        private const string RewriteTextId = "ui.post.btn_rewrite";
        private const string PublishChoiceId = "post_publish";
        private const string RewriteChoiceId = "post_rewrite";

        private const string FeedbackDownTextId = "ui.post.feedback_down";
        private const string FeedbackUpTextId = "ui.post.feedback_up";
        private const string FeedbackFlatTextId = "ui.post.feedback_flat";
        private const string ResultDownTextId = "ui.post.result_down";
        private const string ResultUpTextId = "ui.post.result_up";
        private const string ResultFlatTextId = "ui.post.result_flat";
        private const string TakenDownTextId = "ui.post.taken_down";

        /// <summary>댓글이 하나씩 달리는 간격(초). 댓글 연습 때와 같은 빠르기다.</summary>
        private const float ReactionInterval = 0.8f;

        /// <summary>마지막 반응을 읽고 한영이 입을 열기까지의 뜸(초).</summary>
        private const float TalkDelay = 1.4f;

        private void Start()
        {
            ServiceRegistry.TryGet(out _loc);
            ServiceRegistry.TryGet(out _save);
            ServiceRegistry.TryGet(out _belief);
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

        /// <summary>사건을 새로 열 때 부른다. 지난번 사건의 횟수와 올린 글 기록을 버린다.</summary>
        public void ResetProgress()
        {
            StopAllCoroutines();
            _data = null;
            _attempts = 0;
            _lastPost = null;
            _lastShift = 0;
            _busy = false;
        }

        // ------------------------------------------------------------- 쓰기

        /// <summary>
        /// 글쓰기 단추를 눌렀을 때. 튜토리얼이면 한영이 먼저 한마디 한다.
        /// 보고서를 그대로 올리면 안 된다는 것, 읽는 사람이 안심하게 써야 한다는 것을 이른다.
        /// </summary>
        public void Begin(PostWritingSO data)
        {
            if (data == null || _communityScreen == null || _busy || !CanWrite(data)) return;

            _data = data;

            Debug.Log($"[PostWriting] 글쓰기 시작 | {data.PostWritingId} | {_attempts + 1}번째");

            bool talked = data.IsTutorial && _tutorial != null
                          && _tutorial.ShowComputerTalk(data.IntroLineTextId, StartDraft);
            if (!talked) StartDraft();
        }

        /// <summary>처음부터 새로 쓴다. 고른 것은 모두 비운다.</summary>
        private void StartDraft()
        {
            _section = 0;
            for (int i = 0; i < _picks.Length; i++) _picks[i] = -1;

            _communityScreen.ShowNotice(null);
            ShowDraft();
        }

        /// <summary>
        /// 쓰고 있는 글을 괴담넷 글 화면에 건다.
        /// 고른 문장은 본문에, 아직 고르지 않은 지금 칸은 자리 표시로 선다. 아래에는 이 칸의 후보가 뜬다.
        /// </summary>
        private void ShowDraft()
        {
            string titleId = _picks[0] >= 0 ? Picked(0).textId : TitlePlaceholderTextId;

            var body = new List<string>();
            for (int s = 1; s < PostWritingSO.SectionCount; s++)
            {
                if (_picks[s] >= 0) body.Add(Picked(s).textId);
                else if (s == _section) body.Add(BodyPlaceholderTextIds[s]);
            }

            // 아직 올리지 않은 글이다. 방금 쓴 것으로 보이도록 올린 시각을 지금으로 둔다.
            _communityScreen.BindPost(titleId, body, _data.AuthorTextId, _data.BoardTextId,
                0, -GameClock.ElapsedMinutes);
            _communityScreen.BindComments(new List<CommunityComment>());
            _communityScreen.BindReactions(null, null);

            if (_section < PostWritingSO.SectionCount)
            {
                var candidates = _data.GetCandidates(_section);
                var choices = new List<TutorialCommentChoice>();
                if (candidates != null)
                {
                    foreach (var c in candidates)
                    {
                        if (c == null) continue;
                        choices.Add(new TutorialCommentChoice { ChoiceId = c.candidateId, BodyTextId = c.textId });
                    }
                }

                _communityScreen.SetChoiceHeader(SectionHeaderTextIds[_section]);
                _communityScreen.BindChoices(choices, ChoiceLabel, OnCandidatePicked);
            }
            else
            {
                // 다 골랐다. 올리기 전에 한 번 본다. 마지막으로 고른 문장의 방향 알림은 그대로 둔다.
                _communityScreen.SetChoiceHeader(PreviewHeaderTextId);
                _communityScreen.BindChoices(new List<TutorialCommentChoice>
                {
                    new TutorialCommentChoice { ChoiceId = PublishChoiceId, BodyTextId = PublishTextId },
                    new TutorialCommentChoice { ChoiceId = RewriteChoiceId, BodyTextId = RewriteTextId },
                }, ChoiceLabel, OnPreviewPicked);
            }

            _communityScreen.ShowBoard(false);
        }

        private string ChoiceLabel(TutorialCommentChoice choice)
        {
            return choice == null ? string.Empty : _loc.Get(choice.BodyTextId);
        }

        private PostWritingCandidate Picked(int section)
        {
            var list = _data.GetCandidates(section);
            int i = _picks[section];
            return list != null && i >= 0 && i < list.Count ? list[i] : null;
        }

        /// <summary>
        /// 후보 하나를 골랐다. 이 문장이 믿음을 어느 쪽으로 미는지만 알린다.
        /// 숫자도 문장의 성격 이름도 보여주지 않는다. 방향을 보고 규칙을 스스로 알아 가게 한다.
        /// </summary>
        private void OnCandidatePicked(TutorialCommentChoice choice)
        {
            if (choice == null || _data == null || _section >= PostWritingSO.SectionCount) return;

            var list = _data.GetCandidates(_section);
            int index = -1;
            for (int i = 0; list != null && i < list.Count; i++)
            {
                if (list[i] != null && list[i].candidateId == choice.ChoiceId) { index = i; break; }
            }
            if (index < 0) return;

            _picks[_section] = index;
            var picked = list[index];

            string sectionId = SectionNameTextIds[_section];
            string feedbackId = picked.beliefDelta < 0f ? FeedbackDownTextId
                : picked.beliefDelta > 0f ? FeedbackUpTextId
                : FeedbackFlatTextId;
            _communityScreen.ShowNotice(() => _loc.Get(feedbackId, _loc.Get(sectionId)));

            Debug.Log($"[PostWriting] {_section}칸 | {picked.candidateId} ({picked.tag}, {picked.beliefDelta:+0.#;-0.#;0})");

            _section++;
            ShowDraft();
        }

        private void OnPreviewPicked(TutorialCommentChoice choice)
        {
            if (choice == null || _busy) return;

            if (choice.ChoiceId == RewriteChoiceId)
            {
                StartDraft();
                return;
            }

            if (choice.ChoiceId == PublishChoiceId) Publish();
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

        private enum Outcome { Success, Weak, Fail }

        /// <summary>글을 실제로 게시판에 건다. 반응이 달리고 믿음도가 움직인 뒤 한영이 평한다.</summary>
        private void Publish()
        {
            if (_tutorial == null) return;

            int shift = SumDelta();
            int before = _tutorial.GetLegendBelief(_data.LegendId);
            int expected = Mathf.Clamp(before + shift, 0, 100);

            var outcome = expected <= _data.TutorialTargetBelief ? Outcome.Success
                : shift > 0 ? Outcome.Fail
                : Outcome.Weak;

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
                Likes = outcome == Outcome.Success ? _data.SuccessLikes : outcome == Outcome.Fail ? _data.FailLikes : _data.WeakLikes,
                Dislikes = outcome == Outcome.Success ? _data.SuccessDislikes : outcome == Outcome.Fail ? _data.FailDislikes : _data.WeakDislikes,
                IsPlayerPost = true,
            };

            _tutorial.AddBoardPost(entry);
            _attempts++;
            _lastPost = entry;

            // 올린 글을 연다. 게시판에 걸린 그 글이다.
            _tutorial.OpenFillerPost(entry);
            _communityScreen.SetControlsEnabled(false);

            Debug.Log($"[PostWriting] 게시 | {_data.PostWritingId} {_attempts}번째 | 몫 {shift:+0;-0;0}% | 결과 {outcome}");
            StartCoroutine(PlayReactions(entry, shift, before, outcome));
        }

        private IEnumerator PlayReactions(CommunityBoardEntry entry, int shift, int before, Outcome outcome)
        {
            _busy = true;

            yield return new WaitForSecondsRealtime(ReactionInterval);

            // 믿음도가 움직인다. 게시판 몫이 바뀌면 작업 표시줄과 휴대폰 숫자가 따라 바뀐다.
            _tutorial.ShiftLegendBelief(_data.LegendId, shift);
            _lastShift = shift;
            if (_belief != null && _save != null) _belief.TryAddBelief(_save.Current, shift);

            int after = _tutorial.GetLegendBelief(_data.LegendId);
            string resultId = after < before ? ResultDownTextId : after > before ? ResultUpTextId : ResultFlatTextId;
            _communityScreen.ShowNotice(() => _loc.Get(resultId, before, after));

            var reactions = outcome == Outcome.Success ? _data.SuccessReactions
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

            yield return new WaitForSecondsRealtime(TalkDelay);

            _communityScreen.SetControlsEnabled(true);
            _busy = false;

            Debug.Log($"[PostWriting] 반응 끝 | 이 괴담 {before}% -> {after}% (목표 {_data.TutorialTargetBelief}%) | {outcome}");

            if (outcome == Outcome.Success)
            {
                Talk(_data.SuccessLineTextId, OnSucceeded);
                yield break;
            }

            Talk(outcome == Outcome.Fail ? _data.FailLineTextId : _data.WeakLineTextId, AfterMiss);
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
            if (_save != null && _save.Current != null)
            {
                string flag = PublishedFlagPrefix + _data.PostWritingId;
                if (!_save.Current.storyFlags.Contains(flag)) _save.Current.storyFlags.Add(flag);
                _save.MarkDirty();
            }

            _communityScreen.BindWrite(null);
            Debug.Log($"[PostWriting] 성공 | {_data.PostWritingId} | 다음 튜토리얼 단계로 넘어갈 자리");
        }

        /// <summary>
        /// 목표에 못 미쳤다. 다시 쓸 수 있으면 다시 쓰게 한다.
        /// 튜토리얼에서는 한영이 실패한 글을 내리고, 그 글이 올려 둔 믿음도 되돌린다.
        /// 그래야 몇 번을 틀려도 제대로 쓴 글 하나로 목표에 닿을 수 있다.
        /// </summary>
        private void AfterMiss()
        {
            if (!CanWrite(_data))
            {
                _communityScreen.BindWrite(null);
                Debug.Log($"[PostWriting] 더 쓸 수 없다 | {_data.PostWritingId} | {_attempts}번 올림");
                return;
            }

            if (!_data.TakeDownFailedPost || _lastPost == null)
            {
                StartDraft();
                return;
            }

            int before = _tutorial.GetLegendBelief(_data.LegendId);
            _tutorial.RemoveBoardPost(_lastPost);
            _tutorial.ShiftLegendBelief(_data.LegendId, -_lastShift);
            if (_belief != null && _save != null) _belief.TryAddBelief(_save.Current, -_lastShift);
            int after = _tutorial.GetLegendBelief(_data.LegendId);

            _lastPost = null;
            _lastShift = 0;

            Debug.Log($"[PostWriting] 실패한 글을 내림 | 이 괴담 {before}% -> {after}%");

            Talk(_data.TakeDownLineTextId, () =>
            {
                StartDraft();
                _communityScreen.ShowNotice(() => _loc.Get(TakenDownTextId, before, after));
            });
        }
    }
}
