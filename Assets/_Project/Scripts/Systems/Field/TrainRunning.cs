using UnityEngine;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 달리는 열차 안. 창밖의 터널 불빛과 기둥이 뒤로 빠르게 지나가고, 객실이 잔잔히 흔들리며, 손잡이가 흔들린다.
    ///
    /// 창밖의 것들은 창 유리 안에서만 보인다(유리 자리에 SpriteMask). 창의 왼쪽 끝을 넘어간 것은 오른쪽 끝으로 돌아온다.
    /// 흔들리는 것은 객실 묶음 전체다. 사람과 조사 지점도 함께 흔들리니 서로의 거리는 그대로다.
    /// </summary>
    public class TrainRunning : MonoBehaviour
    {
        [Tooltip("창밖에서 지나가는 것들.")]
        [SerializeField] private Transform[] _passing = new Transform[0];

        [Tooltip("지나가는 빠르기(월드 단위/초). _passing 과 같은 순서다. 가까운 것일수록 빠르다.")]
        [SerializeField] private float[] _speeds = new float[0];

        [Tooltip("각 것이 오가는 왼쪽 끝과 오른쪽 끝(부모 기준 x). _passing 과 같은 순서다.")]
        [SerializeField] private Vector2[] _ranges = new Vector2[0];

        [Tooltip("흔들리는 객실 묶음.")]
        [SerializeField] private Transform _car;

        [Tooltip("흔들림 세기(월드 단위).")]
        [SerializeField] private float _shake = 0.025f;

        [Tooltip("흔들리는 손잡이 고리.")]
        [SerializeField] private Transform[] _rings = new Transform[0];

        [Tooltip("타고 나서 제 빠르기에 이르기까지 걸리는 시간(초). 처음에는 거의 서 있다가 서서히 빨라진다.")]
        [SerializeField] private float _startSeconds = 5f;

        /// <summary>지금 빠르기의 배율. 0 이면 서 있고 1 이면 제 빠르기다.</summary>
        private float _pace;
        private float _startedAt;

        private Vector3 _carBase;
        private Vector3[] _ringBase;

        private void OnEnable()
        {
            _startedAt = Time.time;
            _pace = 0f;
            if (_car != null) _carBase = _car.localPosition;
            _ringBase = new Vector3[_rings.Length];
            for (int i = 0; i < _rings.Length; i++) if (_rings[i] != null) _ringBase[i] = _rings[i].localPosition;
        }

        private void OnDisable()
        {
            if (_car != null) _car.localPosition = _carBase;
            for (int i = 0; i < _rings.Length; i++) if (_rings[i] != null) _rings[i].localPosition = _ringBase[i];
        }

        private void Update()
        {
            // 서서히 출발한다. 처음 1초는 덜컹 하고 움찔한 뒤, 느리게 시작해 점점 빨라진다.
            float since = Time.time - _startedAt;
            float k = _startSeconds > 0f ? Mathf.Clamp01(since / _startSeconds) : 1f;
            _pace = k * k;
            float dt = Time.deltaTime * _pace;
            for (int i = 0; i < _passing.Length; i++)
            {
                var t = _passing[i];
                if (t == null) continue;
                float speed = i < _speeds.Length ? _speeds[i] : 20f;
                var range = i < _ranges.Length ? _ranges[i] : new Vector2(-3f, 3f);
                var p = t.localPosition;
                p.x -= speed * dt;   // 열차가 오른쪽으로 달리니 창밖은 왼쪽으로 흘러간다(가까이 보기 화면과 같은 쪽)
                if (p.x < range.x) p.x += range.y - range.x;
                t.localPosition = p;
            }

            float time = Time.time;
            if (_car != null)
            {
                // 잔잔한 떨림에 이따금 레일 이음매를 넘는 덜컹이 섞인다.
                float bump = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(time * 2.1f)), 24f) * 0.04f;
                float jolt = since < 0.35f ? Mathf.Sin(since / 0.35f * Mathf.PI) * 0.05f : 0f;   // 출발할 때 한 번 덜컹
                float y = (Mathf.PerlinNoise(time * 6f, 0.3f) - 0.5f) * 2f * _shake * Mathf.Max(0.2f, _pace) + bump * _pace - jolt;
                _car.localPosition = _carBase + new Vector3(0f, y, 0f);
            }

            for (int i = 0; i < _rings.Length; i++)
            {
                if (_rings[i] == null) continue;
                float sway = Mathf.Sin(time * 1.7f + i * 0.6f) * 0.05f * _pace + (since < 1.2f ? Mathf.Sin(since / 1.2f * Mathf.PI) * 0.08f : 0f);   // 출발할 때 뒤로 쏠렸다 돌아온다
                _rings[i].localPosition = _ringBase[i] + new Vector3(sway, 0f, 0f);
            }
        }
    }
}
