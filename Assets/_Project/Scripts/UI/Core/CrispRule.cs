using UnityEngine;
using UnityEngine.UI;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 언제나 같은 두께로 보이는 가로선.
    ///
    /// 캔버스는 화면 크기에 맞춰 통째로 줄었다 늘었다 한다.
    /// 그래서 기준 해상도에서 2로 준 선이 작은 창에서는 0.7 픽셀이 되어 아예 사라진다.
    /// 여기서는 실제 화면 픽셀 수를 정해 두고, 배율이 바뀔 때마다 높이를 거꾸로 계산한다.
    /// </summary>
    [ExecuteAlways]
    public class CrispRule : MonoBehaviour
    {
        [Tooltip("실제 화면에서 차지할 픽셀 수.")]
        [SerializeField] private float _pixels = 1f;

        private Canvas _canvas;
        private LayoutElement _element;
        private RectTransform _rect;
        private float _appliedScale = -1f;

        private void OnEnable()
        {
            _rect = (RectTransform)transform;
            _element = GetComponent<LayoutElement>();
            _canvas = null;
            _appliedScale = -1f;
            Apply();
        }

        private void Update()
        {
            Apply();
        }

        private void Apply()
        {
            if (_rect == null) _rect = (RectTransform)transform;
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();

            // 캔버스 배율만 보면 안 된다. 중간에 통째로 줄인 것이 있으면 선도 그만큼 얇아져 사라진다.
            // 휴대폰 안에 접어 넣은 괴담넷이 그렇다. 실제로 실린 배율 전체를 본다.
            float scale = Mathf.Abs(_rect.lossyScale.y);
            if (scale < 0.0001f)
            {
                scale = _canvas != null && _canvas.scaleFactor > 0.0001f ? _canvas.scaleFactor : 1f;
            }
            if (Mathf.Approximately(scale, _appliedScale)) return;

            _appliedScale = scale;
            float height = Mathf.Max(_pixels / scale, 1f);

            // 배치에 맡긴 선은 LayoutElement 로, 직접 붙인 선은 크기로 정한다.
            // 배치에서 빠져 있다고 표시된 선은 LayoutElement 가 있어도 제 크기로 정해야 한다.
            if (_element != null && !_element.ignoreLayout)
            {
                _element.minHeight = height;
                _element.preferredHeight = height;
                _element.flexibleHeight = 0f;
                LayoutRebuilder.MarkLayoutForRebuild(_rect);
            }
            else
            {
                _rect.sizeDelta = new Vector2(_rect.sizeDelta.x, height);
            }
        }
    }
}
