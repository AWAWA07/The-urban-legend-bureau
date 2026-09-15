namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 조사 행동 한 번의 결과.
    ///
    /// "3회째 조사 / 30분 경과 / 확산 20% -> 21.5%" 같은 안내를 만들 수 있도록
    /// 시간과 확산 변화를 함께 돌려준다. SpreadChangeResult와 같은 이유로 구조체다.
    /// </summary>
    public readonly struct InvestigationTickResult
    {
        /// <summary>행동이 실제로 기록됐는가. 실패하면 아래 값은 현재 상태 그대로다.</summary>
        public bool Registered { get; }

        /// <summary>이 사건에서 지금까지 한 조사 행동 횟수.</summary>
        public int ActionCount { get; }

        /// <summary>이 사건의 경과 시간(분).</summary>
        public int ElapsedMinutes { get; }

        /// <summary>이번 행동으로 생긴 확산 변화. 확산이 없는 행동이면 Changed가 false다.</summary>
        public SpreadChangeResult Spread { get; }

        public InvestigationTickResult(bool registered, int actionCount, int elapsedMinutes, SpreadChangeResult spread)
        {
            Registered = registered;
            ActionCount = actionCount;
            ElapsedMinutes = elapsedMinutes;
            Spread = spread;
        }

        /// <summary>기록하지 못했을 때. 현재 값만 담아 돌려준다.</summary>
        public static InvestigationTickResult Failed(int actionCount, int elapsedMinutes)
        {
            return new InvestigationTickResult(false, actionCount, elapsedMinutes, default);
        }
    }
}
