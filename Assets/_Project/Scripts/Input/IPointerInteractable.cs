namespace UrbanLegendBureau.InputSystemLayer
{
    /// <summary>
    /// 포인터로 조작 가능한 대상이 구현하는 최소 계약.
    /// 조사 오브젝트, 인터넷 게시글, 단서 카드 등이 나중에 이 인터페이스를 구현한다.
    /// 구현체는 마우스인지 터치인지 알 필요가 없다.
    /// </summary>
    public interface IPointerInteractable
    {
        /// <summary>포인터가 이 대상 위에서 눌린 순간.</summary>
        void PointerPressed(in PointerContext context);

        /// <summary>포인터가 떼어진 순간. 누른 대상과 뗀 대상이 다를 수 있으므로 구현 측에서 판단한다.</summary>
        void PointerReleased(in PointerContext context);

        /// <summary>누르고 있는 동안 매 프레임. 롱프레스 연출에 사용한다.</summary>
        void PointerHeld(in PointerContext context);
    }
}
