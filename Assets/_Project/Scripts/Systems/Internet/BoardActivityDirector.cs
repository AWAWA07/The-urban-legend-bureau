using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.Save;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 괴담넷이 시간에 따라 살아 움직이게 한다.
    ///
    /// 게임 안 벽시계(GameClock)가 흐른 분만큼만 반영한다. 매 프레임 무언가를 더하지 않는다.
    /// 시계가 01:00 에서 01:10 이 되면 10분 치의 조회수 / 좋아요 / 싫어요 / 댓글이 붙고,
    /// 그 사이에 올라오기로 한 글이 있으면 게시판에 걸린다.
    ///
    /// 게시판 목록은 기존 TutorialDirector 가 들고 있는 그 목록이다. 새 게시판을 만들지 않는다.
    /// 얼마나 붙는지는 BoardActivitySO 에 있다.
    ///
    /// 어디까지 반영했는지는 저장본(global.boardActivityMinute)에 적는다.
    /// 불러온 뒤 같은 시간만큼 또 늘어나지 않는다.
    /// </summary>
    public class BoardActivityDirector : MonoBehaviour
    {
        [SerializeField] private BoardActivitySO _data;
        [SerializeField] private TutorialDirector _tutorial;
        [SerializeField] private CommunityPageScreen _communityScreen;

        [Tooltip("한 번에 반영하는 최대 분. 시계가 크게 뛰어도 반응이 한꺼번에 폭증하지 않게 한다.")]
        [SerializeField] private int _maxStepMinutes = 240;

        private SaveService _save;

        /// <summary>마지막으로 반영한 시계의 흐른 분. 음수면 아직 세기 시작하지 않았다.</summary>
        private int _lastMinute = -1;

        /// <summary>반영하고 있는 게시판의 세대. 튜토리얼을 다시 돌려 게시판을 새로 지으면 달라진다.</summary>
        private int _generation = -1;

        /// <summary>글마다 1에 못 미치는 몫을 모아 둔다. 조회 / 좋아요 / 싫어요 / 댓글.</summary>
        private readonly Dictionary<CommunityBoardEntry, float[]> _carry = new Dictionary<CommunityBoardEntry, float[]>();

        /// <summary>글마다 시간이 흘러 새로 붙은 댓글 수.</summary>
        private readonly Dictionary<CommunityBoardEntry, int> _added = new Dictionary<CommunityBoardEntry, int>();

        private readonly HashSet<string> _posted = new HashSet<string>();

        private void Start()
        {
            ServiceRegistry.TryGet(out _save);
        }

        private void Update()
        {
            if (_data == null || _tutorial == null) return;

            // 게시판을 새로 지었으면 처음부터 센다. 저장본에 적힌 값이 있으면 거기서 잇는다.
            if (_generation != _tutorial.BoardGeneration)
            {
                _generation = _tutorial.BoardGeneration;
                _carry.Clear();
                _added.Clear();
                _posted.Clear();
                _lastMinute = GameClock.ElapsedMinutes;
                MarkPostedUpTo(_lastMinute);
                return;
            }

            int now = GameClock.ElapsedMinutes;
            if (now == _lastMinute) return;

            // 시계를 뒤로 돌렸다. 거꾸로 줄이지는 않고 여기서부터 다시 센다.
            if (now < _lastMinute)
            {
                _lastMinute = now;
                return;
            }

            int from = _lastMinute;
            int minutes = Mathf.Min(now - from, _maxStepMinutes);
            _lastMinute = now;

            Apply(from, now, minutes);
            Remember(now);
        }

        /// <summary>
        /// 저장본을 불러왔을 때 부른다. 적힌 곳까지는 이미 반영한 것으로 친다.
        /// 게시판을 새로 짓는 경우(튜토리얼 시작)는 세대가 바뀌어 처음부터 센다.
        /// </summary>
        public void RestoreFromSave()
        {
            if (_save == null && !ServiceRegistry.TryGet(out _save)) return;
            int saved = _save.Current != null && _save.Current.global != null ? _save.Current.global.boardActivityMinute : -1;
            if (saved >= 0) _lastMinute = saved;
        }

        /// <summary>이미 지난 시각에 올라오기로 한 글은 다시 올리지 않는다.</summary>
        private void MarkPostedUpTo(int minute)
        {
            foreach (var post in _data.ScheduledPosts)
            {
                if (post != null && post.atElapsedMinute <= minute) _posted.Add(post.fillerKey);
            }
        }

        private void Remember(int now)
        {
            if (_save == null || _save.Current == null || _save.Current.global == null) return;

            _save.Current.global.boardActivityMinute = now;
            _save.Current.global.clockElapsedMinutes = now;
            _save.MarkDirty();
        }

        // ------------------------------------------------------------- 반영

        private void Apply(int from, int to, int minutes)
        {
            bool changed = false;

            // 그 사이에 올라오기로 한 글.
            foreach (var post in _data.ScheduledPosts)
            {
                if (post == null || string.IsNullOrEmpty(post.fillerKey) || _posted.Contains(post.fillerKey)) continue;
                if (post.atElapsedMinute <= from || post.atElapsedMinute > to) continue;

                _posted.Add(post.fillerKey);
                if (_tutorial.AddScheduledPost(post) != null)
                {
                    changed = true;
                    Debug.Log($"[BoardActivity] 새 글이 올라왔다 | {post.fillerKey} ({post.atElapsedMinute}분)");
                }
            }

            float hours = minutes / 60f;
            foreach (var entry in _tutorial.BoardEntries)
            {
                if (entry == null) continue;
                changed |= Grow(entry, hours);
            }

            if (changed) RefreshScreen();
        }

        /// <summary>
        /// 글 하나에 흐른 시간만큼 반응을 붙인다.
        /// 믿음 몫이 높을수록 많이 읽히고 좋아요와 댓글이 붙는다. 낮으면 싫어요가 조금 더 붙는다.
        /// </summary>
        private bool Grow(CommunityBoardEntry entry, float hours)
        {
            if (!_carry.TryGetValue(entry, out var carry))
            {
                carry = new float[4];
                _carry[entry] = carry;
            }

            // 플레이어가 올린 글은 제 몫을 들고 있지 않다. 그 괴담의 지금 믿음도로 읽힌다.
            int belief = entry.IsPlayerPost ? 0 : entry.BeliefPercent;
            float doubt = Mathf.Max(0, _data.BelieverThreshold - belief);

            float views = (_data.ViewsPerHour + _data.ViewsPerHourPerBelief * belief) * (entry.IsHot ? _data.HotViewMultiplier : 1f);
            float likes = _data.LikesPerHour + _data.LikesPerHourPerBelief * belief;
            float dislikes = _data.DislikesPerHour + _data.DislikesPerHourPerDoubt * doubt;
            float comments = _data.CommentsPerHour + _data.CommentsPerHourPerBelief * belief;

            int addViews = Take(ref carry[0], views * hours);
            int addLikes = Take(ref carry[1], likes * hours);
            int addDislikes = Take(ref carry[2], dislikes * hours);
            int addComments = Take(ref carry[3], comments * hours);

            entry.Views += addViews;
            entry.Likes += addLikes;
            entry.Dislikes += addDislikes;

            for (int i = 0; i < addComments; i++) AddComment(entry, belief);

            return addViews + addLikes + addDislikes + addComments > 0;
        }

        private static int Take(ref float carry, float amount)
        {
            carry += amount;
            int whole = Mathf.FloorToInt(carry);
            carry -= whole;
            return whole;
        }

        /// <summary>
        /// 댓글 하나를 붙인다. 글의 성격에 맞는 쪽에서 아직 이 글에 없는 말을 고른다.
        /// 한 글에 붙는 수에는 한도가 있다. 같은 사람이 같은 말을 되풀이하면 사람이 모인 곳처럼 보이지 않는다.
        /// </summary>
        private void AddComment(CommunityBoardEntry entry, int belief)
        {
            if (entry.Comments == null) return;

            _added.TryGetValue(entry, out int count);
            if (count >= _data.MaxAddedComments) return;

            var pool = entry.IsPlayerPost ? _data.PlayerPostComments
                : belief <= 0 ? _data.DailyComments
                : belief >= _data.BelieverThreshold ? _data.BelieverComments
                : _data.SkepticComments;
            if (pool == null || pool.Count == 0) return;

            // 글마다 고르는 순서를 달리한다. 모든 글에 같은 댓글이 같은 순서로 붙지 않게 한다.
            int start = Mathf.Abs((entry.TitleTextId ?? string.Empty).GetHashCode()) + count;
            for (int i = 0; i < pool.Count; i++)
            {
                var pick = pool[(start + i) % pool.Count];
                if (pick == null || HasComment(entry, pick.bodyTextId)) continue;

                entry.Comments.Add(new CommunityComment { AuthorTextId = pick.authorTextId, BodyTextId = pick.bodyTextId });
                _added[entry] = count + 1;
                return;
            }
        }

        private static bool HasComment(CommunityBoardEntry entry, string bodyTextId)
        {
            foreach (var c in entry.Comments)
            {
                if (c != null && c.BodyTextId == bodyTextId) return true;
            }
            return false;
        }

        /// <summary>괴담넷이 떠 있으면 바뀐 숫자를 바로 보여 준다. 열려 있는 글은 스크롤을 건드리지 않는다.</summary>
        private void RefreshScreen()
        {
            if (_communityScreen == null || !_communityScreen.IsOpen) return;

            if (_communityScreen.IsShowingBoard) _communityScreen.Refresh();
            else _communityScreen.RefreshCounts();
        }
    }
}
