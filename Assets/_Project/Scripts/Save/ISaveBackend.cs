namespace UrbanLegendBureau.Save
{
    /// <summary>
    /// 저장 매체 추상화. 플랫폼 차이는 이 인터페이스의 구현체 안에서만 다룬다.
    /// 게임 코드는 물론이고 SaveService조차 어떤 매체인지 알 필요가 없다.
    /// </summary>
    public interface ISaveBackend
    {
        /// <summary>진단 로그용 이름.</summary>
        string Name { get; }

        /// <summary>사람이 확인할 수 있는 저장 위치(경로 또는 키 이름).</summary>
        string DescribeLocation(string key);

        bool Exists(string key);

        /// <summary>없으면 null을 돌려준다. 예외를 던지지 않는다.</summary>
        string Read(string key);

        bool Write(string key, string json);

        /// <summary>매체에 실제로 밀어 넣는다. WebGL의 IndexedDB 동기화가 여기서 일어난다.</summary>
        void Flush();

        bool Delete(string key);
    }
}
