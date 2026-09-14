using UnityEngine;

namespace UrbanLegendBureau.InputSystemLayer
{
    /// <summary>
    /// 포인터 콜백에 함께 전달되는 스냅샷.
    /// 수신 측이 PointerInput을 다시 조회하지 않아도 되게 한다.
    /// </summary>
    public readonly struct PointerContext
    {
        public readonly Vector2 ScreenPosition;
        public readonly Vector2 WorldPosition;
        public readonly float HeldDuration;
        public readonly bool IsOverUI;

        public PointerContext(Vector2 screenPosition, Vector2 worldPosition, float heldDuration, bool isOverUI)
        {
            ScreenPosition = screenPosition;
            WorldPosition = worldPosition;
            HeldDuration = heldDuration;
            IsOverUI = isOverUI;
        }
    }
}
