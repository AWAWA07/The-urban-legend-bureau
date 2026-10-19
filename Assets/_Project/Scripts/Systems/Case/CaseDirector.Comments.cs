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
    ///   사건 글     사건 데이터(PostWritingSO.CommentOptions)의 댓글. 그 괴담 믿음도가 해결 목표(기본 30%) 이상인 동안
    ///               같은 댓글도 몇 번이고 단다(댓글마다 근무 시간이 든다). 목표 아래로 내려오면 더 달 수 없다.
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
        private const string CommentLowTextId = "ui.net.comment_low";

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
                bool question = _workday.FindQuestion(entry) != null;
                _communityScreen.SetChoiceHeader(question && _workday.CommentUsed(c.CaseId, null, WorkdayDirector.EntryKey(entry))
                    ? CommentDoneTextId : !question && !CommentsOpen(c) ? CommentLowTextId : CommentNoneTextId);
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

            // 사건 글에는 그 괴담 믿음도가 해결 목표(기본 30%) 이상인 동안 댓글을 단다. 목표 아래로 내려오면 더 달 수 없다.
            if (!CommentsOpen(c)) return list;

            string pageId = entry.Page != null ? entry.Page.PageId : null;
            foreach (var o in c.PostWriting.CommentOptions)
                if (o != null && CommentFits(c, o, pageId, key) && HasCommentEvidence(c, o)) list.Add(o);
            return list;
        }

        /// <summary>
        /// 이 사건의 괴담 글에 댓글을 달 수 있는가. 믿음도가 해결 목표(기본 30%) 이상일 때다.
        /// 그동안은 같은 댓글도 시간만 있으면 몇 번이고 단다. 목표 아래로 내려온 뒤로는 정정글과 질문 글로만 다룬다.
        /// </summary>
        private bool CommentsOpen(CaseSO c)
        {
            return c != null && _workday != null && _workday.DisplayBelief(c.LegendId) >= _workday.ResolveTarget(c);
        }

        /// <summary>
        /// 사건 글에 이 댓글을 달 수 있는가.
        ///   그 글을 겨눈 댓글인가(pageIds 가 비었으면 어느 글이든)
        ///   이 글에 이미 단 댓글이 아닌가
        ///   반박 댓글이면 그 주장을 이 사건에서 아직 반박하지 않았는가(다른 글에서 같은 주장을 반박했어도 빠진다)
        /// 글마다 다른 잘못된 주장이 있으면 주장마다 따로 반박할 수 있다. 같은 주장을 여러 글에서 거듭 반박해 깎을 수는 없다.
        /// </summary>
        private bool CommentFits(CaseSO c, CaseCommentOption o, string pageId, string key)
        {
            // 그 글을 겨눈 댓글인가만 본다. 믿음도가 목표 이상인 동안은 같은 댓글을 되풀이해 달 수 있다(CommentsOpen).
            // 댓글마다 근무 시간이 들어, 하루에 다는 수는 남은 시간이 정한다.
            if (o.pageIds != null && o.pageIds.Count > 0 && (pageId == null || !o.pageIds.Contains(pageId))) return false;
            return CommentsOpen(c);
        }

        /// <summary>결과 글에 넣는 숫자. 글 쪽에 % 가 붙어 있어 여기서는 숫자만 낸다(정정글 결과와 같은 모양).</summary>
        private static string Num(float v) => v.ToString("0.#");

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
            if (question ? _workday.CommentUsed(c.CaseId, null, key) : !CommentFits(c, option, entry.Page != null ? entry.Page.PageId : null, key)) return;

            AdvanceTime(_workday.Balance.CommentMinutes);

            float before = _workday.DisplayBelief(c.LegendId);
            float overallBefore = _workday.DisplayOverall;
            int shift = Mathf.RoundToInt(option.beliefDelta);

            entry.Comments ??= new List<CommunityComment>();
            entry.Comments.Add(new CommunityComment { AuthorTextId = "ui.net.author_anon", BodyTextId = option.bodyTextId, IsPlayer = true });
            if (shift != 0 && _tutorial != null) _tutorial.ShiftLegendBelief(c.LegendId, shift);
            // 반박 댓글은 믿음도를 내렸을 때만 그 주장을 반박한 것으로 적는다.
            _workday.LogComment(c.CaseId, option.optionId, key, shift, shift < 0 ? option.claimId : null);

            float after = _workday.DisplayBelief(c.LegendId);
            float overallAfter = _workday.DisplayOverall;
            _communityScreen.BindComments(entry.Comments);
            string resultId = after < before ? "ui.post.result_down" : after > before ? "ui.post.result_up" : "ui.post.result_flat";
            _communityScreen.ShowNotice(() => _loc.Get(resultId, Num(before), Num(after), Num(overallBefore), Num(overallAfter)));
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
