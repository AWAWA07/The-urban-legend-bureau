namespace UrbanLegendBureau.Core
{
    /// <summary>
    /// 매 프레임 갱신이 필요한 서비스가 구현한다.
    /// GameRoot가 등록 순서대로 Tick을 호출한다. 서비스마다 MonoBehaviour를 만들지 않기 위한 장치.
    /// </summary>
    public interface ITickable
    {
        void Tick(float deltaTime);
    }
}
