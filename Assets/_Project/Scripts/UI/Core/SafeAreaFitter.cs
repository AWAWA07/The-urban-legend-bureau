using UnityEngine;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// RectTransform을 기기의 Safe Area에 맞춘다.
    /// Android 노치/펀치홀과 둥근 모서리에서 UI가 잘리는 것을 막는다.
    ///
    /// PC / WebGL에서는 Screen.safeArea가 화면 전체와 같으므로 아무 일도 하지 않는다.
    /// 해상도나 방향이 바뀔 때만 다시 계산한다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Tooltip("가로 방향 여백만 적용한다. 상하를 꽉 채우는 배경에 사용한다.")]
        [SerializeField] private bool _horizontalOnly;

        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        /// <summary>마지막으로 적용된 Safe Area. 진단용.</summary>
        public Rect AppliedSafeArea => _lastSafeArea;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (HasChanged())
            {
                Apply();
            }
        }

        private bool HasChanged()
        {
            return Screen.safeArea != _lastSafeArea
                || Screen.width != _lastScreenWidth
                || Screen.height != _lastScreenHeight;
        }

        /// <summary>즉시 다시 계산한다.</summary>
        public void Apply()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (Screen.width <= 0 || Screen.height <= 0) return;

            var safeArea = Screen.safeArea;
            _lastSafeArea = safeArea;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            var min = safeArea.position;
            var max = safeArea.position + safeArea.size;

            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            if (_horizontalOnly)
            {
                min.y = 0f;
                max.y = 1f;
            }

            _rectTransform.anchorMin = min;
            _rectTransform.anchorMax = max;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}
