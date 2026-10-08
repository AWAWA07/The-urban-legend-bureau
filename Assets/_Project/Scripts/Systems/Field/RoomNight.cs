using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 숙소의 밤. 방에 붙어 있는 것들의 모습만 다룬다.
    ///
    ///   조명    - 방 전체를 비추는 전역 조명(Light2D)을 켜고 끈다. 끄면 창으로 들어오는 달빛만 남는다.
    ///   방문    - 열린 모습과 닫힌 모습을 갈아 끼운다.
    ///   한영    - 문까지 걸어가 문을 열고 나간다. 걷는 것은 기존 따라가기(FieldFollower)의 걸음이다.
    ///   차지한  - 침대까지 걸어가 눕고, 눈을 몇 번 깜빡이다 감는다. 걷는 것은 기존 걷기(FieldWalker)다.
    ///
    /// 무엇을 언제 할지는 CaseDirector 가 정한다. 여기서는 부르면 그 모습을 보여 주고 끝나면 알린다.
    /// </summary>
    public class RoomNight : MonoBehaviour
    {
        [Header("사람")]
        [SerializeField] private FieldWalker _chajihan;
        [SerializeField] private FieldFollower _hanyoung;

        [Tooltip("차지한의 두 눈. 깜빡임과 잠드는 모습에 쓴다.")]
        [SerializeField] private Transform[] _eyes;

        [Header("방문")]
        [SerializeField] private GameObject _doorClosed;
        [SerializeField] private GameObject _doorOpen;

        [Tooltip("한영이 문 앞에 서는 자리(방 기준 좌우). 문 한가운데다.")]
        [SerializeField] private float _doorInsideX = 12.8f;

        [Tooltip("문간으로 들어설 때 위로 오르는 만큼. 뒤쪽 벽의 문으로 들어가니 멀어지며 조금 올라간다.")]
        [SerializeField] private float _doorStepUp = 0.35f;

        [Tooltip("문간에 들어선 뒤의 크기 배율. 멀어지는 만큼 작아진다.")]
        [SerializeField] private float _doorStepScale = 0.86f;

        [SerializeField] private float _exitWalkSpeed = 3.6f;

        [Header("조명")]
        [SerializeField] private Light2D _roomLight;
        [SerializeField] private Light2D _moonLight;
        [SerializeField] private float _lightOnIntensity = 1f;
        [SerializeField] private float _lightOffIntensity = 0.22f;
        [SerializeField] private Color _lightOffColor = new Color(0.62f, 0.68f, 0.95f);

        [Tooltip("스위치의 토글. 켜면 위로, 끄면 아래로 딸깍 넘어간다.")]
        [SerializeField] private Transform _switchToggle;
        [SerializeField] private float _switchOnY = -0.27f;
        [SerializeField] private float _switchOffY = -0.43f;

        [Header("잠자리")]
        [Tooltip("침대 발치. 차지한이 여기까지 걸어와 눕는다.")]
        [SerializeField] private float _bedFootX = -6.4f;

        [Tooltip("누운 자리(방 기준). 발이 이 자리에 놓이고 머리는 베개 쪽으로 간다.")]
        [SerializeField] private Vector2 _lyingPosition = new Vector2(-8.2f, -1.4f);

        [Tooltip("눕는 그림을 쓸 때 누운 몸의 아래 가운데 자리(방 기준). 매트리스 위, 머리가 베개(x -11.45)에 오는 자리다.")]
        [SerializeField] private Vector2 _lyingArtPosition = new Vector2(-10.45f, -2.1f);

        [Tooltip("누운 몸 위로 덮는 이불. 누울 때 켜진다.")]
        [SerializeField] private GameObject _blanketOver;

        [Header("옷걸이")]
        [Tooltip("옷걸이에 걸린 코트. 평소에는 꺼 두고, 차지한이 코트를 거는 순간 켠다.")]
        [SerializeField] private GameObject _coatHung;

        [Tooltip("코트를 걸 때 차지한이 서는 자리(방 기준 좌우). 옷걸이 오른쪽, 팔을 뻗으면 고리에 닿는 곳이다.")]
        [SerializeField] private float _coatStandX = -11.5f;

        [Tooltip("침대 끝에 걸터앉을 때 몸이 올라가는 높이. 침대가 바닥보다 높아 앉으면 발이 살짝 뜬다.")]
        [SerializeField] private float _bedSitLift = 1.0f;

        [Tooltip("코트 벗기, 걸기, 넥타이, 앉기 그림 한 칸을 보여 주는 시간(초).")]
        [SerializeField] private float _undressFrameSeconds = 0.24f;

        [Header("눈꺼풀")]
        [Tooltip("눈 모양으로 뚫린 검은 판. 위아래로 납작해지며 감긴다. 위아래 바깥은 자식 판이 덮는다.")]
        [SerializeField] private RectTransform _eyeMask;

        [Tooltip("눈꺼풀이 거의 닫힐 즈음 화면 전체를 덮는 검은 판. 틈 없이 까맣게 끝난다.")]
        [SerializeField] private CanvasGroup _lidFade;

        /// <summary>졸음이 온 뒤로는 화면도 차지한의 눈을 따라 감긴다.</summary>
        private bool _lidsFollowEyes;

        public bool IsLightOn { get; private set; } = true;

        private Vector3[] _eyeScales;
        private Vector3 _chajihanScale;
        private SpriteRenderer[] _hanyoungRenderers;
        private Color[] _hanyoungColors;

        private void Awake()
        {
            BuildEyeSprite();
            if (_eyes != null)
            {
                _eyeScales = new Vector3[_eyes.Length];
                for (int i = 0; i < _eyes.Length; i++) if (_eyes[i] != null) _eyeScales[i] = _eyes[i].localScale;
            }

            if (_chajihan != null) _chajihanScale = _chajihan.transform.localScale;

            if (_hanyoung != null)
            {
                _hanyoungRenderers = _hanyoung.GetComponentsInChildren<SpriteRenderer>(true);
                _hanyoungColors = new Color[_hanyoungRenderers.Length];
                for (int i = 0; i < _hanyoungRenderers.Length; i++) _hanyoungColors[i] = _hanyoungRenderers[i].color;
            }

            ApplyLight();
            PlaceSwitch();
            SetDoorOpen(false);
            SetLids(0f);
        }

        /// <summary>
        /// 방을 처음 모습으로 되돌린다. 불은 켜져 있고, 문은 닫혀 있고, 한영은 방 안에 있고, 차지한은 서 있다.
        /// 숙소에 새로 들어설 때 부른다.
        /// </summary>
        public void ResetRoom()
        {
            StopAllCoroutines();

            IsLightOn = true;
            ApplyLight();
            PlaceSwitch();
            SetDoorOpen(false);

            if (_hanyoung != null)
            {
                _hanyoung.gameObject.SetActive(true);
                _hanyoung.Following = true;
                SetHanyoungAlpha(1f);
            }

            if (_chajihan != null)
            {
                _chajihan.transform.localRotation = Quaternion.identity;
                _chajihan.transform.localScale = _chajihanScale;
            }

            SetEyes(1f);
            if (_blanketOver != null) _blanketOver.SetActive(false);
            _sleepLook = null;
            if (_coatHung != null) _coatHung.SetActive(false);
            var look = _chajihan != null ? _chajihan.GetComponent<FieldSpriteAnimator>() : null;
            if (look != null) look.Release();
            _lidsFollowEyes = false;
            SetLids(0f);
            SetShadow(true);
        }

        // ------------------------------------------------------------- 조명

        /// <summary>불을 켜거나 끈다. 끄면 방이 어두워지고 창으로 달빛만 들어온다.</summary>
        public void ToggleLight()
        {
            IsLightOn = !IsLightOn;
            ApplyLight();
            if (_switchToggle != null) StartCoroutine(FlipSwitch(IsLightOn ? _switchOnY : _switchOffY));
            Debug.Log("[RoomNight] 방 조명 " + (IsLightOn ? "켬" : "끔"));
        }

        /// <summary>
        /// 스위치를 누르는 모습. 토글이 한 번 눌려 들어갔다가 반대쪽으로 넘어간다. 아주 짧다.
        /// </summary>
        private IEnumerator FlipSwitch(float targetY)
        {
            var t = _switchToggle;
            var baseScale = new Vector3(Mathf.Abs(t.localScale.x), Mathf.Abs(t.localScale.y), 1f);
            float fromY = t.localPosition.y;

            const float Seconds = 0.12f;
            for (float e = 0f; e < Seconds; e += Time.deltaTime)
            {
                float k = e / Seconds;
                var p = t.localPosition;
                p.y = Mathf.Lerp(fromY, targetY, k * k);
                t.localPosition = p;

                // 누르는 순간 살짝 찌그러졌다 돌아온다. 눌린 느낌이 난다.
                float squash = 1f - 0.25f * Mathf.Sin(k * Mathf.PI);
                t.localScale = new Vector3(baseScale.x, baseScale.y * squash, 1f);
                yield return null;
            }

            var end = t.localPosition;
            end.y = targetY;
            t.localPosition = end;
            t.localScale = baseScale;
        }

        /// <summary>토글을 지금 상태의 자리에 곧바로 놓는다. 움직임 없이.</summary>
        private void PlaceSwitch()
        {
            if (_switchToggle == null) return;
            var p = _switchToggle.localPosition;
            p.y = IsLightOn ? _switchOnY : _switchOffY;
            _switchToggle.localPosition = p;
        }

        private void ApplyLight()
        {
            if (_roomLight != null)
            {
                _roomLight.intensity = IsLightOn ? _lightOnIntensity : _lightOffIntensity;
                _roomLight.color = IsLightOn ? Color.white : _lightOffColor;
            }

            if (_moonLight != null) _moonLight.gameObject.SetActive(!IsLightOn);
        }

        private void SetDoorOpen(bool open)
        {
            if (_doorClosed != null) _doorClosed.SetActive(!open);
            if (_doorOpen != null) _doorOpen.SetActive(open);
        }

        // ------------------------------------------------------------- 한영이 나간다

        /// <summary>
        /// 차지한은 그 자리에 세워 두고 한영만 문으로 걸어가 나간다. 다 나가면 onDone 을 부른다.
        /// 문 앞에서 문을 열고, 문간을 지나며 옅어져 사라진 뒤 문이 닫힌다.
        /// </summary>
        public void PlayHanyoungExit(Action onDone)
        {
            if (_hanyoung == null) { onDone?.Invoke(); return; }

            if (_chajihan != null) _chajihan.Locked = true;
            _hanyoung.Following = false;   // 문 앞에서 서 있는 동안 차지한 쪽으로 되돌아오지 않게
            _hanyoung.WalkTo(_doorInsideX, _exitWalkSpeed, () => StartCoroutine(ExitThroughDoor(onDone)));
            Debug.Log("[RoomNight] 한영이 문으로 간다");
        }

        private IEnumerator ExitThroughDoor(Action onDone)
        {
            yield return new WaitForSeconds(0.2f);   // 문고리를 잡는 틈
            SetDoorOpen(true);
            yield return new WaitForSeconds(0.15f);

            // 문은 뒤쪽 벽에 있다. 옆으로 미끄러지지 않고 안쪽으로 들어선다.
            // 조금 올라가고 작아지며 옅어진다. 문 너머 어둠 속으로 멀어지는 것처럼 보인다.
            // 문을 향해 돌아선다. 뒷모습 그림이 있으면 그것으로 걸어 들어간다.
            var look = _hanyoung.GetComponent<FieldSpriteAnimator>();
            if (look != null) look.ShowBack(true);

            var t = _hanyoung.transform;
            Vector3 fromPos = t.localPosition;
            Vector3 toPos = fromPos + new Vector3(0f, _doorStepUp, 0f);
            Vector3 fromScale = t.localScale;
            const float Duration = 0.5f;
            for (float e = 0f; e < Duration; e += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, e / Duration);
                t.localPosition = Vector3.Lerp(fromPos, toPos, k);
                t.localScale = fromScale * Mathf.Lerp(1f, _doorStepScale, k);
                SetHanyoungAlpha(1f - k);
                yield return null;
            }

            SetHanyoungAlpha(0f);
            t.localPosition = fromPos;     // 다음에 다시 켤 때를 위해 자리와 크기를 되돌려 둔다
            t.localScale = fromScale;
            if (look != null) look.ShowBack(false);
            _hanyoung.gameObject.SetActive(false);

            yield return new WaitForSeconds(0.25f);
            SetDoorOpen(false);
            yield return new WaitForSeconds(0.25f);

            if (_chajihan != null) _chajihan.Locked = false;
            Debug.Log("[RoomNight] 한영이 방을 나갔다");
            onDone?.Invoke();
        }

        private void SetHanyoungAlpha(float alpha)
        {
            if (_hanyoungRenderers == null) return;
            for (int i = 0; i < _hanyoungRenderers.Length; i++)
            {
                if (_hanyoungRenderers[i] == null) continue;
                var c = _hanyoungColors[i];
                c.a *= alpha;
                _hanyoungRenderers[i].color = c;
            }
        }

        // ------------------------------------------------------------- 잠든다

        /// <summary>
        /// 차지한이 침대로 가서 눕고 잠든다. 다 잠들면 onDone 을 부른다.
        /// 걸어가 → 눕고 → 몇 번 깜빡이다 → 천천히 눈을 감는다. 그 뒤로는 움직이지 않는다.
        /// </summary>
        public void PlaySleep(Action onDone)
        {
            if (_chajihan == null) { onDone?.Invoke(); return; }

            _chajihan.Locked = true;

            // 코트를 벗는 그림이 있으면 옷걸이 앞으로 가서 벗어 걸고, 넥타이를 풀고, 침대 끝에 앉았다가 눕는다.
            // 그림이 없으면 예전처럼 침대 발치로 가서 곧바로 눕는다.
            var look = _chajihan.GetComponent<FieldSpriteAnimator>();
            if (look != null && look.FrameCount("coatoff") > 0)
            {
                _chajihan.WalkTo(_coatStandX, () => StartCoroutine(Undress(look, onDone)), 0.6f);
                Debug.Log("[RoomNight] 차지한이 옷걸이로 간다");
                return;
            }

            _chajihan.WalkTo(_bedFootX, () => StartCoroutine(LieDown(onDone)), 0.6f);
            Debug.Log("[RoomNight] 차지한이 침대로 간다");
        }

        /// <summary>
        /// 자기 전 차림을 푼다. 코트를 벗어 옷걸이에 걸고, 넥타이를 풀고, 침대 끝에 걸터앉는다. 그다음 눕는다.
        /// 옷걸이는 차지한의 왼쪽에 있다. 걸어오며 왼쪽을 보고 섰으니 그대로 돌아서지 않고 건다.
        /// </summary>
        private IEnumerator Undress(FieldSpriteAnimator look, Action onDone)
        {
            yield return new WaitForSeconds(0.3f);

            yield return PlayClip(look, "coatoff", _undressFrameSeconds, null);

            // 마지막 칸에서 손을 뗀다. 그 순간 코트가 옷걸이에 걸린다.
            int hangLast = look.FrameCount("coathang") - 1;
            yield return PlayClip(look, "coathang", _undressFrameSeconds * 1.2f, i =>
            {
                if (i == hangLast && _coatHung != null) _coatHung.SetActive(true);
            });
            if (_coatHung != null) _coatHung.SetActive(true);

            yield return new WaitForSeconds(0.2f);
            yield return PlayClip(look, "tie", _undressFrameSeconds * 1.4f, null);
            yield return new WaitForSeconds(0.3f);

            // 침대 끝에 걸터앉는다. 침대가 높아서 앉으며 몸이 올라간다. 발밑 그림자는 바닥에 남지 않게 끈다.
            int sitCount = look.FrameCount("sit");
            if (sitCount > 0)
            {
                var t = _chajihan.transform;
                Vector3 from = t.localPosition;
                Vector3 to = from + new Vector3(0.25f, _bedSitLift, 0f);
                // 침대 쪽으로 돌아앉는다. 앉기와 눕기 그림은 오른쪽을 보고 그려져 있어 뒤집지 않는다.
                t.localScale = new Vector3(Mathf.Abs(_chajihanScale.x), _chajihanScale.y, _chajihanScale.z);
                // 첫 칸은 제자리에서 무릎을 굽힌다. 다음 칸에서 엉덩이를 침대 끝에 걸치며 몸이 올라가고,
                // 마지막 칸에서 털썩 내려앉는다. 오르는 동안 둥글게 솟았다가 살짝 가라앉아 앉는 무게가 느껴지게 한다.
                float seconds = _undressFrameSeconds * 1.3f;
                look.Hold("sit", 0);
                yield return new WaitForSeconds(seconds);

                if (sitCount > 1) look.Hold("sit", 1);
                SetShadow(false);
                const float RiseSeconds = 0.32f;
                for (float e = 0f; e < RiseSeconds; e += Time.deltaTime)
                {
                    float k = e / RiseSeconds;
                    float ease = 1f - (1f - k) * (1f - k);                       // 빨리 올라가다 느려진다
                    float over = Mathf.Sin(k * Mathf.PI) * 0.12f;                // 살짝 더 솟는다
                    t.localPosition = Vector3.Lerp(from, to, ease) + new Vector3(0f, over, 0f);
                    if (k > 0.5f && sitCount > 2) look.Hold("sit", 2);
                    yield return null;
                }
                t.localPosition = to + new Vector3(0f, 0.04f, 0f);
                look.Hold("sit", sitCount - 1);
                yield return new WaitForSeconds(0.08f);
                t.localPosition = to;                                             // 털썩
                yield return new WaitForSeconds(0.9f);
            }

            // 눕는 그림이 있으면 그것으로 침대에 몸을 뉘인다. 엉덩이 자리에서 누운 자리로 조금씩 옮긴다.
            int lieCount = look.FrameCount("lie");
            if (lieCount > 0)
            {
                var t = _chajihan.transform;
                t.localScale = new Vector3(Mathf.Abs(_chajihanScale.x), _chajihanScale.y, _chajihanScale.z);
                Vector3 from = t.localPosition;
                Vector3 to = new Vector3(_lyingArtPosition.x, _lyingArtPosition.y, from.z);
                for (int i = 0; i < lieCount; i++)
                {
                    look.Hold("lie", i);
                    Vector3 a = Vector3.Lerp(from, to, (float)i / lieCount);
                    Vector3 b = Vector3.Lerp(from, to, (float)(i + 1) / lieCount);
                    float seconds = _undressFrameSeconds * 1.6f;
                    for (float e = 0f; e < seconds; e += Time.deltaTime)
                    {
                        t.localPosition = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, e / seconds));
                        yield return null;
                    }
                    t.localPosition = b;
                }
                if (look.FrameCount("sleep") > 0) { _sleepLook = look; SetEyes(1f); }
                yield return LieDown(onDone, false);
                yield break;
            }

            // 눕는 그림이 없다. 코트를 벗은 서 있는 그림(넥타이를 푼 마지막 칸)을 눕힌다.
            look.Hold("tie", look.FrameCount("tie") - 1);
            yield return LieDown(onDone);
        }

        /// <summary>그림 묶음을 처음부터 끝까지 한 칸씩 보여 준다. 칸이 바뀔 때마다 onFrame 에 칸 번호를 넘긴다.</summary>
        private IEnumerator PlayClip(FieldSpriteAnimator look, string clip, float seconds, Action<int> onFrame)
        {
            int count = look.FrameCount(clip);
            for (int i = 0; i < count; i++)
            {
                look.Hold(clip, i);
                onFrame?.Invoke(i);
                yield return new WaitForSeconds(seconds);
            }
        }

        private IEnumerator LieDown(Action onDone, bool rotate = true)
        {
            var t = _chajihan.transform;
            yield return new WaitForSeconds(rotate ? 0.4f : 0f);
            if (rotate)
            {

            // 눕는다. 머리가 베개 쪽(왼쪽)으로 가도록 반 바퀴의 절반만 돈다. 얼굴은 천장을 본다.
            Vector3 fromPos = t.localPosition;
            Vector3 toPos = new Vector3(_lyingPosition.x, _lyingPosition.y, fromPos.z);
            var up = new Vector3(Mathf.Abs(_chajihanScale.x), _chajihanScale.y, _chajihanScale.z);
            t.localScale = up;

            const float LieSeconds = 0.9f;
            for (float e = 0f; e < LieSeconds; e += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, e / LieSeconds);
                t.localPosition = Vector3.Lerp(fromPos, toPos, k);
                t.localRotation = Quaternion.Euler(0f, 0f, 90f * k);
                yield return null;
            }
            t.localPosition = toPos;
            t.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            if (_blanketOver != null) _blanketOver.SetActive(true);
            SetShadow(false);   // 누우면 발밑 그림자가 허공에 뜬다
            Debug.Log("[RoomNight] 누웠다");

            // 눈을 뜬 채 잠시 있다가, 몇 번 깜빡이고, 점점 무겁게 감긴다.
            yield return new WaitForSeconds(1.2f);
            yield return Blink(0.09f, 0.12f);
            yield return new WaitForSeconds(1.4f);
            yield return Blink(0.1f, 0.14f);
            yield return new WaitForSeconds(0.9f);
            yield return Blink(0.18f, 0.3f);           // 한 번 느리게. 졸음이 오기 시작한다
            _lidsFollowEyes = true;                    // 여기서부터 화면도 함께 감긴다
            yield return EyesTo(0.5f, 0.6f);           // 반쯤 감긴다
            yield return new WaitForSeconds(0.9f);
            yield return EyesTo(0.85f, 0.35f);          // 억지로 한 번 뜬다
            yield return new WaitForSeconds(0.6f);
            yield return EyesTo(0.08f, 1.6f);           // 천천히 감는다

            Debug.Log("[RoomNight] 잠들었다");
            onDone?.Invoke();
        }

        /// <summary>눈을 감았다 뜬다. 감는 데와 뜨는 데 드는 시간을 따로 준다.</summary>
        private IEnumerator Blink(float close, float open)
        {
            yield return EyesTo(0.08f, close);
            yield return EyesTo(1f, open);
        }

        /// <summary>눈을 그만큼 뜬 상태로 천천히 옮긴다. 1 이 다 뜬 것, 0 에 가까울수록 감긴 것이다.</summary>
        private IEnumerator EyesTo(float target, float seconds)
        {
            float from = _eyeOpen;
            for (float e = 0f; e < seconds; e += Time.deltaTime)
            {
                SetEyes(Mathf.Lerp(from, target, e / seconds));
                yield return null;
            }
            SetEyes(target);
        }

        /// <summary>
        /// 화면의 눈꺼풀을 그만큼 닫는다. 0 이면 다 뜬 것, 1 이면 다 감은 것이다.
        /// 위아래 판이 가운데로 다가오고, 거의 닫힐 즈음 화면 전체가 까매진다.
        /// </summary>
        private void SetLids(float closed)
        {
            closed = Mathf.Clamp01(closed);

            // 눈 모양 구멍이 위아래로 납작해진다. 처음에는 눈꼬리 쪽 가장자리만 어두워지고,
            // 다 감기면 가는 선 하나만 남았다가 사라진다.
            if (_eyeMask != null)
            {
                _eyeMask.gameObject.SetActive(closed > 0f);
                var screen = _eyeMask.parent as RectTransform;
                float h = screen != null ? screen.rect.height : Screen.height;
                float w = screen != null ? screen.rect.width : Screen.width;
                float lid = Mathf.SmoothStep(0f, 1f, closed);
                _eyeMask.sizeDelta = new Vector2(w * EyeWidth, h * Mathf.Lerp(EyeOpenHeight, 0f, lid));
            }

            if (_lidFade != null) _lidFade.alpha = Mathf.InverseLerp(0.75f, 1f, closed);
        }

        /// <summary>눈 구멍의 폭(화면 폭에 대한 배율). 눈꼬리가 화면 양 끝 조금 안쪽에 온다.</summary>
        private const float EyeWidth = 1.12f;

        /// <summary>다 떴을 때 구멍의 높이(화면 높이에 대한 배율). 눈꼬리 쪽까지 화면이 다 보일 만큼 크다.</summary>
        private const float EyeOpenHeight = 5f;

        /// <summary>
        /// 눈 모양 구멍을 그린다. 가운데가 넓고 양 끝이 뾰족한 아몬드 꼴이다. 구멍 밖은 검다.
        /// 그림 파일을 따로 두지 않고 처음 한 번 만든다.
        /// </summary>
        private void BuildEyeSprite()
        {
            if (_eyeMask == null) return;
            var image = _eyeMask.GetComponent<UnityEngine.UI.Image>();
            if (image == null) return;

            const int W = 512, H = 256;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = (y + 0.5f) / H * 2f - 1f;
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W * 2f - 1f;
                    float half = 1f - u * u;                          // 눈꺼풀의 곡선. 가운데가 가장 높다
                    float edge = Mathf.Abs(v) - half;                 // 0 보다 작으면 구멍 안
                    float a = Mathf.Clamp01(edge / 0.06f + 0.5f);     // 가장자리를 살짝 흐린다
                    pixels[y * W + x] = new Color32(0, 0, 0, (byte)(a * 255));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            image.sprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f));
            image.type = UnityEngine.UI.Image.Type.Simple;
            image.preserveAspect = false;
        }

        private void SetShadow(bool on)
        {
            var shadow = _chajihan != null ? _chajihan.transform.Find("Shadow") : null;
            if (shadow != null) shadow.gameObject.SetActive(on);
        }

        private float _eyeOpen = 1f;

        /// <summary>잠들기 그림으로 눈을 감기는 차지한. 누운 그림으로 누웠을 때만 있다.</summary>
        private FieldSpriteAnimator _sleepLook;

        private void SetEyes(float open)
        {
            _eyeOpen = open;
            if (_lidsFollowEyes) SetLids(Mathf.InverseLerp(1f, 0.08f, open));

            // 누운 그림이 있으면 눈 뜬 정도에 맞는 잠들기 칸을 고른다. 0 이 뜬 눈, 마지막이 감은 눈이다.
            if (_sleepLook != null)
            {
                int count = _sleepLook.FrameCount("sleep");
                if (count > 0)
                {
                    float closed = Mathf.InverseLerp(1f, 0.08f, open);
                    _sleepLook.Hold("sleep", Mathf.Min(count - 1, Mathf.FloorToInt(closed * count)));
                }
            }
            if (_eyes == null || _eyeScales == null) return;

            for (int i = 0; i < _eyes.Length; i++)
            {
                if (_eyes[i] == null) continue;
                var s = _eyeScales[i];
                _eyes[i].localScale = new Vector3(s.x, s.y * open, s.z);
            }
        }
    }
}
