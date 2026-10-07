using System.Collections.Generic;
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

        [Tooltip("서 있는 그림. 비워 두면 걷기 칸 하나(_idleFrame)에 멈춘다.")]
        [SerializeField] private Sprite[] _idle = new Sprite[0];

        [Tooltip("서 있는 그림 한 칸을 보여 주는 시간(초).")]
        [SerializeField] private float _idleFrameSeconds = 0.4f;

        [Tooltip("뒷모습으로 걷는 그림. 문 안으로 들어설 때처럼 화면 안쪽으로 걸을 때 쓴다.")]
        [SerializeField] private Sprite[] _back = new Sprite[0];

        [Tooltip("뒷모습 한 칸을 보여 주는 시간(초).")]
        [SerializeField] private float _backFrameSeconds = 0.14f;

        /// <summary>연출에서 한 칸씩 골라 보여 주는 그림 한 벌. 코트 벗기, 앉기처럼 걷기와 상관없는 동작이다.</summary>
        [System.Serializable]
        public class Clip
        {
            public string name;
            public Sprite[] frames = new Sprite[0];
        }

        [Tooltip("연출용 그림 묶음. 이름으로 찾는다(coatoff, coathang, tie, sit ...).")]
        [SerializeField] private List<Clip> _clips = new List<Clip>();

        /// <summary>연출이 붙잡아 둔 그림. 있으면 걷기와 서 있기 대신 이것만 보인다.</summary>
        private Sprite _hold;

        /// <summary>그 묶음의 칸 수. 없으면 0.</summary>
        public int FrameCount(string clip)
        {
            var c = Find(clip);
            return c != null && c.frames != null ? c.frames.Length : 0;
        }

        /// <summary>그 묶음의 한 칸을 붙잡아 보여 준다. 놓아줄 때까지 걷기 그림으로 돌아가지 않는다.</summary>
        public bool Hold(string clip, int frame)
        {
            var c = Find(clip);
            if (c == null || c.frames == null || c.frames.Length == 0) return false;
            _hold = c.frames[Mathf.Clamp(frame, 0, c.frames.Length - 1)];
            Pose(0f, 0f, 1f);
            SetSprite(_hold);
            return true;
        }

        /// <summary>붙잡은 그림을 놓는다. 다음 프레임부터 걷기와 서 있기 그림으로 돌아간다.</summary>
        public void Release()
        {
            _hold = null;
        }

        private Clip Find(string clip)
        {
            if (_clips == null) return null;
            foreach (var c in _clips) if (c != null && c.name == clip) return c;
            return null;
        }

        [Tooltip("서 있을 때 보여 줄 걷기 칸. 두 발이 가장 모인 칸이 자연스럽다.")]
        [SerializeField] private int _idleFrame;

        [Tooltip("걷기 한 바퀴(모든 칸)에 걷는 거리(월드 단위). 작을수록 발이 빨라진다.")]
        [SerializeField] private float _cycleDistance = 3.6f;

        [Tooltip("한 걸음마다 몸이 위로 튀는 높이(그림 자리 단위). 걷기 그림에 오르내림이 이미 들어 있어 살짝만 더한다.")]
        [SerializeField] private float _stepBob = 0.02f;

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
        private bool _showBack;
        private float _backTime;

        /// <summary>
        /// 뒷모습으로 걷게 한다. 켜 두는 동안에는 좌우 움직임과 상관없이 뒷모습 칸을 차례로 넘긴다.
        /// 뒷모습 그림이 없으면 아무 일도 하지 않는다.
        /// </summary>
        public void ShowBack(bool on)
        {
            _showBack = on && _back != null && _back.Length > 0;
            _backTime = 0f;
            if (_showBack) { Pose(0f, 0f, 1f); SetSprite(_back[0]); }
        }
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
            _hold = null;
            _showBack = false;
            if (_idle != null && _idle.Length > 0) SetSprite(_idle[0]); else Show(_idleFrame);
        }

        private void LateUpdate()
        {
            if (_hold != null)
            {
                SetSprite(_hold);
                _lastX = transform.position.x;
                return;
            }

            if (_showBack)
            {
                _backTime += Time.deltaTime;
                SetSprite(_back[(int)(_backTime / Mathf.Max(0.02f, _backFrameSeconds)) % _back.Length]);
                _lastX = transform.position.x;
                return;
            }

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
                _breathTime += Time.deltaTime;
                if (_idle != null && _idle.Length > 0)
                {
                    // 서 있는 그림이 숨 쉬는 모습까지 담고 있다. 누워 있으면 첫 칸에 멈춘다.
                    int frame = lying ? 0 : (int)(_breathTime / Mathf.Max(0.05f, _idleFrameSeconds)) % _idle.Length;
                    SetSprite(_idle[frame]);
                    Pose(0f, 0f, 1f);
                    return;
                }
                Show(_idleFrame);
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
            SetSprite(sprite);
        }

        private void SetSprite(Sprite sprite)
        {
            if (_renderer != null && sprite != null && _renderer.sprite != sprite) _renderer.sprite = sprite;
        }
    }
}
