namespace UrbanLegendBureau.Data
{
    /// <summary>
    /// 조사 행동을 어디에서 고를 수 있는가.
    ///
    /// 기본값이 Both인 이유: 이 값을 정하지 않은 기존 데이터가
    /// 예전처럼 사무실과 현장 양쪽에 그대로 나오게 하기 위해서다.
    /// 값을 새로 넣지 않아도 동작이 달라지지 않는다.
    /// </summary>
    public enum ActionUsage
    {
        /// <summary>어디서든 고를 수 있다. 사용 위치를 지정하지 않은 데이터의 기본값.</summary>
        Both = 0,

        /// <summary>사무실에서만. 인터넷 검색처럼 현장에 가지 않아도 되는 조사.</summary>
        Office = 1,

        /// <summary>현장 지점에서만. 그 자리에 있어야 할 수 있는 조사.</summary>
        Field = 2
    }
}
