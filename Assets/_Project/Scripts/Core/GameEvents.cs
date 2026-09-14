namespace UrbanLegendBureau.Core
{
    /// <summary>
    /// 1단계에서 필요한 코어 이벤트만 정의한다.
    /// 시스템이 추가될 때마다 해당 시스템의 이벤트를 이 파일에 이어서 선언한다.
    /// </summary>

    /// <summary>모든 코어 서비스 초기화가 끝났을 때.</summary>
    public struct GameBootCompletedEvent : IGameEvent { }

    /// <summary>표시 언어가 바뀌었을 때. UI는 이걸 받아 텍스트를 다시 채운다.</summary>
    public struct LanguageChangedEvent : IGameEvent
    {
        public readonly string LanguageCode;

        public LanguageChangedEvent(string languageCode)
        {
            LanguageCode = languageCode;
        }
    }
}
