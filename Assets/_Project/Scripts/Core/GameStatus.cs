namespace UrbanLegendBureau.Core
{
    /// <summary>
    /// 화면 구석에 늘 떠 있는 숫자들.
    ///
    /// 컴퓨터 작업 표시줄에도, 휴대폰 상태 줄에도 같은 전체 믿음도가 떠야 한다.
    /// 그래서 값은 한 곳에 두고, 아는 쪽이 넣어 준다. 판정은 BeliefService 가 한다.
    /// </summary>
    public static class GameStatus
    {
        /// <summary>전체 믿음도(%). 0~100.</summary>
        public static int Belief { get; private set; }

        public static void SetBelief(int percent)
        {
            Belief = percent;
        }
    }
}
