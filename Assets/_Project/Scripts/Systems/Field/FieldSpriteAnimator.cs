using UnityEngine;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 현장 인물의 걷는 그림을 넘긴다.
    ///
    /// 누가 움직이는지(플레이어 입력, 따라 걷기, 연출이 시킨 걸음)는 따지지 않는다.
    /// 좌우로 옮겨진 거리만 보고 그만큼 그림을 넘긴다. 그래서 빨리 걸으면 발도 빨라지고 발이 미끄러져 보이지 않는다.
    /// 서 있으면 서 있는 칸에 멈춘다. 누워 있는 동안(몸이 기울어 있을 때)도 넘기지 않는다.
    /// </summary>
    public class FieldSpriteAnimator : MonoBehaviour
    {
        [Tooltip("그림을 갈아 끼울 곳.")]
        [SerializeField] private SpriteRenderer _renderer;

        [Tooltip("걷는 그림. 한 걸음씩 두 발을 내딛는 한 바퀴를 차례대로 넣는다.")]
        [SerializeField] private Sprite[] _walk = new Sprite[0];

        [Tooltip("서 있을 때 보여 줄 걷기 칸. 두 발이 가장 모인 칸이 자연스럽다.")]
        [SerializeField] private int _idleFrame;

        [Tooltip("걷기 한 바퀴(모든 칸)에 걷는 거리(월드 단위). 작을수록 발이 빨라진다.")]
        [SerializeField] private float _cycleDistance = 3.6f;

        [Tooltip("한 걸음마다 몸이 위로 튀는 높이(그림 자리 단위). 그림의 다리 움직임이 작아 걷는 느낌을 몸의 오르내림으로 채운다.")]
        [SerializeField] private float _stepBob = 0.06f;

        [Tooltip("걸을 때 앞으로 기우는 각도(도).")]
        [SerializeField] private float _walkLean = 3f;

        [Tooltip("서 있을 때 숨 쉬듯 위아래로 늘었다 줄어드는 정도(비율).")]
        [SerializeField] private float _breath = 0.012f;

        [Tooltip("숨 한 번의 길이(초).")]
        [SerializeField] private float _breathSeconds = 2.6f;

        [Tooltip("멈춘 뒤 이만큼(초) 지나면 서 있는 칸으로 돌아간다. 걸음 사이의 한 프레임 멈춤에 깜박이지 않게 한다.")]
        [SerializeField] private float _stopDelay = 0.08f;

        private float _lastX;
        private float _travelled;
        private float _still;
        private float _breathTime;
        private Transform _art;
        private Vector3 _artPosition;
        private Vector3 _artScale = Vector3.one;

        private void OnEnable()
        {
            if (_renderer != null && _art == null)
            {
                _art = _renderer.transform;
                _artPosition = _art.localPosition;
                _artScale = _art.localScale;
            }
            _lastX = transform.position.x;
            _travelled = 0f;
            _still = _stopDelay;
            Show(_idleFrame);
        }

        private void LateUpdate()
        {
            if (_renderer == null || _walk == null || _walk.Length == 0) return;

            float x = transform.position.x;
            float moved = Mathf.Abs(x - _lastX);
            _lastX = x;

            bool lying = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, 0f)) > 1f;
            if (moved > 0.0001f && !lying)
            {
                _still = 0f;
                _travelled += moved;
                float cycle = Mathf.Max(0.1f, _cycleDistance);
                _travelled %= cycle;
                Show((int)(_travelled / cycle * _walk.Length));
                // 한 바퀴에 두 걸음. 발을 디딜 때 낮고 다리를 모을 때 높다.
                float step = Mathf.Abs(Mathf.Cos(_travelled / cycle * Mathf.PI * 2f));
                Pose(_stepBob * step, -_walkLean, 1f);
                return;
            }

            _still += Time.deltaTime;
            if (_still >= _stopDelay)
            {
                _travelled = 0f;
                Show(_idleFrame);
                _breathTime += Time.deltaTime;
                float breath = lying ? 0f : (1f - Mathf.Cos(_breathTime / Mathf.Max(0.1f, _breathSeconds) * Mathf.PI * 2f)) * 0.5f;
                Pose(0f, 0f, 1f + _breath * breath);
            }
        }

        /// <summary>그림만 올리고 기울이고 늘인다. 인물 자체(발 자리, 좌우 뒤집기)는 건드리지 않는다.</summary>
        private void Pose(float up, float lean, float stretch)
        {
            if (_art == null) return;
            _art.localPosition = _artPosition + new Vector3(0f, up, 0f);
            _art.localRotation = Quaternion.Euler(0f, 0f, lean);
            _art.localScale = new Vector3(_artScale.x, _artScale.y * stretch, _artScale.z);
        }

        private void Show(int frame)
        {
            if (_renderer == null || _walk == null || _walk.Length == 0) return;
            var sprite = _walk[Mathf.Clamp(frame, 0, _walk.Length - 1)];
            if (sprite != null && _renderer.sprite != sprite) _renderer.sprite = sprite;
        }
    }
}
