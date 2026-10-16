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

        /// <summary>
        /// 믿음도(%)에서 위험등급을 정한다. 관측 1~10 / 전파 11~30 / 침식 31~50 / 현현 51~84 / 멸망 85~99.
        /// 85 는 멸망급으로 본다. 100 은 ???급(게임 오버)이라 IsBeyond 로 따로 묻는다. 0 은 괴담과 무관한 글이다.
        /// </summary>
        public static LegendRiskLevel FromBelief(float belief)
        {
            if (belief >= 85f) return LegendRiskLevel.Annihilation;
            if (belief >= 51f) return LegendRiskLevel.Manifestation;
            if (belief >= 31f) return LegendRiskLevel.Erosion;
            if (belief >= 11f) return LegendRiskLevel.Propagation;
            return LegendRiskLevel.Observation;
        }

        /// <summary>???급(100%). 이미 끝난 뒤를 가리키는 수치다.</summary>
        public static bool IsBeyond(float belief) => belief >= 100f;
    }
}
