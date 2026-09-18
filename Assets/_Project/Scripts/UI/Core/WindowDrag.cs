using UnityEngine;
using UnityEngine.EventSystems;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 창을 잡아 끌어 옮긴다. 제목 줄에 붙인다.
    ///
    /// 실제 창이 그렇듯 제목 줄만 잡힌다. 안쪽을 끌면 글이 잡히지 회색 창이 끌리지 않는다.
    /// 옮긴 창은 화면 밖으로 나가지 않는다. 제목 줄을 잃으면 다시 잡을 수 없기 때문이다.
    ///
    /// 끌 자리가 없는 곳(휴대폰 껍데기 안)에서는 여는 쪽이 이 물건을 꺼 둔다.
    /// </summary>
    public class WindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [Tooltip("실제로 움직일 사각형. 보통 제목 줄이 들어 있는 창 전체다.")]
        [SerializeField] private RectTransform _target;

        private RectTransform _area;
        private Vector2 _grabOffset;

        private void Awake()
        {
            if (_target == null) _target = transform.parent as RectTransform;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_target == null) return;

            _area = _target.parent as RectTransform;
            if (_area == null) return;

            // 잡은 자리와 창 한가운데의 거리. 이 거리를 지켜야 창이 손에서 튀지 않는다.
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _area, eventData.position, eventData.pressEventCamera, out var local)) return;

            _grabOffset = _target.anchoredPosition - local;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_target == null || _area == null) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _area, eventData.position, eventData.pressEventCamera, out var local)) return;

            _target.anchoredPosition = Clamp(local + _grabOffset);
        }

        /// <summary>창의 절반쯤은 늘 화면 안에 남게 한다.</summary>
        private Vector2 Clamp(Vector2 wanted)
        {
            var size = _target.rect.size * 0.5f;
            var bounds = _area.rect.size * 0.5f;

            return new Vector2(
                Mathf.Clamp(wanted.x, -bounds.x - size.x * 0.5f, bounds.x + size.x * 0.5f),
                Mathf.Clamp(wanted.y, -bounds.y, bounds.y));
        }
    }
}
