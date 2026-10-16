using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 본편 괴담넷 댓글. 숙소 컴퓨터로 사건 괴담 글을 열면 그 글에 댓글을 달 수 있다(휴대폰으로는 읽기만 한다).
    ///
    ///   사건 글     사건 데이터(PostWritingSO.CommentOptions)의 댓글. 각각 이 사건에서 한 번만 쓴다.
    ///   질문 글     근무일마다 올라오는 질문 글. 글마다 답글 하나만 단다. 선택지는 그 글의 틀에 있다.
    ///   근거        근거(단서·분석 기록)가 있어야 보이는 댓글은 그 근거를 얻은 뒤에 보인다.
    ///   확정        등록 직전에 확인창을 띄운다. 등록한 댓글은 고치거나 지우지 않는다. 시간이 흐르고 믿음도가 한 번 움직인다.
    ///   저장        댓글 내용과 믿음도는 게시판 글 상태에, 단 댓글은 사건별 댓글 기록에 남는다. 불러와도 다시 반영하지 않는다.
    /// 튜토리얼의 댓글 연습(인기글)과는 따로다. 튜토리얼에서는(InMain 이 아닐 때) 아무것도 하지 않는다.
    /// </summary>
    public partial class CaseDirector
    {
        /// <summary>괴담넷을 숙소 컴퓨터로 열었는가. 휴대폰으로 본 글에는 댓글을 달지 않는다.</summary>
        private bool _netOnComputer;

        private const string CommentHeaderTextId = "ui.net.main_comment_header";
        private const string CommentNoneTextId = "ui.net.comment_none";
        private const string CommentDoneTextId = "ui.net.comment_done";

        /// <summary>열어 본 글이 풀리지 않은 본편 사건의 괴담 글이면, 달 수 있는 댓글을 선택지로 세운다.</summary>
        private void BindMainComments(CommunityBoardEntry entry)
        {
            if (!InMain || !_netOnComputer || entry == null || entry.IsPlayerPost || _communityScreen == null) return;
            var c = MainCaseOfLegend(entry.LegendId);
            if (c == null || c.PostWriting == null) return;

            var options = AvailableComments(c, entry);
            var choices = new List<TutorialCommentChoice>();
            foreach (var o in options) choices.Add(new TutorialCommentChoice { ChoiceId = o.optionId, BodyTextId = o.bodyTextId });

            if (choices.Count == 0)
            {
                _communityScreen.SetChoiceHeader(_workday.FindQuestion(entry) != null && _workday.CommentUsed(c.CaseId, null, WorkdayDirector.EntryKey(entry))
                    ? CommentDoneTextId : CommentNoneTextId);
                _communityScreen.BindChoices(null, null, null);
                return;
            }

            _communityScreen.SetChoiceHeader(CommentHeaderTextId);
            _communityScreen.BindChoices(choices, ch => ch != null ? _loc.Get(ch.BodyTextId) : string.Empty, ch =>
            {
                var picked = options.Find(o => o.optionId == ch.ChoiceId);
                if (picked != null) AskComment(entry, c, picked);
            });
        }

        /// <summary>이 괴담을 다루는, 아직 풀리지 않은 활성 사건.</summary>
        private CaseSO MainCaseOfLegend(string legendId)
        {
            if (string.IsNullOrEmpty(legendId)) return null;
            foreach (var c in _workday.ActiveCases())
                if (c != null && c.LegendId == legendId && !_workday.IsSolved(c.CaseId)) return c;
            return null;
        }

        /// <summary>이 글에 지금 달 수 있는 댓글. 이미 쓴 것과 근거가 모자란 것은 빠진다.</summary>
        private List<CaseCommentOption> AvailableComments(CaseSO c, CommunityBoardEntry entry)
        {
            var list = new List<CaseCommentOption>();
            var q = _workday.FindQuestion(entry);
            string key = WorkdayDirector.EntryKey(entry);
            if (q != null)
            {
                // 질문 글에는 답글 하나만 단다.
                if (_workday.CommentUsed(c.CaseId, null, key)) return list;
                var templates = c.PostWriting.QuestionTemplates;
                if (q.Template < 0 || q.Template >= templates.Count) return list;
                foreach (var o in templates[q.Template].options)
                    if (o != null && HasCommentEvidence(c, o)) list.Add(o);
                return list;
            }

            foreach (var o in c.PostWriting.CommentOptions)
                if (o != null && !_workday.CommentUsed(c.CaseId, o.optionId) && HasCommentEvidence(c, o)) list.Add(o);
            return list;
        }

        private bool HasCommentEvidence(CaseSO c, CaseCommentOption o)
        {
            if (o.requiredEvidenceIds == null) return true;
            foreach (var id in o.requiredEvidenceIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (!_save.Current.acquiredClueIds.Contains(id) && !_save.Current.analysisRecords.Contains(c.CaseId + "|" + id)) return false;
            }
            return true;
        }

        private void AskComment(CommunityBoardEntry entry, CaseSO c, CaseCommentOption option)
        {
            ConfirmDialog.Show(_communityScreen.Window, _communityScreen.BodyFont, _communityScreen.TitleFont,
                _loc.Get("ui.net.confirm_comment"), _loc.Get("ui.net.confirm_comment_yes"), _loc.Get("ui.post.confirm_back"),
                () => PostMainComment(entry, c, option), null);
        }

        /// <summary>
        /// 댓글을 실제로 단다(확인창에서 등록을 고른 순간 한 번).
        /// 시간이 먼저 흐르고(그동안의 확산 포함), 그 뒤의 믿음도에서 이 댓글의 몫이 움직인다. 정정글과 같은 순서다.
        /// </summary>
        private void PostMainComment(CommunityBoardEntry entry, CaseSO c, CaseCommentOption option)
        {
            string key = WorkdayDirector.EntryKey(entry);
            bool question = _workday.FindQuestion(entry) != null;
            // 두 번 눌렸거나 그사이 다른 길로 이미 달았으면 아무것도 하지 않는다.
            if (question ? _workday.CommentUsed(c.CaseId, null, key) : _workday.CommentUsed(c.CaseId, option.optionId)) return;

            AdvanceTime(_workday.Balance.CommentMinutes);

            float before = _workday.DisplayBelief(c.LegendId);
            float overallBefore = _workday.DisplayOverall;
            int shift = Mathf.RoundToInt(option.beliefDelta);

            entry.Comments ??= new List<CommunityComment>();
            entry.Comments.Add(new CommunityComment { AuthorTextId = "ui.net.author_anon", BodyTextId = option.bodyTextId, IsPlayer = true });
            if (shift != 0 && _tutorial != null) _tutorial.ShiftLegendBelief(c.LegendId, shift);
            _workday.LogComment(c.CaseId, option.optionId, key, shift);

            float after = _workday.DisplayBelief(c.LegendId);
            float overallAfter = _workday.DisplayOverall;
            _communityScreen.BindComments(entry.Comments);
            string resultId = after < before ? "ui.post.result_down" : after > before ? "ui.post.result_up" : "ui.post.result_flat";
            _communityScreen.ShowNotice(() => _loc.Get(resultId, Pct(before), Pct(after), Pct(overallBefore), Pct(overallAfter)));
            _communityScreen.ScrollPostToBottom();
            PushStatus();
            Debug.Log($"[CaseDirector] 본편 | 댓글 | {c.CaseId} {option.optionId} on {key} | {shift:+0;-0;0}% | {before:0.0}% -> {after:0.0}%");

            BindMainComments(entry);

            // 댓글로 목표 아래까지 내려왔을 수 있다. 다른 조건이 모두 차 있으면 여기서 정식 해결이다.
            if (shift < 0 && c.PostWriting != null) TryResolveMainCase(c.PostWriting, quiet: true);
        }

        /// <summary>
        /// 읽은 글이 주는 단서. 수사 노트의 단서 칸에 "이 인터넷 글에서 얻는다(fromPageId)"로 적힌 것만 준다.
        /// 사건이 풀리지 않은 동안에만 준다. 추가 인터넷 조사로 새 근거를 얻는 길이다.
        /// </summary>
        private void GrantPageClues(WebPageSO page)
        {
            if (!InMain || page == null || _workday == null) return;
            foreach (var c in _workday.ActiveCases())
            {
                var data = c != null ? c.Deduction : null;
                if (data == null) continue;
                foreach (var fc in data.FieldClues)
                {
                    if (fc == null || fc.fromPageId != page.PageId) continue;
                    if (!CaseFlow.AcquireClue(_save, fc.clueId)) continue;
                    ShowMainToast(_loc.Get("ui.net.clue_from_page", ResolveClueTitleFor(c, fc.clueId)));
                    Debug.Log($"[CaseDirector] 본편 | 글에서 단서 | {page.PageId} -> {fc.clueId} ({c.CaseId})");
                }
            }
        }

        private string ResolveClueTitleFor(CaseSO c, string clueId)
        {
            var legend = _legends != null ? _legends.GetLegend(c.LegendId) : null;
            if (legend == null) return clueId;
            foreach (var clue in legend.Clues)
            {
                if (clue == null || clue.ClueId != clueId || string.IsNullOrEmpty(clue.ClueTextId)) continue;
                string titleId = clue.ClueTextId.EndsWith(".text") ? clue.ClueTextId.Substring(0, clue.ClueTextId.Length - 5) + ".title" : clue.ClueTextId;
                string title = _loc.Get(titleId);
                return title == titleId ? _loc.Get(clue.ClueTextId) : title;
            }
            return clueId;
        }
    }
}
