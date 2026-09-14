using UnityEngine;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 확산도 변화 한 건의 전후 값.
    ///
    /// 기존 bool 반환 API는 그대로 두고, 변화를 보여줘야 하는 곳에서만 이걸 쓴다.
    /// "42% -> 30%" 같은 안내를 만들려면 호출 전 값을 따로 기억해야 했는데,
    /// 그 책임을 SpreadService 안으로 옮긴 것이다.
    /// </summary>
    public readonly struct SpreadChangeResult
    {
        /// <summary>변화가 실제로 적용됐는가. 잘못된 인자였으면 false.</summary>
        public readonly bool Applied;

        public readonly float PreviousRate;
        public readonly float CurrentRate;
        public readonly SpreadLevel PreviousLevel;
        public readonly SpreadLevel CurrentLevel;

        public SpreadChangeResult(bool applied, float previousRate, float currentRate,
            SpreadLevel previousLevel, SpreadLevel currentLevel)
        {
            Applied = applied;
            PreviousRate = previousRate;
            CurrentRate = currentRate;
            PreviousLevel = previousLevel;
            CurrentLevel = currentLevel;
        }

        /// <summary>실제 증감량. 상한/하한에 걸렸다면 요청량보다 작다.</summary>
        public float Delta => CurrentRate - PreviousRate;

        /// <summary>수치가 실제로 달라졌는가. Clamp에 걸려 제자리면 false.</summary>
        public bool Changed => Applied && !Mathf.Approximately(PreviousRate, CurrentRate);

        /// <summary>단계가 바뀌었는가. 경고 문구를 새로 띄울지 판단하는 데 쓴다.</summary>
        public bool LevelChanged => Applied && PreviousLevel != CurrentLevel;

        public static SpreadChangeResult Failed(float rate, SpreadLevel level)
        {
            return new SpreadChangeResult(false, rate, rate, level, level);
        }
    }
}
