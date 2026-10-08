using System.Collections.Generic;
using UnityEngine;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 열차 안을 살필수록 객실이 조금씩 이상해진다. 무엇이 바뀌었는지 말해 주지 않는다. "아까랑 뭔가 다른데?" 하고 느끼게 한다.
    ///
    /// 살핀 곳 1곳: 형광등 하나가 이따금 깜빡인다.
    /// 2곳: 창밖 터널이 잠깐 멈췄다가 다시 흐른다. 객실은 그대로 흔들리는데 바깥만 멈춘다.
    /// 3곳: 불이 미친 듯이 깜빡이다 모두 꺼진다(정전). 그 뒤로는 차지한의 휴대폰 라이트 둘레만 조금 보인다.
    ///      손잡이 하나도 흔들림과 상관없이 혼자 크게 흔들린다.
    /// 4곳: 가운데 문 유리 너머에 무언가 서 있다. 가까이 가면 사라진다. 어둠 속에서도 유리 속 그것만은 보인다.
    ///
    /// 평소에도 객실 전체가 아주 조금 어둡다(_dark).
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
        [SerializeField] private float _figureAlpha = 0.2f;
        [Tooltip("이만큼(월드 x) 다가가면 사라진다.")]
        [SerializeField] private float _vanishDistance = 3.2f;
        [Tooltip("사라진 뒤 다시 서 있기까지 기다리는 시간(초). 그동안 멀리 떨어져 있어야 한다.")]
        [SerializeField] private float _returnSeconds = 14f;
        [Tooltip("다가가는 사람. 비워 두면 객실 안의 걷는 사람을 찾는다.")]
        [SerializeField] private Transform _watcher;

        [Header("어둠")]
        [Tooltip("객실 전체를 덮는 검은 막. 평소에도 아주 조금 어둡다. 정전이 되면 거의 새까맣다.")]
        [SerializeField] private SpriteRenderer _dark;
        [SerializeField] private float _dimAlpha = 0.12f;
        [SerializeField] private float _blackAlpha = 0.995f;   // 색을 선형으로 섞어서 0.97 만 돼도 꽤 비쳐 보인다
        [Tooltip("정전 때 함께 깜빡이다 꺼지는 형광등과 그 빛.")]
        [SerializeField] private SpriteRenderer[] _allLamps = new SpriteRenderer[0];
        [Tooltip("창밖 터널을 지나가는 불빛. 정전이 되어도 창 너머로 이것만은 흘러간다(어둠보다 앞으로 올린다).")]
        [SerializeField] private SpriteRenderer[] _outsideLights = new SpriteRenderer[0];

        [Header("휴대폰 라이트")]
        [Tooltip("차지한 둘레로 밝아지는 폭(월드 단위). 가로, 세로 반지름이다.")]
        [SerializeField] private Vector2 _lightRadius = new Vector2(2.2f, 2.0f);
        [Tooltip("빛 한가운데의 어둠. 0 이면 환하고 1 이면 깜깜하다. 조금만 밝힌다.")]
        [SerializeField] private float _lightInnerAlpha = 0.72f;
        [Tooltip("빛 가운데가 차지한의 발에서 얼마나 떨어져 있는가.")]
        [SerializeField] private Vector2 _lightOffset = new Vector2(0f, 2.2f);

        /// <summary>
        /// 살핀 곳이 셋이 된 뒤, 플레이어가 현장으로 돌아와 아무것도 하지 않을 때 한 번 부른다.
        /// 받는 쪽(CaseDirector)이 정전 연출을 시작한다(Blackout).
        /// </summary>
        public System.Action<TrainUnease> BlackoutReady;

        private enum DarkState { None, Flicker, Black, Phone }
        private DarkState _darkState;

        /// <summary>불이 다 꺼졌는가(정전 뒤). 가까이 보기 화면도 이때는 어둡게 연다.</summary>
        public bool IsDark => _darkState == DarkState.Black || _darkState == DarkState.Phone;
        private bool _blackoutAsked;
        private float _idleSince = -1f;
        private Color[] _allLampColors;
        private SpriteRenderer _phone;
        private int[] _outsideOrders;

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
            SetDark(_dimAlpha);
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
            _outsideOrders = new int[_outsideLights.Length];
            for (int i = 0; i < _outsideLights.Length; i++) if (_outsideLights[i] != null) _outsideOrders[i] = _outsideLights[i].sortingOrder;
            _allLampColors = new Color[_allLamps.Length];
            for (int i = 0; i < _allLamps.Length; i++) if (_allLamps[i] != null) _allLampColors[i] = _allLamps[i].color;
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
            StopAllCoroutines();
            _darkState = DarkState.None;
            _blackoutAsked = false;
            _idleSince = -1f;
            SetLamps(1f);
            SetDark(_dimAlpha);
            if (_phone != null) _phone.enabled = false;
            SetOutsideAboveDark(false);
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
            if (_darkState == DarkState.None) Apply(lampOn);

            // --- 3. 정전 ---
            // 셋째 곳을 살피고 현장으로 돌아와 잠깐 숨을 돌린 뒤에 일어난다.
            if (stage >= 3 && !_blackoutAsked && _darkState == DarkState.None)
            {
                if (!IsFieldIdle()) _idleSince = -1f;
                else if (_idleSince < 0f) _idleSince = now;
                else if (now - _idleSince > 0.8f && BlackoutReady != null)
                {
                    _blackoutAsked = true;
                    BlackoutReady(this);
                }
            }

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

            if (_darkState == DarkState.Phone) FollowPhone();
        }

        // ------------------------------------------------------------- 정전

        /// <summary>
        /// 불이 미친 듯이 깜빡이다가 모두 꺼진다. 거의 아무것도 보이지 않는다. 다 꺼지고 잠시 뒤 onDark 를 부른다.
        /// </summary>
        public void Blackout(System.Action onDark)
        {
            StopAllCoroutines();
            StartCoroutine(RunBlackout(onDark));
        }

        private System.Collections.IEnumerator RunBlackout(System.Action onDark)
        {
            _darkState = DarkState.Flicker;
            SetOutsideAboveDark(true);
            const float Seconds = 1.9f;
            float end = Time.time + Seconds;
            bool off = false;
            while (Time.time < end)
            {
                off = !off;
                // 갈수록 꺼져 있는 틈이 길어지고 켜져 있는 틈이 짧아진다.
                float left = (end - Time.time) / Seconds;
                SetDark(off ? Random.Range(0.7f, 0.92f) : _dimAlpha);
                SetLamps(off ? 0.08f : 1f);
                yield return new WaitForSeconds(off ? Random.Range(0.03f, 0.12f) * (2f - left) : Random.Range(0.03f, 0.1f) * (0.4f + left));
            }
            SetLamps(0f);
            SetDark(_blackAlpha);
            _darkState = DarkState.Black;
            yield return new WaitForSeconds(0.8f);
            onDark?.Invoke();
        }

        /// <summary>차지한이 휴대폰 라이트를 켠다. 딸깍 한두 번 깜빡이고 켜진다. 그 둘레만 조금 밝다. 다 켜지면 onDone 을 부른다.</summary>
        public void PhoneLight(System.Action onDone)
        {
            StopAllCoroutines();
            StartCoroutine(RunPhoneLight(onDone));
        }

        private System.Collections.IEnumerator RunPhoneLight(System.Action onDone)
        {
            EnsurePhone();
            _darkState = DarkState.Phone;
            FollowPhone();
            foreach (float wait in new[] { 0.06f, 0.08f, 0.05f })
            {
                _phone.enabled = !_phone.enabled;
                if (_dark != null) _dark.enabled = !_phone.enabled;
                yield return new WaitForSeconds(wait);
            }
            _phone.enabled = true;
            if (_dark != null) _dark.enabled = false;
            yield return new WaitForSeconds(0.3f);
            onDone?.Invoke();
        }

        private void SetDark(float alpha)
        {
            if (_dark == null) return;
            _dark.enabled = true;
            var c = _dark.color;
            c.a = alpha;
            _dark.color = c;
        }

        /// <summary>창밖 터널 불빛을 어둠보다 앞으로 올리거나 제자리로 돌린다. 불빛은 유리 마스크 안에서만 보이므로 창 너머로만 흐른다.</summary>
        private void SetOutsideAboveDark(bool above)
        {
            if (_outsideOrders == null) return;
            int front = (_dark != null ? _dark.sortingOrder : 20) + 1;
            for (int i = 0; i < _outsideLights.Length; i++)
                if (_outsideLights[i] != null)
                    // 번지는 빛을 먼저, 불빛 줄기를 그 위에 그린다. 같은 순서끼리는 앞뒤가 들쭉날쭉해진다.
                    _outsideLights[i].sortingOrder = above ? front + (_outsideLights[i].name == "Glow" ? 0 : 1) : _outsideOrders[i];
        }

        private void SetLamps(float on)
        {
            if (_allLampColors == null) return;
            for (int i = 0; i < _allLamps.Length; i++)
            {
                if (_allLamps[i] == null) continue;
                var c = _allLampColors[i];
                c.a *= on;
                _allLamps[i].color = c;
            }
        }

        /// <summary>
        /// 휴대폰 빛. 화면을 다 덮는 검은 판에 타원 구멍이 하나 뚫려 있다. 구멍 가운데도 아주 환하지는 않다.
        /// 그림은 처음 켤 때 한 번 만든다.
        /// </summary>
        private void EnsurePhone()
        {
            if (_phone != null) return;
            const int W = 512, H = 256;
            const float WorldW = 72f;   // 객실 끝에 서도 화면 끝까지 덮는다
            float ppu = W / WorldW;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[W * H];
            float rx = _lightRadius.x * ppu, ry = _lightRadius.y * ppu;
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dx = (x + 0.5f - W * 0.5f) / rx, dy = (y + 0.5f - H * 0.5f) / ry;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Lerp(_lightInnerAlpha, _blackAlpha, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 1f, d)));
                px[y * W + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply();

            var go = new GameObject("PhoneLight");
            go.transform.SetParent(transform, false);
            _phone = go.AddComponent<SpriteRenderer>();
            _phone.sprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), ppu);
            _phone.sortingOrder = _dark != null ? _dark.sortingOrder : 20;
            _phone.enabled = false;
        }

        private void FollowPhone()
        {
            if (_phone == null || _watcher == null) return;
            var p = _watcher.position + (Vector3)_lightOffset;
            p.z = _phone.transform.position.z;
            _phone.transform.position = p;
        }

        /// <summary>플레이어가 현장에서 자유로운가. 말하는 중이거나 다른 화면이 덮여 있으면 아니다.</summary>
        private static bool IsFieldIdle()
        {
            return UrbanLegendBureau.UI.FieldHudScreen.IsFront && !UrbanLegendBureau.UI.FieldHudScreen.IsSpeaking
                && !UrbanLegendBureau.UI.FieldHudScreen.IsCutscene && !UrbanLegendBureau.UI.TravelScreen.IsPlaying;
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
