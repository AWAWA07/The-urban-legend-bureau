namespace UrbanLegendBureau.Data
{
    /// <summary>
    /// 괴담 위험 등급. 낮을수록 안전하다.
    ///
    /// 화면에 보이는 이름은 코드가 아니라 Localization 테이블에서 가져온다.
    /// (legend.risk.* ID) 기획 명칭은 그 테이블에 그대로 들어 있다.
    /// </summary>
    public enum LegendRiskLevel
    {
        /// <summary>관측급 - 목격담만 존재한다. 실체가 없다.</summary>
        Observation = 1,

        /// <summary>전파급 - 소문이 퍼지기 시작했다.</summary>
        Propagation = 2,

        /// <summary>침식급 - 현실에 간섭이 시작됐다.</summary>
        Erosion = 3,

        /// <summary>현현급 - 실체가 나타났다.</summary>
        Manifestation = 4,

        /// <summary>멸망급 - 통제 불능.</summary>
        Annihilation = 5
    }

    public static class LegendRiskLevelExtensions
    {
        /// <summary>등급 이름의 Localization String ID.</summary>
        public static string ToTextId(this LegendRiskLevel level)
        {
            switch (level)
            {
                case LegendRiskLevel.Observation: return "legend.risk.observation";
                case LegendRiskLevel.Propagation: return "legend.risk.propagation";
                case LegendRiskLevel.Erosion: return "legend.risk.erosion";
                case LegendRiskLevel.Manifestation: return "legend.risk.manifestation";
                case LegendRiskLevel.Annihilation: return "legend.risk.annihilation";
                default: return "legend.risk.observation";
            }
        }
    }
}
