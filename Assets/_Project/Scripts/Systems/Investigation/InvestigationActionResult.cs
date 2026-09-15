namespace UrbanLegendBureau.Systems
{
    /// <summary>조사 행동을 실행할 수 없었던 이유.</summary>
    public enum InvestigationFailure
    {
        /// <summary>실패하지 않았다.</summary>
        None = 0,

        /// <summary>행동 데이터를 찾지 못했다.</summary>
        NotFound = 1,

        /// <summary>저장 데이터가 없다.</summary>
        NoSaveData = 2,

        /// <summary>필요한 단서를 아직 가지고 있지 않다.</summary>
        MissingClue = 3,

        /// <summary>사건이 아직 필요한 단계까지 가지 않았다.</summary>
        StepNotReached = 4
    }

    /// <summary>
    /// 조사 행동이 무엇을 남겼는가.
    ///
    /// "조건에 막혀 못 했다"와 "했지만 새 정보가 없었다"는 전혀 다른 일이다.
    /// 앞은 아무 자원도 쓰지 않았고, 뒤는 시간과 확산을 쓴 선택이다.
    /// </summary>
    public enum InvestigationOutcome
    {
        /// <summary>조건을 만족하지 못해 실행되지 않았다. 시간/확산/단서 변화 없음.</summary>
        Blocked = 0,

        /// <summary>새 단서를 얻었다.</summary>
        NewClue = 1,

        /// <summary>이미 알고 있던 내용이었다. 시간과 확산은 썼다.</summary>
        AlreadyKnown = 2,

        /// <summary>단서를 주지 않는 조사였다. 시간과 확산은 썼다.</summary>
        NoInformation = 3
    }

    /// <summary>
    /// 조사 행동 한 번의 결과.
    ///
    /// bool 하나로 돌려주면 화면이 "무엇이 일어났는지"를 다시 계산해야 한다.
    /// 시간/확산/단서/문구를 모두 담아 돌려주어 화면은 표시만 하면 되게 한다.
    /// </summary>
    public readonly struct InvestigationActionResult
    {
        public bool Success { get; }

        /// <summary>실패 이유. 성공이면 None.</summary>
        public InvestigationFailure Failure { get; }

        public string ActionId { get; }

        /// <summary>15단계 기준의 행동 종류.</summary>
        public InvestigationAction Action { get; }

        /// <summary>이번 행동으로 흐른 사건 시간(분). 실패하면 0.</summary>
        public int MinutesAdded { get; }

        /// <summary>실행 후의 조사 행동 횟수.</summary>
        public int ActionCount { get; }

        /// <summary>실행 후의 사건 경과 시간(분).</summary>
        public int ElapsedMinutes { get; }

        /// <summary>확산 변화. 확산이 없는 행동이면 Changed가 false.</summary>
        public SpreadChangeResult Spread { get; }

        /// <summary>이번 행동으로 얻은 단서 ID. 없거나 이미 가지고 있었다면 null.</summary>
        public string AcquiredClueId { get; }

        /// <summary>화면에 보여줄 결과 문구의 String ID.</summary>
        public string MessageTextId { get; }

        /// <summary>이번 조사가 무엇을 남겼는가.</summary>
        public InvestigationOutcome Outcome { get; }

        /// <summary>이번 조사로 올라간 확산량. 변화가 없으면 0.</summary>
        public float SpreadAdded => Spread.Changed ? Spread.CurrentRate - Spread.PreviousRate : 0f;

        public InvestigationActionResult(bool success, InvestigationFailure failure, string actionId,
            InvestigationAction action, int minutesAdded, int actionCount, int elapsedMinutes,
            SpreadChangeResult spread, string acquiredClueId, string messageTextId,
            InvestigationOutcome outcome = InvestigationOutcome.NoInformation)
        {
            Outcome = outcome;
            Success = success;
            Failure = failure;
            ActionId = actionId;
            Action = action;
            MinutesAdded = minutesAdded;
            ActionCount = actionCount;
            ElapsedMinutes = elapsedMinutes;
            Spread = spread;
            AcquiredClueId = acquiredClueId;
            MessageTextId = messageTextId;
        }

        /// <summary>새 단서를 얻었는가.</summary>
        public bool GotNewClue => !string.IsNullOrEmpty(AcquiredClueId);

        /// <summary>조건을 만족하지 못해 실행되지 않았다. 시간도 확산도 움직이지 않았다.</summary>
        public static InvestigationActionResult Blocked(string actionId, InvestigationFailure failure,
            int actionCount, int elapsedMinutes, string messageTextId)
        {
            return new InvestigationActionResult(false, failure, actionId, default, 0,
                actionCount, elapsedMinutes, default, null, messageTextId, InvestigationOutcome.Blocked);
        }
    }
}
