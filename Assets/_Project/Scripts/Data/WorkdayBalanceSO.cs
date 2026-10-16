using System;
using UnityEngine;

namespace UrbanLegendBureau.Data
{
    /// <summary>
    /// 본편의 하루 진행과 자연 확산 수치. 코드에 박지 않고 여기서 고친다.
    /// Resources/Balance/WorkdayBalance 에 하나 둔다. 없으면 이 클래스의 기본값을 쓴다.
    /// 수치는 처음 짜 본 시험값이다. 확정 수치가 아니다.
    /// </summary>
    [CreateAssetMenu(fileName = "WorkdayBalance", menuName = "Game Data/Workday Balance", order = 20)]
    public class WorkdayBalanceSO : ScriptableObject
    {
        [Header("근무 시간")]
        [Tooltip("출근 시각(시). 18 이면 오후 6시.")]
        [SerializeField] private int _shiftStartHour = 18;

        [Tooltip("퇴근 시각(시). 다음 날 이 시각까지 일한다. 3 이면 오전 3시.")]
        [SerializeField] private int _shiftEndHour = 3;

        [Tooltip("1일차(튜토리얼 날)의 달력 날짜. 화면에 'n월 n일'로 적을 때만 쓴다.")]
        [SerializeField] private int _startYear = 2026;
        [SerializeField] private int _startMonth = 10;
        [SerializeField] private int _startDay = 15;

        [Header("자연 확산 — 위험등급별 시간당 상승(%p)")]
        [Tooltip("근무 중. 관측 / 전파 / 침식 / 현현 / 멸망 순.")]
        [SerializeField] private float[] _riseWhileWorking = { 0.08f, 0.18f, 0.32f, 0.55f, 0.90f };

        [Tooltip("취침 중. 관측 / 전파 / 침식 / 현현 / 멸망 순.")]
        [SerializeField] private float[] _riseWhileSleeping = { 0.02f, 0.05f, 0.09f, 0.16f, 0.30f };

        [Tooltip("확산도(0~100)에 따른 상승 보정. 1 + 확산도/100 × 이 값을 곱한다. 0 이면 확산도를 보지 않는다.")]
        [SerializeField] private float _spreadBoost = 0.5f;

        [Header("동시 사건")]
        [Tooltip("해결한 고유 사건 수에 따른 동시 활성 상한. 앞에서부터 해결 0~4건, 5~9건, 10~14건, 15건 이상.")]
        [SerializeField] private int[] _maxActiveBySolved = { 2, 3, 3, 4 };

        [Header("게임 오버")]
        [Tooltip("개별 괴담 또는 전체 괴담 믿음도가 이 값(%)에 닿으면 게임 오버.")]
        [SerializeField] private int _gameOverBelief = 100;

        [Tooltip("처음 넘을 때 한 번 알리는 경고 단계(%).")]
        [SerializeField] private int[] _warningThresholds = { 50, 75, 85, 95, 99 };

        [Header("사건 해결")]
        [Tooltip("사건을 정식으로 해결하려면 그 괴담 믿음도가 이 값(%) 이하여야 한다. 사건 데이터에 따로 정한 목표가 없을 때 쓴다.")]
        [SerializeField] private int _defaultResolveBelief = 30;

        [Tooltip("본편에서 글이나 댓글 하나를 실제로 올릴 때 흐르는 시간(분). 올리기를 확정한 순간 한 번만 흐른다.")]
        [SerializeField] private int _postMinutes = 20;

        public int ShiftStartHour => _shiftStartHour;
        public int ShiftEndHour => _shiftEndHour;
        public DateTime StartDate => new DateTime(_startYear, _startMonth, _startDay);
        public float SpreadBoost => _spreadBoost;
        public int GameOverBelief => _gameOverBelief;
        public int[] WarningThresholds => _warningThresholds;
        public int DefaultResolveBelief => _defaultResolveBelief;
        public int PostMinutes => _postMinutes;

        /// <summary>등급(1 관측 ~ 5 멸망)과 상태에 맞는 시간당 상승(%p).</summary>
        public float RisePerHour(int grade, bool sleeping)
        {
            var table = sleeping ? _riseWhileSleeping : _riseWhileWorking;
            if (table == null || table.Length == 0) return 0f;
            int i = Mathf.Clamp(grade - 1, 0, table.Length - 1);
            return table[i];
        }

        public int MaxActiveCases(int solved)
        {
            if (_maxActiveBySolved == null || _maxActiveBySolved.Length == 0) return 2;
            int i = Mathf.Clamp(solved / 5, 0, _maxActiveBySolved.Length - 1);
            return _maxActiveBySolved[i];
        }

        private static WorkdayBalanceSO _loaded;

        /// <summary>Resources/Balance/WorkdayBalance 를 읽는다. 없으면 기본값으로 하나 만든다.</summary>
        public static WorkdayBalanceSO Load()
        {
            if (_loaded != null) return _loaded;
            _loaded = Resources.Load<WorkdayBalanceSO>("Balance/WorkdayBalance");
            if (_loaded == null) _loaded = CreateInstance<WorkdayBalanceSO>();
            return _loaded;
        }
    }
}
