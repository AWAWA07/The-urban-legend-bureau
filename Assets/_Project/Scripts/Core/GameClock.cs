using UnityEngine;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.Core
{
    /// <summary>
    /// 그날 밤의 벽시계.
    ///
    /// 컴퓨터 작업 표시줄과 휴대폰 상단이 같은 시각을 보여야 한다. 그래서 한 곳에서만 센다.
    /// 켠 시각에서 실제로 흐른 만큼을 더한다. 실제 시간과 같은 속도로 간다.
    ///
    /// 사건 진행에 쓰는 조사 시간(InvestigationTimeService)과는 다른 것이다.
    /// 저쪽은 조사 행동 수로 가고, 이쪽은 그냥 벽에 걸린 시계다.
    /// </summary>
    public static class GameClock
    {
        private const string ClockTextId = "ui.desktop.clock";
        private const string AmTextId = "ui.desktop.am";
        private const string PmTextId = "ui.desktop.pm";

        private static int _startHour = 22;
        private static int _startMinute = 30;

        private static float _origin;
        private static bool _started;

        /// <summary>건너뛴 분. 화면 밖에서 흘러간 시간이 여기에 쌓인다.</summary>
        private static int _skipped;

        /// <summary>몇 시에 시작하는지 정한다. 컴퓨터 화면이 제 값으로 한 번 불러 준다.</summary>
        public static void Configure(int hour, int minute)
        {
            _startHour = hour;
            _startMinute = minute;
            EnsureStarted();
        }

        /// <summary>처음부터 다시 센다. 새 사건을 시작할 때 쓸 수 있다.</summary>
        public static void Restart()
        {
            _origin = Time.unscaledTime;
            _started = true;
            _skipped = 0;
        }

        /// <summary>
        /// 시계를 앞으로 돌린다. 화면 밖에서 흘러간 시간을 채워 넣을 때 쓴다.
        /// 조사 행동으로 사건 시간이 흐른 만큼이 그렇다.
        ///
        /// 흐른 시각을 따로 쌓아 둔다. 시작점을 뒤로 미루는 식으로 하면
        /// 그 값이 음수가 되어 "아직 시작하지 않음"과 구별되지 않는다.
        /// </summary>
        public static void Skip(int minutes)
        {
            EnsureStarted();
            _skipped += minutes;
        }

        /// <summary>
        /// 그 시각이 될 때까지 시계를 앞으로 돌린다. 이미 지났으면 다음 날 그 시각으로 간다.
        /// 장면이 바뀌면서 시각이 정해져 있는 곳에 쓴다. 현장에 닿는 시각이 그렇다.
        ///
        /// 시작점을 옮기지 않고 앞으로만 돌린다. 게시판 글이 "시작에서 몇 분 전"으로 적혀 있어,
        /// 시작점을 옮기면 그 글들이 올라온 시각까지 통째로 밀린다.
        /// </summary>
        public static void SetTo(int hour, int minute)
        {
            EnsureStarted();

            int target = hour * 60 + minute;
            int now = (_startHour * 60 + _startMinute + ElapsedMinutes) % 1440;

            int forward = target - now;
            if (forward < 0) forward += 1440;

            _skipped += forward;
        }

        /// <summary>시작한 뒤로 흐른 분. 이 값이 바뀔 때만 글자를 다시 쓰면 된다.</summary>
        public static int ElapsedMinutes
        {
            get
            {
                EnsureStarted();
                return _skipped + Mathf.FloorToInt((Time.unscaledTime - _origin) / 60f);
            }
        }

        private static void EnsureStarted()
        {
            if (_started) return;
            _origin = Time.unscaledTime;
            _started = true;
        }

        /// <summary>"오전 02:44" 꼴의 지금 시각.</summary>
        public static string Format(LocalizationService loc)
        {
            if (loc == null) return string.Empty;

            Split(out int hour12, out int minute, out bool morning);

            return loc.Get(ClockTextId,
                loc.Get(morning ? AmTextId : PmTextId),
                hour12.ToString("00"),
                minute.ToString("00"));
        }

        /// <summary>
        /// "02:44" 꼴. 오전/오후를 뗀다.
        /// 휴대폰 상태 줄처럼 좁은 자리에서 쓴다. 실제 휴대폰도 거기에는 시각만 적는다.
        /// </summary>
        public static string FormatShort()
        {
            Split(out int hour12, out int minute, out _);
            return hour12.ToString("00") + ":" + minute.ToString("00");
        }

        /// <summary>
        /// 지금에서 minutesAgo 만큼 거슬러 올라간 때가 며칠 전 몇 시인가.
        /// 게시판이 "어제 01:33" 같은 것을 적을 때 쓴다. 24시간 표기 그대로 돌려준다.
        /// </summary>
        public static void SplitPast(int minutesAgo, out int days, out int hour24, out int minute)
        {
            int nowMinutes = _startHour * 60 + _startMinute + ElapsedMinutes;
            int then = nowMinutes - Mathf.Max(0, minutesAgo);

            // 자정을 몇 번 넘었는가. 오늘 안이면 0, 어제면 1 이다.
            days = Mathf.FloorToInt(then / 1440f);
            days = nowMinutes / 1440 - days;

            int inDay = ((then % 1440) + 1440) % 1440;
            hour24 = inDay / 60;
            minute = inDay % 60;
        }

        private static void Split(out int hour12, out int minute, out bool morning)
        {
            int total = _startHour * 60 + _startMinute + ElapsedMinutes;
            int hour24 = (total / 60) % 24;

            minute = total % 60;
            morning = hour24 < 12;

            hour12 = hour24 % 12;
            if (hour12 == 0) hour12 = 12;
        }
    }
}
