using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 가까이 보기 화면에서 살필 곳에 마우스를 올리면 그 물건 모양 그대로 둘레가 옅게 빛난다.
    /// 네모난 판을 덮지 않는다. 금, 흠집, 스티커 같은 그림 하나하나에 테두리 빛을 켰다 끈다.
    /// 누르는 자리(투명한 단추)와 빛낼 그림은 따로 둔다.
    /// </summary>
    public class HotspotGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("빛낼 그림들. 이 아래 그림까지 모두 빛난다.")]
        [SerializeField] private GameObject[] _targets = new GameObject[0];

        [SerializeField] private Color _glowColor = new Color(1f, 0.93f, 0.7f, 0.55f);
        [SerializeField] private float _glowWidth = 3f;

        private Outline[] _outlines;
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            var list = new System.Collections.Generic.List<Outline>();
            foreach (var root in _targets)
            {
                if (root == null) continue;
                foreach (var g in root.GetComponentsInChildren<Graphic>(true))
                {
                    if (g is TMPro.TMP_Text) continue;
                    var o = g.gameObject.AddComponent<Outline>();
                    o.effectColor = _glowColor;
                    o.effectDistance = new Vector2(_glowWidth, -_glowWidth);
                    o.useGraphicAlpha = false;
                    o.enabled = false;
                    list.Add(o);
                }
            }
            _outlines = list.ToArray();
        }

        private void OnDisable()
        {
            Set(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable) return;
            Set(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Set(false);
        }

        private void Set(bool on)
        {
            if (_outlines == null) return;
            foreach (var o in _outlines) if (o != null) o.enabled = on;
        }
    }
}
