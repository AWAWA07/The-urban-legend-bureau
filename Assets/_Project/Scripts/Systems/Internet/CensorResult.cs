namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 검열 시도의 결과.
    ///
    /// bool 하나로 끝내지 않는 이유: 다음 단계에서 확산 수치와 페널티를 붙일 때
    /// "실패했다"만으로는 확산을 얼마나 올릴지 판단할 수 없다.
    /// 특히 WrongTarget(검열하면 안 되는 글을 건드림)은 페널티 대상이다.
    /// </summary>
    public enum CensorResult
    {
        /// <summary>검열 성공. censoredPageIds에 추가됐다.</summary>
        Success = 0,

        /// <summary>이미 검열한 글이다. 아무 일도 일어나지 않았다.</summary>
        AlreadyCensored = 1,

        /// <summary>그런 pageId의 글이 없다. 데이터 문제다.</summary>
        NotFound = 2,

        /// <summary>검열할 수 없는 글을 검열하려 했다. 잘못된 검열 시도.</summary>
        WrongTarget = 3,

        /// <summary>저장 데이터가 없어 판단할 수 없다.</summary>
        NoSaveData = 4
    }

    public static class CensorResultExtensions
    {
        public static bool IsSuccess(this CensorResult result) => result == CensorResult.Success;

        /// <summary>다음 단계에서 페널티를 적용할 대상인가.</summary>
        public static bool IsWrongAttempt(this CensorResult result) => result == CensorResult.WrongTarget;

        /// <summary>상태 표시 문구의 Localization String ID.</summary>
        public static string ToTextId(this CensorResult result)
        {
            switch (result)
            {
                case CensorResult.Success: return "ui.net.censor_done";
                case CensorResult.AlreadyCensored: return "ui.net.censor_done";
                case CensorResult.WrongTarget: return "ui.net.censor_wrong";
                case CensorResult.NotFound: return "ui.net.page_missing";
                default: return "ui.net.page_missing";
            }
        }
    }
}
