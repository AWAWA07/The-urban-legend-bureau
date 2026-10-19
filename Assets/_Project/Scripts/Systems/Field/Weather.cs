namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 그날 비가 오는가. 창밖 빗줄기(WindowRain)와 숙소 빗소리(RoomNight)가 이것을 본다.
    ///
    ///   튜토리얼 첫날 밤   반드시 온다.
    ///   본편              날마다 정해진다. 같은 날은 몇 번을 불러와도 같고, 날짜가 바뀌면 새로 정해진다.
    /// 무작위처럼 보이지만 날짜에서 계산한 값이라 저장할 필요가 없다.
    /// </summary>
    public static class Weather
    {
        /// <summary>본편 중인가. 본편 진행을 맡은 쪽(WorkdayDirector)이 건다. 비어 있으면 첫날 밤으로 본다.</summary>
        public static System.Func<bool> InMain;

        /// <summary>본편에서 비가 올 확률(%).</summary>
        public const int RainChance = 45;

        public static bool IsRaining
        {
            get
            {
                if (InMain == null || !InMain()) return true;
                return RainsOn(WorkdayDirector.CalendarDay);
            }
        }

        /// <summary>그 날짜에 비가 오는가. 날짜를 뒤섞어 고르게 흩어지게 한다.</summary>
        public static bool RainsOn(int day)
        {
            unchecked
            {
                uint h = (uint)day * 2654435761u;
                h ^= h >> 15;
                h *= 0x2C1B3C6Du;
                h ^= h >> 12;
                return h % 100u < (uint)RainChance;
            }
        }
    }
}
