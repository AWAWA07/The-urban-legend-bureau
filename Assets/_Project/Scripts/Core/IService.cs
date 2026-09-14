namespace UrbanLegendBureau.Core
{
    /// <summary>
    /// 모든 전역 서비스의 공통 계약.
    /// GameRoot가 등록 순서대로 Initialize, 역순으로 Shutdown을 호출한다.
    /// </summary>
    public interface IService
    {
        void Initialize();
        void Shutdown();
    }
}
