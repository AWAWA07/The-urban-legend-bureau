namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 확산도를 사람이 읽을 수 있는 단계로 나눈 것.
    ///
    /// 경계는 SpreadService.GetSpreadLevel 한 곳에서만 정한다.
    ///   0 이상 25 미만  Stable
    ///   25 이상 50 미만 Spreading
    ///   50 이상 75 미만 Dangerous
    ///   75 이상 100 이하 Critical
    /// </summary>
    public enum SpreadLevel
    {
        /// <summary>안정. 아직 소문 수준이다.</summary>
        Stable = 0,

        /// <summary>확산. 퍼지기 시작했다.</summary>
        Spreading = 1,

        /// <summary>위험. 현실에 영향을 미칠 수 있다.</summary>
        Dangerous = 2,

        /// <summary>폭주. 통제를 벗어나고 있다.</summary>
        Critical = 3
    }

    public static class SpreadLevelExtensions
    {
        /// <summary>단계 이름의 Localization String ID.</summary>
        public static string ToTextId(this SpreadLevel level)
        {
            switch (level)
            {
                case SpreadLevel.Stable: return "spread.level.stable";
                case SpreadLevel.Spreading: return "spread.level.spreading";
                case SpreadLevel.Dangerous: return "spread.level.dangerous";
                case SpreadLevel.Critical: return "spread.level.critical";
                default: return "spread.level.stable";
            }
        }

        /// <summary>현장 진입 전에 경고할 단계인가.</summary>
        public static bool NeedsFieldWarning(this SpreadLevel level)
        {
            return level >= SpreadLevel.Dangerous;
        }

        /// <summary>현장 진입 경고 문구의 String ID. 경고가 필요 없으면 null.</summary>
        public static string ToWarningTextId(this SpreadLevel level)
        {
            switch (level)
            {
                case SpreadLevel.Dangerous: return "spread.warning.dangerous";
                case SpreadLevel.Critical: return "spread.warning.critical";
                default: return null;
            }
        }
    }
}
