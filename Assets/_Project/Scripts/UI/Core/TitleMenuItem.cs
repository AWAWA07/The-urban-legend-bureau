using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 타이틀 메뉴 한 줄. 마우스를 올리면 뒤에 붉은 띠가 깔리고, 내리면 걷힌다.
    /// 키보드나 패드로 골랐을 때(선택됨)도 같은 모습을 보인다.
    /// 누르는 일은 같은 물건의 Button 이 맡는다.
    /// </summary>
    public class TitleMenuItem : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [Tooltip("마우스를 올렸을 때 글자 뒤에 깔리는 붉은 띠.")]
        [SerializeField] private Graphic _highlight;

        [Tooltip("띠가 나타나고 사라지는 시간(초).")]
        [SerializeField] private float _fadeSeconds = 0.08f;

        private bool _hovered;
        private bool _selected;

        private void OnEnable()
        {
            _hovered = false;
            _selected = false;
            if (_highlight != null) _highlight.canvasRenderer.SetAlpha(0f);
        }

        public void OnPointerEnter(PointerEventData eventData) { _hovered = true; Apply(); }
        public void OnPointerExit(PointerEventData eventData) { _hovered = false; Apply(); }
        public void OnSelect(BaseEventData eventData) { _selected = true; Apply(); }
        public void OnDeselect(BaseEventData eventData) { _selected = false; Apply(); }

        private void Apply()
        {
            if (_highlight == null) return;
            _highlight.CrossFadeAlpha(_hovered || _selected ? 1f : 0f, _fadeSeconds, true);
        }
    }
}
