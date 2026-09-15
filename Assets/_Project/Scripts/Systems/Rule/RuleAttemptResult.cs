namespace UrbanLegendBureau.Systems
{
    /// <summary>추론을 시도할 수 없었던 이유.</summary>
    public enum RuleAttemptFailure
    {
        None = 0,

        /// <summary>그런 규칙이 없다.</summary>
        NotFound = 1,

        /// <summary>저장 데이터가 없다.</summary>
        NoSaveData = 2,

        /// <summary>근거가 되는 단서가 부족하다.</summary>
        MissingClue = 3
    }

    /// <summary>
    /// 플레이어가 규칙 하나를 "이것이 맞다"고 골랐을 때의 판정 결과.
    ///
    /// 정답 여부(IsCorrect)는 고르고 난 뒤에야 알 수 있는 값이다.
    /// 후보 목록을 만들 때는 이 값을 쓰지 않는다. 쓰면 답이 새어 나간다.
    /// </summary>
    public readonly struct RuleAttemptResult
    {
        /// <summary>추론 시도가 성립했는가. 근거가 부족하면 false.</summary>
        public bool Success { get; }

        /// <summary>올바른 규칙이었는가. 시도가 성립했을 때만 의미가 있다.</summary>
        public bool IsCorrect { get; }

        /// <summary>이번에 새로 기록했는가.</summary>
        public bool NewlyDeduced { get; }

        /// <summary>이미 확인했던 규칙인가. 이 경우 중복 기록하지 않는다.</summary>
        public bool AlreadyDeduced { get; }

        public string RuleId { get; }

        public RuleAttemptFailure Failure { get; }

        /// <summary>화면에 보여줄 결과 문구의 String ID.</summary>
        public string MessageTextId { get; }

        public RuleAttemptResult(bool success, bool isCorrect, bool newlyDeduced, bool alreadyDeduced,
            string ruleId, RuleAttemptFailure failure, string messageTextId)
        {
            Success = success;
            IsCorrect = isCorrect;
            NewlyDeduced = newlyDeduced;
            AlreadyDeduced = alreadyDeduced;
            RuleId = ruleId;
            Failure = failure;
            MessageTextId = messageTextId;
        }

        public static RuleAttemptResult Blocked(string ruleId, RuleAttemptFailure failure, string messageTextId)
        {
            return new RuleAttemptResult(false, false, false, false, ruleId, failure, messageTextId);
        }
    }
}
