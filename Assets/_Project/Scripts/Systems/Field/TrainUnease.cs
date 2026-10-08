using System.Collections.Generic;
using UnityEngine;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 열차 안을 살필수록 객실이 조금씩 이상해진다. 무엇이 바뀌었는지 말해 주지 않는다. "아까랑 뭔가 다른데?" 하고 느끼게 한다.
    ///
    /// 타자마자: 형광등 하나가 이따금 깜빡인다.
    /// 살핀 곳 1곳: 살피고 돌아오면 곧바로 불이 미친 듯이 깜빡이다 모두 꺼진다(정전). 그 뒤로는 차지한의 휴대폰 라이트 둘레만 조금 보인다.
    /// 3곳: 손잡이 하나가 흔들림과 상관없이 혼자 크게 흔들린다.
    /// 4곳: 맨 오른쪽 창 너머에 무언가 서 있다. 가까이 가면 사라진다. 어둠 속에서도 유리 속 그것만은 보인다.
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

        [Header("3. 혼자 흔들리는 손잡이")]
        [Tooltip("손잡이 끈. 위 끝을 축으로 돈다.")]
        [SerializeField] private Transform _strap;
        [SerializeField] private Transform _ring;
        [Tooltip("흔들리는 폭(도).")]
        [SerializeField] private float _swingAngle = 24f;
        [Tooltip("한 번 오가는 데 걸리는 시간(초).")]
        [SerializeField] private float _swingPeriod = 2.3f;

        [Header("창의 손바닥 자국")]
        [Tooltip("창 가까이 보기에서 손바닥 자국을 살피고 나면 현장의 창에도 보인다. 그 전에는 꺼 둔다.")]
        [SerializeField] private GameObject _handprint;

        /// <summary>현장의 창에 손바닥 자국을 남기거나 지운다.</summary>
        public void ShowHandprint(bool on)
        {
            if (_handprint != null) _handprint.SetActive(on);
        }

        [Header("4. 맨 오른쪽 창 너머")]
        [SerializeField] private SpriteRenderer _figure;
        [Tooltip("다 드러났을 때의 진하기. 유리에 비친 것처럼 옅다.")]
        [SerializeField] private float _figureAlpha = 0.11f;
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
        [Tooltip("차지한 둘레에 잔잔하게 남는 빛. 발에서 빛 가운데까지와 가로, 세로 반지름(월드 단위).")]
        [SerializeField] private Vector2 _lightOffset = new Vector2(0f, 2.2f);
        [SerializeField] private Vector2 _lightRadius = new Vector2(2.2f, 2.0f);
        [Tooltip("차지한 둘레 빛 한가운데의 어둠. 0 이면 환하고 1 이면 깜깜하다. 조금만 밝힌다.")]
        [SerializeField] private float _lightInnerAlpha = 0.72f;
        [Tooltip("마우스를 따라가는 빛의 반지름(월드 단위). 비춰 가며 객실을 살핀다.")]
        [SerializeField] private float _pointerRadius = 1.7f;
        [Tooltip("마우스 빛 한가운데의 어둠.")]
        [SerializeField] private float _pointerInnerAlpha = 0.62f;

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

        private void OnDisable()
        {
            // 열차 밖(승강장, 숙소)에서는 휴대폰에 라이트 표시를 띄우지 않는다.
            UrbanLegendBureau.UI.FieldHudScreen.FlashlightOn = false;
        }

        private void OnEnable()
        {
            // 가까이 보기에서 돌아온 직후 곧바로 무언가 일어나지 않게 조금 숨을 돌린다.
            _nextFlicker = Time.time + Random.Range(2f, 5f);
        }

        /// <summary>처음으로 되돌린다. 새 사건을 시작할 때 부른다.</summary>
        public void ResetUnease()
        {
            Capture();
            _looked.Clear();
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
            ShowHandprint(false);
            var baseAnim = _watcher != null ? _watcher.GetComponent<FieldSpriteAnimator>() : null;
            if (baseAnim != null) baseAnim.UseBaseSet();
        }

        /// <summary>열차 안의 한 곳을 살폈다. 처음 살핀 곳이면 객실이 한 단계 더 이상해진다.</summary>
        public void Look(string pointId)
        {
            if (string.IsNullOrEmpty(pointId) || !_looked.Add(pointId)) return;

            if (Stage == 3) _swingStarted = Time.time;
        }

        private void Update()
        {
            float now = Time.time;
            int stage = Stage;

            // --- 1. 형광등 ---
            float lampOn = 1f;
            // 열차에 타자마자부터 깜빡인다. 아직 아무것도 살피지 않았을 때부터 어딘가 불안하다.
            if (_lamp != null)
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
            // 처음 한 곳을 살피고 현장으로 돌아와 잠깐 숨을 돌린 뒤에 곧바로 일어난다.
            if (stage >= 1 && !_blackoutAsked && _darkState == DarkState.None)
            {
                if (!IsFieldIdle()) _idleSince = -1f;
                else if (_idleSince < 0f) _idleSince = now;
                else if (now - _idleSince > 0.8f && BlackoutReady != null)
                {
                    _blackoutAsked = true;
                    BlackoutReady(this);
                }
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
            UrbanLegendBureau.UI.FieldHudScreen.FlashlightOn = _darkState == DarkState.Phone;
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

        /// <summary>차지한이 휴대폰을 든다. 걷기와 서 있기 그림이 휴대폰 든 모습으로 곧바로 바뀐다. 불은 아직 켜지 않는다.</summary>
        public void RaisePhone()
        {
            var anim = _watcher != null ? _watcher.GetComponent<FieldSpriteAnimator>() : null;
            if (anim != null) anim.UseSet("phonewalk", "phoneidle");
        }

        private System.Collections.IEnumerator RunPhoneLight(System.Action onDone)
        {
            RaisePhone();   // 아직 들지 않았다면 켜기 전에 든다
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
        /// 휴대폰 빛. 카메라에 보이는 자리를 다 덮는 검은 판이다. 두 군데에 빛이 뚫린다.
        /// 차지한 둘레에는 잔잔한 둥근 빛, 마우스가 가리키는 자리에는 비춰 보는 둥근 빛.
        /// 구멍 둘이 서로 가리지 않게 판 하나에 함께 그린다. 판은 거칠게(1 칸이 0.1 단위쯤) 그리고 부드럽게 늘여 보여, 매 프레임 다시 그려도 가볍다.
        /// </summary>
        private void EnsurePhone()
        {
            if (_phone != null) return;
            const int H = 120;
            var cam = Camera.main;
            float aspect = cam != null ? cam.aspect : 16f / 9f;
            int w = Mathf.Max(16, Mathf.RoundToInt(H * aspect));
            _phoneTex = new Texture2D(w, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            _phonePixels = new Color32[w * H];

            var go = new GameObject("PhoneLight");
            go.transform.SetParent(transform, false);
            _phone = go.AddComponent<SpriteRenderer>();
            _phone.sprite = Sprite.Create(_phoneTex, new Rect(0, 0, w, H), new Vector2(0.5f, 0.5f), 1f);
            _phone.sortingOrder = _dark != null ? _dark.sortingOrder : 20;
            _phone.enabled = false;
        }

        private Texture2D _phoneTex;
        private Color32[] _phonePixels;

        /// <summary>판을 카메라에 맞추고, 차지한 둘레와 마우스 자리에 빛을 다시 뚫는다.</summary>
        private void FollowPhone()
        {
            if (_phone == null || _phoneTex == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            // 판은 카메라에 보이는 자리보다 조금 크다. 흔들려도 가장자리가 드러나지 않는다.
            const float Margin = 1.15f;
            float viewH = cam.orthographicSize * 2f * Margin;
            float viewW = viewH * cam.aspect;
            int w = _phoneTex.width, h = _phoneTex.height;
            var center = cam.transform.position;
            _phone.transform.position = new Vector3(center.x, center.y, _phone.transform.position.z);
            var parentScale = _phone.transform.parent != null ? _phone.transform.parent.lossyScale : Vector3.one;
            _phone.transform.localScale = new Vector3(viewW / w / parentScale.x, viewH / h / parentScale.y, 1f);

            Vector2 glow = _watcher != null ? (Vector2)_watcher.position + _lightOffset : new Vector2(9999f, 9999f);

            // 마우스가 카메라 화면 안에 있을 때만 그 자리를 비춘다. 대사 띠 위에 있으면 빛이 없다.
            Vector2 pointer = new Vector2(9999f, 9999f);
            var device = UnityEngine.InputSystem.Pointer.current;
            if (device != null)
            {
                Vector2 screen = device.position.ReadValue();
                if (cam.pixelRect.Contains(screen)) pointer = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            }

            float left = center.x - viewW * 0.5f, bottom = center.y - viewH * 0.5f;
            float stepX = viewW / w, stepY = viewH / h;
            for (int y = 0; y < h; y++)
            {
                float wy = bottom + (y + 0.5f) * stepY;
                for (int x = 0; x < w; x++)
                {
                    float wx = left + (x + 0.5f) * stepX;
                    float gx = (wx - glow.x) / _lightRadius.x, gy = (wy - glow.y) / _lightRadius.y;
                    float a = Hole(Mathf.Sqrt(gx * gx + gy * gy), _lightInnerAlpha);
                    float px = (wx - pointer.x) / _pointerRadius, py = (wy - pointer.y) / _pointerRadius;
                    a = Mathf.Min(a, Hole(Mathf.Sqrt(px * px + py * py), _pointerInnerAlpha));
                    _phonePixels[y * w + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            _phoneTex.SetPixels32(_phonePixels);
            _phoneTex.Apply(false);
        }

        /// <summary>둥근 빛 하나의 어둠. d 는 가운데에서 잰 거리를 반지름으로 나눈 값이다.</summary>
        private float Hole(float d, float inner)
        {
            return Mathf.Lerp(inner, _blackAlpha, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 1f, d)));
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
