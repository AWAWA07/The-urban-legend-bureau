using System;
using System.Collections.Generic;
using UnityEngine;

namespace UrbanLegendBureau.Data
{
    /// <summary>
    /// 시간이 흐르면 괴담넷에 새로 올라오는 글 하나.
    /// 게시판을 채우는 다른 글들과 같은 문구 규칙(board.filler.NNN)을 쓴다.
    /// </summary>
    [Serializable]
    public class ScheduledBoardPost
    {
        [Tooltip("그날 밤 시계가 시작한 뒤 몇 분째에 올라오는가. 22:30 이 0 이다.")]
        public int atElapsedMinute;

        [Tooltip("문구 번호. board.filler.<번호> 아래에 제목 / 본문 / 작성자 / 댓글이 있다.")]
        public string fillerKey;

        public int views;

        [Tooltip("이 글이 괴담의 믿음에 보태는 몫(%). 0 이면 일상 게시판 글이다.")]
        public int belief;

        public int likes;
        public int dislikes;

        [Tooltip("처음부터 달려 있는 댓글을 쓴 사람들. nick_5|op|anon 처럼 세로줄로 잇는다.")]
        public string commenters;

        [Tooltip("이 글이 실어 나르는 괴담. 비워 두면 어느 사건과도 묶이지 않는다.")]
        public string legendId;
    }

    /// <summary>
    /// 괴담넷이 시간에 따라 살아 움직이는 정도.
    ///
    /// 게임 안 시계가 흐른 만큼 글마다 조회수와 좋아요 / 싫어요와 댓글이 붙는다.
    /// 믿음 몫이 높은 글일수록 사람이 몰리고 믿는 쪽 댓글이 달린다.
    /// 믿음이 낮은 글에도 반응은 멈추지 않는다. 의심하는 쪽 댓글이 붙는다.
    ///
    /// 한 시간에 몇이 붙는지는 전부 여기서 정한다. 코드에는 숫자를 두지 않는다.
    /// 시간을 세는 쪽은 BoardActivityDirector 다.
    /// </summary>
    [CreateAssetMenu(fileName = "board_activity", menuName = "Game Data/Board Activity", order = 9)]
    public class BoardActivitySO : ScriptableObject
    {
        [Header("조회수 (한 시간에)")]
        [SerializeField] private float _viewsPerHour = 4f;
        [Tooltip("믿음 몫 1%마다 더 붙는 조회수.")]
        [SerializeField] private float _viewsPerHourPerBelief = 0.5f;
        [Tooltip("인기글은 이만큼 곱해 더 읽힌다.")]
        [SerializeField] private float _hotViewMultiplier = 2.5f;

        [Header("좋아요 / 싫어요 (한 시간에)")]
        [SerializeField] private float _likesPerHour = 0.3f;
        [SerializeField] private float _likesPerHourPerBelief = 0.03f;
        [SerializeField] private float _dislikesPerHour = 0.25f;
        [Tooltip("믿음 몫이 기준보다 낮은 만큼 1%마다 더 붙는 싫어요.")]
        [SerializeField] private float _dislikesPerHourPerDoubt = 0.02f;

        [Header("댓글 (한 시간에)")]
        [SerializeField] private float _commentsPerHour = 0.2f;
        [SerializeField] private float _commentsPerHourPerBelief = 0.015f;
        [Tooltip("글 하나에 시간이 흘러 새로 붙는 댓글의 최대 수. 같은 말이 되풀이되지 않게 막는다.")]
        [SerializeField] private int _maxAddedComments = 4;

        [Tooltip("이 몫(%) 이상이면 믿는 쪽 댓글이, 아래면 의심하는 쪽 댓글이 붙는다. 싫어요 계산의 기준이기도 하다.")]
        [SerializeField] private int _believerThreshold = 25;

        [Header("새로 붙는 댓글")]
        [SerializeField] private List<PostWritingReaction> _believerComments = new List<PostWritingReaction>();
        [SerializeField] private List<PostWritingReaction> _skepticComments = new List<PostWritingReaction>();
        [Tooltip("괴담과 무관한 일상 글에 붙는 댓글.")]
        [SerializeField] private List<PostWritingReaction> _dailyComments = new List<PostWritingReaction>();
        [Tooltip("플레이어가 올린 글에 붙는 댓글.")]
        [SerializeField] private List<PostWritingReaction> _playerPostComments = new List<PostWritingReaction>();

        [Header("새로 올라오는 글")]
        [SerializeField] private List<ScheduledBoardPost> _scheduledPosts = new List<ScheduledBoardPost>();

        public float ViewsPerHour => _viewsPerHour;
        public float ViewsPerHourPerBelief => _viewsPerHourPerBelief;
        public float HotViewMultiplier => _hotViewMultiplier;
        public float LikesPerHour => _likesPerHour;
        public float LikesPerHourPerBelief => _likesPerHourPerBelief;
        public float DislikesPerHour => _dislikesPerHour;
        public float DislikesPerHourPerDoubt => _dislikesPerHourPerDoubt;
        public float CommentsPerHour => _commentsPerHour;
        public float CommentsPerHourPerBelief => _commentsPerHourPerBelief;
        public int MaxAddedComments => _maxAddedComments;
        public int BelieverThreshold => _believerThreshold;

        public IReadOnlyList<PostWritingReaction> BelieverComments => _believerComments;
        public IReadOnlyList<PostWritingReaction> SkepticComments => _skepticComments;
        public IReadOnlyList<PostWritingReaction> DailyComments => _dailyComments;
        public IReadOnlyList<PostWritingReaction> PlayerPostComments => _playerPostComments;
        public IReadOnlyList<ScheduledBoardPost> ScheduledPosts => _scheduledPosts;
    }
}
