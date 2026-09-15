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
        Seal = 4,

        // --- 16단계: 플레이어가 직접 고르는 조사 행동 ---
        // 값은 저장에 들어가지 않지만 데이터 에셋이 가리키므로 한 번 정한 숫자를 바꾸지 않는다.

        /// <summary>인터넷 검색. 글을 여는 것이 아니라 정보만 훑는다.</summary>
        InternetSearch = 5,

        /// <summary>물건/증거 조사. 현장의 특정 대상을 들여다본다.</summary>
        EvidenceInspect = 6,

        /// <summary>영적 흔적 조사. 가장 크게 건드리는 만큼 더 퍼진다.</summary>
        SpiritTrace = 7
    }
}
