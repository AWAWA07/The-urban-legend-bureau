namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 사건 시간을 진행시키는 조사 행동의 종류.
    ///
    /// 행동이 늘어나면 여기에 항목을 추가하고
    /// InvestigationTimeService의 표에 확산량만 적어 주면 된다.
    /// 호출 측(CaseDirector)은 어떤 행동을 했는지만 알리면 된다.
    /// </summary>
    public enum InvestigationAction
    {
        /// <summary>게시글 열람. 이미 본 글을 다시 열어도 시간은 흐른다.</summary>
        InternetView = 0,

        /// <summary>게시글 검열. 확산 증감은 기존 검열 규칙이 맡는다.</summary>
        PageCensor = 1,

        /// <summary>현장 지점 조사. 단서가 없어도 시간은 흐른다.</summary>
        FieldSearch = 2,

        /// <summary>새 단서 확보. 현장 조사보다 조금 더 크게 움직인다.</summary>
        ClueFound = 3,

        /// <summary>봉인 시도. 확산 처리는 기존 봉인 규칙이 맡는다.</summary>
        Seal = 4
    }
}
