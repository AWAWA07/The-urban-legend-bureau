namespace UrbanLegendBureau.InputSystemLayer
{
    /// <summary>
    /// 마지막으로 사용된 입력 장치 종류.
    /// UI 힌트 문구("클릭" / "탭")나 커서 표시 여부를 여기에 반응시킨다.
    /// 게임 로직의 분기 조건으로는 쓰지 않는다.
    /// </summary>
    public enum InputMode
    {
        Pointer,   // 마우스 / 펜
        Touch,     // 터치스크린
        Gamepad
    }
}
