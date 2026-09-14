namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 봉인 시도의 결과.
    ///
    /// 검열과 마찬가지로 bool 하나로 끝내지 않는다.
    /// 실패 이유마다 플레이어에게 보여줄 안내가 다르고,
    /// 이후 단계에서 봉인 실패 연출이 붙을 자리이기도 하다.
    /// </summary>
    public enum SealResult
    {
        /// <summary>봉인 성공. isSealed가 true가 됐다.</summary>
        Success = 0,

        /// <summary>이미 봉인된 괴담이다. 아무 일도 일어나지 않았다.</summary>
        AlreadySealed = 1,

        /// <summary>그런 legendId의 괴담이 없다. 데이터 문제다.</summary>
        LegendNotFound = 2,

        /// <summary>이 괴담의 규칙을 하나도 알아내지 못했다. 봉인할 수 없다.</summary>
        RuleNotFound = 3,

        /// <summary>저장 데이터가 없어 판단할 수 없다.</summary>
        NoSaveData = 4
    }

    public static class SealResultExtensions
    {
        public static bool IsSuccess(this SealResult result) => result == SealResult.Success;

        /// <summary>결과 안내 문구의 Localization String ID.</summary>
        public static string ToTextId(this SealResult result)
        {
            switch (result)
            {
                case SealResult.Success: return "ui.seal.success";
                case SealResult.AlreadySealed: return "ui.seal.already";
                case SealResult.RuleNotFound: return "ui.seal.no_rule";
                default: return "ui.seal.failed";
            }
        }
    }
}
