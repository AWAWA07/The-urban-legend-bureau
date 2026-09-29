using UnityEngine;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 앞선 인물을 따라 걷는다.
    ///
    /// 붙어 다니지 않는다. 사이를 얼마쯤 띄우고, 그보다 멀어졌을 때만 좁히러 간다.
    /// 그래서 걸음을 멈추면 뒤따르던 쪽도 조금 뒤에서 함께 멈춘다.
    ///
    /// 앞선 쪽보다 조금 빠르게 둔다. 그러지 않으면 한 번 벌어진 사이가 영영 좁혀지지 않는다.
    /// </summary>
    public class FieldFollower : MonoBehaviour
    {
        private Vector3 _start;
        private Vector3 _startScale = Vector3.one;

        /// <summary>처음 세운 자리로 돌려놓는다. 앞선 인물을 되돌릴 때 함께 부른다.</summary>
        public void ResetToStart()
        {
            transform.localPosition = _start;
            if (_body != null) _body.localScale = _startScale;
            _autoX = null;
            _onArrived = null;
        }

        [Tooltip("따라갈 인물.")]
        [SerializeField] private Transform _target;

        [Tooltip("이만큼 떨어져 선다. 이보다 가까우면 따라가지 않는다.")]
        [SerializeField] private float _gap = 2.4f;

        [Tooltip("1초에 걷는 거리(월드 단위).")]
        [SerializeField] private float _speed = 6.2f;

        [SerializeField] private float _minX = -12f;
        [SerializeField] private float _maxX = 12f;

        [Tooltip("좌우를 뒤집을 것. 비워 두면 자기 자신을 뒤집는다.")]
        [SerializeField] private Transform _body;

        private void Awake()
        {
            if (_body == null) _body = transform;
            _start = transform.localPosition;
            _startScale = _body.localScale;
        }

        private float? _autoX;
        private float _autoSpeed;
        private System.Action _onArrived;

        /// <summary>
        /// 따라가기를 멈추고 그 자리까지 혼자 걸어간다. 닿으면 onArrived 를 한 번 부른다.
        /// 걸을 수 있는 좌우 끝은 따지지 않는다. 문 밖으로 나가는 걸음이 그 끝을 넘기 때문이다.
        /// </summary>
        public void WalkTo(float localX, float speed, System.Action onArrived)
        {
            _autoX = localX;
            _autoSpeed = speed > 0f ? speed : _speed;
            _onArrived = onArrived;
        }

        /// <summary>혼자 걷는 중인가.</summary>
        public bool IsWalkingAlone => _autoX.HasValue;

        private void Update()
        {
            if (_autoX.HasValue)
            {
                var q = transform.localPosition;
                float ax = _autoX.Value - q.x;
                if (Mathf.Abs(ax) < 0.02f)
                {
                    _autoX = null;
                    var done = _onArrived;
                    _onArrived = null;
                    done?.Invoke();
                    return;
                }

                q.x += Mathf.Sign(ax) * Mathf.Min(Mathf.Abs(ax), _autoSpeed * Time.deltaTime);
                transform.localPosition = q;
                FaceTo(Mathf.Sign(ax));
                return;
            }

            if (_target == null) return;

            float dx = _target.localPosition.x - transform.localPosition.x;
            float distance = Mathf.Abs(dx);
            if (distance <= _gap) return;

            // 사이를 딱 그만큼까지만 좁힌다. 지나쳐 가서 앞지르지 않는다.
            float step = Mathf.Min(distance - _gap, _speed * Time.deltaTime);
            float side = Mathf.Sign(dx);

            var p = transform.localPosition;
            p.x = Mathf.Clamp(p.x + side * step, _minX, _maxX);
            transform.localPosition = p;

            FaceTo(side);
        }

        private void FaceTo(float side)
        {
            if (_body == null) return;

            var s = _body.localScale;
            s.x = Mathf.Abs(s.x) * side;
            _body.localScale = s;
        }
    }
}
