using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.InputSystemLayer;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 현장에서 좌우로 걸어 다니는 인물.
    ///
    /// A / D 와 방향키 ← → 를 쓴다. 두 벌을 따로 읽지 않는다.
    /// 기존 Player/Move 액션이 이미 그 둘을 함께 물고 있으므로 그 값의 좌우 성분만 본다.
    ///
    /// 현장은 옆에서 본 한 폭의 장면이라 위아래로는 움직이지 않는다.
    /// 화면 밖으로 걸어 나가지 않도록 좌우 끝을 정해 둔다.
    ///
    /// 이 물건은 현장 뿌리 아래에 산다. 현장이 꺼져 있으면 Update 도 돌지 않는다.
    /// </summary>
    public class FieldWalker : MonoBehaviour
    {
        private Vector3 _start;
        private float _startFacing = 1f;

        /// <summary>
        /// 처음 세운 자리로 돌려놓는다. 보는 쪽도 처음대로다.
        /// 숙소처럼 다른 곳에서 옮겨 와 들어서는 장소는 들어설 때마다 문 앞에서 시작해야 한다.
        /// </summary>
        public void ResetToStart()
        {
            transform.localPosition = _start;
            Facing = _startFacing;
            IsWalking = false;
            Locked = false;
            _autoX = null;
            _onArrived = null;
            ApplyFacing();
        }

        [Tooltip("1초에 걷는 거리(월드 단위).")]
        [SerializeField] private float _speed = 5.5f;

        [Tooltip("걸어갈 수 있는 왼쪽 끝.")]
        [SerializeField] private float _minX = -12f;

        [Tooltip("걸어갈 수 있는 오른쪽 끝.")]
        [SerializeField] private float _maxX = 12f;

        [Tooltip("좌우를 뒤집을 것. 비워 두면 자기 자신을 뒤집는다.")]
        [SerializeField] private Transform _body;

        [Tooltip("멈춤으로 치는 기울기. 이보다 작게 밀면 움직이지 않는다.")]
        [SerializeField] private float _deadZone = 0.15f;

        private InputService _input;

        /// <summary>보고 있는 쪽. 오른쪽이 1, 왼쪽이 -1.</summary>
        public float Facing { get; private set; } = 1f;

        /// <summary>지금 걷고 있는가. 따라가는 쪽이 이 값을 볼 수 있다.</summary>
        public bool IsWalking { get; private set; }

        private void Awake()
        {
            if (_body == null) _body = transform;

            // 처음 세운 자리와 보는 쪽을 기억한다. 장소에 다시 들어설 때 그 자리로 돌려놓는다.
            _start = transform.localPosition;
            Facing = _body.localScale.x < 0f ? -1f : 1f;
            _startFacing = Facing;
        }

        /// <summary>
        /// 붙잡아 둔다. 켜 두는 동안에는 입력을 받아도 걷지 않는다.
        /// 다른 사람이 움직이는 연출 동안 플레이어가 제자리에 서 있어야 할 때 쓴다.
        /// </summary>
        public bool Locked { get; set; }

        private float? _autoX;
        private float _autoSpeedScale = 1f;
        private System.Action _onArrived;

        /// <summary>
        /// 입력 없이 그 자리까지 걸어간다. 닿으면 onArrived 를 한 번 부른다.
        /// 걷는 빠르기와 뒤집기는 평소 걸을 때와 같다. speedScale 로 조금 늦출 수 있다.
        /// 붙잡혀 있어도, 누가 말하는 중이어도 이 걸음은 간다. 연출이 부르는 것이기 때문이다.
        /// </summary>
        public void WalkTo(float localX, System.Action onArrived, float speedScale = 1f)
        {
            _autoX = Mathf.Clamp(localX, _minX, _maxX);
            _autoSpeedScale = Mathf.Max(0.1f, speedScale);
            _onArrived = onArrived;
        }

        /// <summary>연출이 시킨 걸음을 한 번 옮긴다. 닿았으면 true.</summary>
        private bool StepAuto()
        {
            var p = transform.localPosition;
            float dx = _autoX.Value - p.x;

            if (Mathf.Abs(dx) < 0.02f)
            {
                IsWalking = false;
                _autoX = null;
                var done = _onArrived;
                _onArrived = null;
                done?.Invoke();
                return true;
            }

            IsWalking = true;
            Facing = Mathf.Sign(dx);
            p.x += Mathf.Sign(dx) * Mathf.Min(Mathf.Abs(dx), _speed * _autoSpeedScale * Time.deltaTime);
            transform.localPosition = p;
            ApplyFacing();
            return false;
        }

        private void Update()
        {
            if (_autoX.HasValue) { StepAuto(); return; }

            if (Locked)
            {
                IsWalking = false;
                return;
            }

            // 서 있어야 할 때가 둘이다.
            //   누가 말하는 동안 - 서서 듣는다. 뒤따르는 쪽도 함께 멈춘다.
            //   다른 화면이 위에 떠 있는 동안 - 규칙 추론이나 조사 방법을 고르는 중이다.
            //     그때도 걸으면 화면 뒤에서 사람이 움직여, 돌아왔을 때 엉뚱한 자리에 서 있다.
            if (UrbanLegendBureau.UI.FieldHudScreen.IsSpeaking || !UrbanLegendBureau.UI.FieldHudScreen.IsFront)
            {
                IsWalking = false;
                return;
            }

            // 현장은 서비스가 다 선 뒤에 켜지지만, 켜지는 순서를 믿지 않고 늦게라도 집는다.
            if (_input == null && !ServiceRegistry.TryGet(out _input)) return;
            if (!_input.IsReady) return;

            float x = _input.MoveValue.x;

            IsWalking = Mathf.Abs(x) > _deadZone;
            if (!IsWalking) return;

            Facing = Mathf.Sign(x);

            var p = transform.localPosition;
            p.x = Mathf.Clamp(p.x + x * _speed * Time.deltaTime, _minX, _maxX);
            transform.localPosition = p;

            ApplyFacing();
        }

        private void ApplyFacing()
        {
            if (_body == null) return;

            var s = _body.localScale;
            s.x = Mathf.Abs(s.x) * Facing;
            _body.localScale = s;
        }
    }
}
