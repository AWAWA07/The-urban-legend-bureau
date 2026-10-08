using UnityEngine;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 달리는 열차의 창밖. 터널 벽의 불빛과 기둥이 오른쪽에서 왼쪽으로 빠르게 지나간다.
    /// 왼쪽 끝을 넘어간 것은 오른쪽 끝으로 돌아와 다시 지나간다. 창 전체는 레일 이음매를 넘듯 가볍게 흔들린다.
    /// </summary>
    public class TunnelScroll : MonoBehaviour
    {
        [Tooltip("지나가는 것들. 각자 따로 움직인다.")]
        [SerializeField] private RectTransform[] _items = new RectTransform[0];

        [Tooltip("지나가는 빠르기(화면 단위/초). _items 와 같은 순서다. 가까운 것일수록 빠르다.")]
        [SerializeField] private float[] _speeds = new float[0];

        [Tooltip("이 너비를 한 바퀴로 돈다. 창 너비보다 조금 넓게 둔다.")]
        [SerializeField] private float _span = 1800f;

        [Tooltip("흔들리는 것. 창 안의 모든 것을 담은 묶음이다.")]
        [SerializeField] private RectTransform _shake;

        [Tooltip("흔들림 세기(화면 단위).")]
        [SerializeField] private float _shakeAmount = 3f;

        private Vector2 _shakeBase;

        private void OnEnable()
        {
            if (_shake != null) _shakeBase = _shake.anchoredPosition;
        }

        private void OnDisable()
        {
            if (_shake != null) _shake.anchoredPosition = _shakeBase;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float half = _span * 0.5f;
            for (int i = 0; i < _items.Length; i++)
            {
                var item = _items[i];
                if (item == null) continue;
                float speed = i < _speeds.Length ? _speeds[i] : 1200f;
                var p = item.anchoredPosition;
                p.x -= speed * dt;
                if (p.x < -half) p.x += _span;
                item.anchoredPosition = p;
            }

            if (_shake != null)
            {
                float t = Time.unscaledTime;
                // 잔잔한 떨림에 이따금 덜컹이 섞인다.
                float bump = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 2.1f)), 24f) * 2.5f;
                float y = (Mathf.PerlinNoise(t * 9f, 0.3f) - 0.5f) * 2f * _shakeAmount + bump;
                float x = (Mathf.PerlinNoise(0.7f, t * 7f) - 0.5f) * _shakeAmount;
                _shake.anchoredPosition = _shakeBase + new Vector2(x, y);
            }
        }
    }
}
