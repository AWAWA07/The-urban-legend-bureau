namespace UrbanLegendBureau.Data
{
    /// <summary>
    /// 고유 ID를 갖는 정적 게임 데이터.
    /// 검증 도구가 타입별로 코드를 중복하지 않기 위한 최소 계약이다.
    /// </summary>
    public interface IGameDataAsset
    {
        /// <summary>SaveData에 기록되는 안정적인 문자열 ID. 한 번 정하면 바꾸지 않는다.</summary>
        string Id { get; }
    }
}
