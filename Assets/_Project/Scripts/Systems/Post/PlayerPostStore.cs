using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Save;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 플레이어가 괴담넷에 올린 글을 저장본(SaveData.boardPosts)과 게시판 사이에서 옮긴다.
    ///
    /// 게시판 목록은 저장하지 않고 매번 새로 짓는다(TutorialDirector). 그래서 플레이어 글만 따로 적어 두었다가
    /// 게시판을 지을 때 되살린다. 저장본의 기록과 게시판의 줄은 PostId 로 짝지어진다.
    ///   올릴 때    Record   - 기록을 만든다
    ///   바뀔 때    Sync     - 조회수 / 반응 / 댓글 / 성장 몫을 기록으로 옮긴다 (저장 직전, 게시판 시간이 흐를 때)
    ///   내릴 때    MarkRemoved - 튜토리얼에서 한영이 실패한 글을 내려 줄 때만. 본편 글은 고치거나 내리지 않는다
    ///   지을 때    Restore  - 아직 게시판에 없는 기록만 되살리고, 그 글이 옮긴 믿음도를 한 번만 다시 반영한다
    ///   사건을 처음부터 다시 할 때  RemoveCase - 새 판을 시작할 때만 쓴다. 게임 안에서 글을 지우는 것이 아니다
    /// </summary>
    public static class PlayerPostStore
    {
        public static SavedBoardPost Find(SaveData save, string postId)
        {
            if (save == null || save.boardPosts == null || string.IsNullOrEmpty(postId)) return null;
            foreach (var r in save.boardPosts)
            {
                if (r != null && r.postId == postId) return r;
            }
            return null;
        }

        /// <summary>방금 올린 글의 기록을 만든다. 같은 ID 가 이미 있으면 그것을 돌려준다.</summary>
        public static SavedBoardPost Record(SaveData save, CommunityBoardEntry entry, string caseId, string postWritingId, string legendId)
        {
            if (save == null || entry == null || string.IsNullOrEmpty(entry.PostId)) return null;
            save.boardPosts ??= new List<SavedBoardPost>();

            var record = Find(save, entry.PostId);
            if (record == null)
            {
                record = new SavedBoardPost
                {
                    postId = entry.PostId,
                    caseId = caseId,
                    postWritingId = postWritingId,
                    legendId = legendId,
                    authorTextId = entry.AuthorTextId,
                    boardTextId = entry.BoardTextId,
                    titleTextId = entry.TitleTextId,
                    bodyTextIds = entry.BodyTextIds != null ? new List<string>(entry.BodyTextIds) : new List<string>(),
                    postedAtMinute = -entry.PostedMinutesAgo,
                };
                save.boardPosts.Add(record);
            }

            CopyFrom(record, entry);
            return record;
        }

        /// <summary>게시판에 걸린 플레이어 글의 지금 숫자를 기록으로 옮긴다.</summary>
        public static void Sync(SaveData save, IReadOnlyList<CommunityBoardEntry> entries)
        {
            if (save == null || save.boardPosts == null || entries == null) return;
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.PostId)) continue;
                var record = Find(save, entry.PostId);
                if (record != null && !record.removed) CopyFrom(record, entry);
            }
        }

        /// <summary>튜토리얼에서 한영이 실패한 글을 내렸을 때. 본편 글은 내리지 않는다.</summary>
        public static void MarkRemoved(SaveData save, string postId)
        {
            var record = Find(save, postId);
            if (record != null) record.removed = true;
        }

        /// <summary>
        /// 게시판을 새로 지은 뒤 부른다. 내리지 않은 글 가운데 게시판에 아직 없는 것만 되살린다.
        /// 같은 글이 두 번 걸리지 않고, 그 글이 옮긴 믿음도도 되살릴 때 한 번만 반영된다.
        /// </summary>
        public static int Restore(SaveData save, TutorialDirector tutorial)
        {
            if (save == null || save.boardPosts == null || tutorial == null) return 0;

            // 본편 저장본은 글마다의 믿음도를 따로 적어 두고 그 값으로 덮는다(WorkdayDirector). 그 값에는 이 글이 옮긴 몫이
            // 이미 들어 있으므로 여기서 또 옮기면 두 번 반영된다. 어느 쪽이 먼저 불려도 같은 결과가 나오게 여기서는 옮기지 않는다.
            bool beliefsSaved = save.work != null && save.work.mainStarted && save.boardStates != null && save.boardStates.Count > 0;

            int restored = 0;
            foreach (var record in save.boardPosts)
            {
                if (record == null || record.removed || string.IsNullOrEmpty(record.postId)) continue;
                if (OnBoard(tutorial.BoardEntries, record.postId)) continue;

                tutorial.AddBoardPost(ToEntry(record));
                if (record.beliefShift != 0 && !beliefsSaved) tutorial.ShiftLegendBelief(record.legendId, record.beliefShift);
                restored++;
                Debug.Log($"[PlayerPostStore] 저장된 글을 게시판에 되살림 | {record.postId} ({record.titleTextId}) 조회 {record.views} 댓글 {record.comments.Count} 믿음 {record.beliefShift:+0;-0;0}%p");
            }
            return restored;
        }

        /// <summary>
        /// 사건을 처음부터 다시 할 때. 그 사건에서 올린 글의 기록을 지우고, 게시판에 걸려 있으면 내리고 옮긴 믿음도도 되돌린다.
        /// </summary>
        public static void RemoveCase(SaveData save, string caseId, TutorialDirector tutorial)
        {
            if (save == null || save.boardPosts == null || string.IsNullOrEmpty(caseId)) return;

            for (int i = save.boardPosts.Count - 1; i >= 0; i--)
            {
                var record = save.boardPosts[i];
                if (record == null || record.caseId != caseId) continue;

                if (tutorial != null && !record.removed)
                {
                    var entry = FindEntry(tutorial.BoardEntries, record.postId);
                    if (entry != null && tutorial.RemoveBoardPost(entry) && record.beliefShift != 0)
                        tutorial.ShiftLegendBelief(record.legendId, -record.beliefShift);
                }
                save.boardPosts.RemoveAt(i);
            }
        }

        public static CommunityBoardEntry ToEntry(SavedBoardPost r)
        {
            var comments = new List<CommunityComment>();
            foreach (var line in r.comments)
            {
                if (string.IsNullOrEmpty(line)) continue;
                var parts = line.Split('|');
                comments.Add(new CommunityComment
                {
                    AuthorTextId = parts[0],
                    BodyTextId = parts.Length > 1 ? parts[1] : string.Empty,
                    IsPlayer = parts.Length > 2 && parts[2] == "1",
                });
            }

            return new CommunityBoardEntry
            {
                PostId = r.postId,
                TitleTextId = r.titleTextId,
                BodyTextIds = new List<string>(r.bodyTextIds),
                AuthorTextId = r.authorTextId,
                BoardTextId = r.boardTextId,
                Views = r.views,
                Likes = r.likes,
                Dislikes = r.dislikes,
                LikePressed = r.likePressed,
                DislikePressed = r.dislikePressed,
                PostedMinutesAgo = -r.postedAtMinute,
                Comments = comments,
                IsPlayerPost = true,
                GrowthCarry = r.growthCarry != null && r.growthCarry.Length == 4 ? (float[])r.growthCarry.Clone() : new float[4],
                GrowthAddedComments = r.growthAddedComments,
            };
        }

        private static void CopyFrom(SavedBoardPost record, CommunityBoardEntry entry)
        {
            record.views = entry.Views;
            record.likes = entry.Likes;
            record.dislikes = entry.Dislikes;
            record.likePressed = entry.LikePressed;
            record.dislikePressed = entry.DislikePressed;
            record.growthAddedComments = entry.GrowthAddedComments;
            record.growthCarry = entry.GrowthCarry != null && entry.GrowthCarry.Length == 4 ? (float[])entry.GrowthCarry.Clone() : new float[4];

            record.comments ??= new List<string>();
            record.comments.Clear();
            if (entry.Comments == null) return;
            foreach (var c in entry.Comments)
            {
                if (c == null) continue;
                record.comments.Add(c.AuthorTextId + "|" + c.BodyTextId + "|" + (c.IsPlayer ? "1" : "0"));
            }
        }

        private static bool OnBoard(IReadOnlyList<CommunityBoardEntry> entries, string postId) => FindEntry(entries, postId) != null;

        private static CommunityBoardEntry FindEntry(IReadOnlyList<CommunityBoardEntry> entries, string postId)
        {
            if (entries == null) return null;
            foreach (var e in entries)
            {
                if (e != null && e.PostId == postId) return e;
            }
            return null;
        }
    }
}
