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

        [Tooltip("멈춘 뒤 이만큼(초) 지나면 서 있는 칸으로 돌아간다. 걸음 사이의 한 프레임 멈춤에 깜박이지 않게 한다.")]
        [SerializeField] private float _stopDelay = 0.08f;

        private float _lastX;
        private float _travelled;
        private float _still;

        private void OnEnable()
        {
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
                return;
            }

            _still += Time.deltaTime;
            if (_still >= _stopDelay)
            {
                _travelled = 0f;
                Show(_idleFrame);
            }
        }

        private void Show(int frame)
        {
            if (_renderer == null || _walk == null || _walk.Length == 0) return;
            var sprite = _walk[Mathf.Clamp(frame, 0, _walk.Length - 1)];
            if (sprite != null && _renderer.sprite != sprite) _renderer.sprite = sprite;
        }
    }
}
