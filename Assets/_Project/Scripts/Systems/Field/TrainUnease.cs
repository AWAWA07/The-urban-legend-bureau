using System.Collections.Generic;
using UnityEngine;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 열차 안을 살필수록 객실이 조금씩 이상해진다. 무엇이 바뀌었는지 말해 주지 않는다. "아까랑 뭔가 다른데?" 하고 느끼게 한다.
    ///
    /// 살핀 곳 1곳: 형광등 하나가 이따금 깜빡인다.
    /// 2곳: 창밖 터널이 잠깐 멈췄다가 다시 흐른다. 객실은 그대로 흔들리는데 바깥만 멈춘다.
    /// 3곳: 손잡이 하나가 흔들림과 상관없이 혼자 크게 흔들린다.
    /// 4곳: 가운데 문 유리 너머에 무언가 서 있다. 가까이 가면 사라진다.
    ///
    /// 객실이 꺼져 있는 동안(가까이 보기 화면)에는 아무것도 하지 않는다. 다시 켜지면 조금 뒤에 이어 간다.
    /// </summary>
    public class TrainUnease : MonoBehaviour
    {
        [Header("1. 깜빡이는 형광등")]
        [SerializeField] private SpriteRenderer _lamp;
        [SerializeField] private SpriteRenderer _lampGlow;

        [Header("2. 멈추는 창밖")]
        [SerializeField] private TrainRunning _running;

        [Header("3. 혼자 흔들리는 손잡이")]
        [Tooltip("손잡이 끈. 위 끝을 축으로 돈다.")]
        [SerializeField] private Transform _strap;
        [SerializeField] private Transform _ring;
        [Tooltip("흔들리는 폭(도).")]
        [SerializeField] private float _swingAngle = 24f;
        [Tooltip("한 번 오가는 데 걸리는 시간(초).")]
        [SerializeField] private float _swingPeriod = 2.3f;

        [Header("4. 문 유리 너머")]
        [SerializeField] private SpriteRenderer _figure;
        [Tooltip("다 드러났을 때의 진하기. 유리에 비친 것처럼 옅다.")]
        [SerializeField] private float _figureAlpha = 0.34f;
        [Tooltip("이만큼(월드 x) 다가가면 사라진다.")]
        [SerializeField] private float _vanishDistance = 3.2f;
        [Tooltip("사라진 뒤 다시 서 있기까지 기다리는 시간(초). 그동안 멀리 떨어져 있어야 한다.")]
        [SerializeField] private float _returnSeconds = 14f;
        [Tooltip("다가가는 사람. 비워 두면 객실 안의 걷는 사람을 찾는다.")]
        [SerializeField] private Transform _watcher;

        private readonly HashSet<string> _looked = new HashSet<string>();

        /// <summary>지금 단계. 살핀 곳의 수다.</summary>
        public int Stage => _looked.Count;

        private Color _lampColor, _glowColor, _figureColor;
        private Vector3 _strapBase, _ringBase;
        private Quaternion _strapRotation;
        private float _strapHalf = 0.5f;   // 끈 길이의 절반(부모 기준)
        private bool _captured;

        private float _nextFlicker;
        private float _flickerEnd;
        private float _nextHold;
        private float _swingStarted = -1f;
        private float _figureFade;          // 0 이면 없고 1 이면 다 드러났다
        private bool _figureGone;
        private float _figureGoneAt = -1000f;

        private void Awake()
        {
            Capture();
            if (_watcher == null)
            {
                var walker = GetComponentInChildren<FieldWalker>(true);
                if (walker != null) _watcher = walker.transform;
            }
            Apply(1f);
            if (_figure != null) _figure.enabled = false;
        }

        private void Capture()
        {
            if (_captured) return;
            _captured = true;
            if (_lamp != null) _lampColor = _lamp.color;
            if (_lampGlow != null) _glowColor = _lampGlow.color;
            if (_figure != null) _figureColor = _figure.color;
            if (_strap != null) { _strapBase = _strap.localPosition; _strapRotation = _strap.localRotation; 
                var sr = _strap.GetComponent<SpriteRenderer>();
                _strapHalf = sr == null ? 0.5f
                    : (sr.drawMode != SpriteDrawMode.Simple ? sr.size.y * 0.5f : sr.sprite != null ? sr.sprite.bounds.extents.y : 0.5f) * _strap.localScale.y; }
            if (_ring != null) _ringBase = _ring.localPosition;
        }

        private void OnEnable()
        {
            // 가까이 보기에서 돌아온 직후 곧바로 무언가 일어나지 않게 조금 숨을 돌린다.
            _nextFlicker = Time.time + Random.Range(2f, 5f);
            if (_nextHold > 0f) _nextHold = Mathf.Max(_nextHold, Time.time + 3f);
        }

        /// <summary>처음으로 되돌린다. 새 사건을 시작할 때 부른다.</summary>
        public void ResetUnease()
        {
            Capture();
            _looked.Clear();
            _nextHold = 0f;
            _swingStarted = -1f;
            _figureFade = 0f;
            _figureGone = false;
            _figureGoneAt = -1000f;
            Apply(1f);
            if (_figure != null) _figure.enabled = false;
            if (_strap != null) { _strap.localPosition = _strapBase; _strap.localRotation = _strapRotation; }
            if (_ring != null) _ring.localPosition = _ringBase;
        }

        /// <summary>열차 안의 한 곳을 살폈다. 처음 살핀 곳이면 객실이 한 단계 더 이상해진다.</summary>
        public void Look(string pointId)
        {
            if (string.IsNullOrEmpty(pointId) || !_looked.Add(pointId)) return;

            // 창밖이 처음 멈추는 것은 살피고 돌아와 걷기 시작할 즈음이다.
            if (Stage == 2) _nextHold = Time.time + 4f;
            if (Stage == 3) _swingStarted = Time.time;
        }

        private void Update()
        {
            float now = Time.time;
            int stage = Stage;

            // --- 1. 형광등 ---
            float lampOn = 1f;
            if (stage >= 1 && _lamp != null)
            {
                if (now >= _nextFlicker)
                {
                    _flickerEnd = now + Random.Range(0.35f, 0.7f);
                    _nextFlicker = now + Random.Range(4f, 9f);
                }
                if (now < _flickerEnd)
                {
                    // 켜졌다 꺼졌다 잘게 끊긴다. 완전히 꺼지지는 않는다.
                    lampOn = Mathf.PerlinNoise(now * 28f, 0.7f) > 0.5f ? 1f : 0.18f;
                }
            }
            Apply(lampOn);

            // --- 2. 창밖 ---
            if (stage >= 2 && _running != null && _nextHold > 0f && now >= _nextHold)
            {
                _running.HoldOutside(Random.Range(1.4f, 2.0f));
                _nextHold = now + Random.Range(22f, 34f);
            }
        }

        private void LateUpdate()
        {
            // --- 3. 손잡이 ---
            // TrainRunning 이 고리를 잔잔히 흔든 뒤에 덮어쓴다. 이 고리 하나만 따로 논다.
            if (Stage >= 3 && _swingStarted >= 0f && _strap != null && _ring != null)
            {
                float t = Time.time - _swingStarted;
                float grow = Mathf.Clamp01(t / 3f);   // 처음에는 조금씩, 점점 크게
                float angle = Mathf.Sin(t * Mathf.PI * 2f / _swingPeriod) * _swingAngle * grow;
                var pivot = _strapBase + new Vector3(0f, _strapHalf, 0f);
                float length = pivot.y - _ringBase.y;
                float rad = angle * Mathf.Deg2Rad;
                var down = new Vector3(Mathf.Sin(rad), -Mathf.Cos(rad), 0f);
                _strap.localRotation = _strapRotation * Quaternion.Euler(0f, 0f, angle);
                _strap.localPosition = pivot + down * _strapHalf;
                _ring.localPosition = pivot + down * length;
            }

            // --- 4. 문 유리 너머 ---
            if (_figure != null) UpdateFigure();
        }

        private void UpdateFigure()
        {
            float now = Time.time;
            float distance = _watcher != null ? Mathf.Abs(_watcher.position.x - _figure.transform.position.x) : 99f;

            if (Stage < 4)
            {
                _figureFade = 0f;
            }
            else if (!_figureGone)
            {
                if (distance < _vanishDistance && _figureFade > 0.5f)
                {
                    // 다가가면 끊기듯 사라진다. 스르르 사라지지 않는다.
                    _figureGone = true;
                    _figureGoneAt = now;
                }
                else
                {
                    _figureFade = Mathf.MoveTowards(_figureFade, 1f, Time.deltaTime / 1.6f);
                }
            }
            else if (now - _figureGoneAt > _returnSeconds && distance > _vanishDistance * 2f)
            {
                _figureGone = false;
            }

            float alpha = _figureFade * _figureAlpha;
            if (_figureGone)
            {
                float since = now - _figureGoneAt;
                // 한 번 깜빡 남았다가 없어진다.
                alpha = since < 0.05f ? 0f : since < 0.1f ? _figureAlpha : 0f;
                if (since >= 0.1f) _figureFade = 0f;
            }

            var c = _figureColor;
            c.a = _figureColor.a * alpha;
            _figure.color = c;
            _figure.enabled = alpha > 0.001f;
        }

        private void Apply(float lampOn)
        {
            if (_lamp != null)
            {
                var c = _lampColor;
                c.r *= lampOn; c.g *= lampOn; c.b *= lampOn;
                _lamp.color = c;
            }
            if (_lampGlow != null)
            {
                var g = _glowColor;
                g.a *= lampOn;
                _lampGlow.color = g;
            }
        }
    }
}
