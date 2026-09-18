using UnityEngine;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 가리키는 표시를 좌우로 조금씩 흔든다.
    ///
    /// 가만히 서 있는 세모는 화면에 그려 넣은 무늬처럼 보인다.
    /// 조금씩 움직이면 사람이 손끝으로 가리키는 것처럼 보여, 눈이 그리로 간다.
    ///
    /// 처음 켤 때의 자리를 기준으로 삼는다. 씬에서 자리를 옮겨도 그 자리에서 흔들린다.
    /// </summary>
    public class HintNudge : MonoBehaviour
    {
        [Tooltip("좌우로 움직이는 폭(픽셀). 가리키는 정도지 튀어 다니면 안 된다.")]
        [SerializeField] private float _distance = 7f;

        [Tooltip("한 번 왕복하는 데 걸리는 시간(초). 느릴수록 가리키는 손짓에 가깝다.")]
        [SerializeField] private float _period = 2.4f;

        private RectTransform _rt;
        private Vector2 _home;
        private bool _homeSet;

        private void Awake()
        {
            _rt = transform as RectTransform;
        }

        private void OnEnable()
        {
            // 자리는 한 번만 기억한다. 켤 때마다 다시 재면 흔들린 자리가 기준이 되어 밀려난다.
            if (_rt != null && !_homeSet)
            {
                _home = _rt.anchoredPosition;
                _homeSet = true;
            }
        }

        private void OnDisable()
        {
            if (_rt != null && _homeSet) _rt.anchoredPosition = _home;
        }

        private void Update()
        {
            if (_rt == null || !_homeSet || _period <= 0f) return;

            float phase = Mathf.Sin(Time.unscaledTime * (Mathf.PI * 2f / _period));
            _rt.anchoredPosition = new Vector2(_home.x + phase * _distance, _home.y);
        }
    }
}
