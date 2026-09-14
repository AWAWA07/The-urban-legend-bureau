namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 사건 진행 단계.
    /// SaveData.currentStepIndex 에 int로 저장된다. SaveData 구조는 바꾸지 않는다.
    /// 값은 저장 파일과 직결되므로 한 번 정한 숫자를 바꾸지 않는다.
    /// </summary>
    public enum CaseStep
    {
        /// <summary>사건이 열렸다.</summary>
        Started = 0,

        /// <summary>괴담 정보 확인 중.</summary>
        LegendBriefing = 1,

        /// <summary>인터넷 조사 중.</summary>
        InternetResearch = 2,

        /// <summary>현장 조사 중.</summary>
        FieldInvestigation = 3,

        /// <summary>단서를 얻었다.</summary>
        ClueAcquired = 4,

        /// <summary>괴담의 규칙을 추론했다.</summary>
        RuleDeduction = 5,

        /// <summary>퇴마/봉인 단계.</summary>
        Exorcism = 6,

        /// <summary>사건 종료.</summary>
        Completed = 7
    }
}
