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

        [Tooltip("한영이 문 앞에 서는 자리(방 기준 좌우). 걸을 수 있는 오른쪽 끝이다.")]
        [SerializeField] private float _doorInsideX = 12f;

        [Tooltip("문을 지나 사라지는 자리. 문 한가운데쯤이다.")]
        [SerializeField] private float _doorOutsideX = 13.3f;

        [SerializeField] private float _exitWalkSpeed = 2.6f;

        [Header("조명")]
        [SerializeField] private Light2D _roomLight;
        [SerializeField] private Light2D _moonLight;
        [SerializeField] private float _lightOnIntensity = 1f;
        [SerializeField] private float _lightOffIntensity = 0.22f;
        [SerializeField] private Color _lightOffColor = new Color(0.62f, 0.68f, 0.95f);

        [Header("잠자리")]
        [Tooltip("침대 발치. 차지한이 여기까지 걸어와 눕는다.")]
        [SerializeField] private float _bedFootX = -6.4f;

        [Tooltip("누운 자리(방 기준). 발이 이 자리에 놓이고 머리는 베개 쪽으로 간다.")]
        [SerializeField] private Vector2 _lyingPosition = new Vector2(-9.2f, -1.6f);

        [Tooltip("누운 몸 위로 덮는 이불. 누울 때 켜진다.")]
        [SerializeField] private GameObject _blanketOver;

        public bool IsLightOn { get; private set; } = true;

        private Vector3[] _eyeScales;
        private Vector3 _chajihanScale;
        private SpriteRenderer[] _hanyoungRenderers;
        private Color[] _hanyoungColors;

        private void Awake()
        {
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
            SetDoorOpen(false);
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
            SetDoorOpen(false);

            if (_hanyoung != null)
            {
                _hanyoung.gameObject.SetActive(true);
                SetHanyoungAlpha(1f);
            }

            if (_chajihan != null)
            {
                _chajihan.transform.localRotation = Quaternion.identity;
                _chajihan.transform.localScale = _chajihanScale;
            }

            SetEyes(1f);
            if (_blanketOver != null) _blanketOver.SetActive(false);
        }

        // ------------------------------------------------------------- 조명

        /// <summary>불을 켜거나 끈다. 끄면 방이 어두워지고 창으로 달빛만 들어온다.</summary>
        public void ToggleLight()
        {
            IsLightOn = !IsLightOn;
            ApplyLight();
            Debug.Log("[RoomNight] 방 조명 " + (IsLightOn ? "켬" : "끔"));
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
            _hanyoung.WalkTo(_doorInsideX, _exitWalkSpeed, () => StartCoroutine(ExitThroughDoor(onDone)));
            Debug.Log("[RoomNight] 한영이 문으로 간다");
        }

        private IEnumerator ExitThroughDoor(Action onDone)
        {
            yield return new WaitForSeconds(0.35f);   // 문고리를 잡는 틈
            SetDoorOpen(true);
            yield return new WaitForSeconds(0.3f);

            // 문간을 지나며 옅어진다. 문 너머 어둠으로 들어가는 것처럼 보인다.
            var t = _hanyoung.transform;
            float startX = t.localPosition.x;
            const float Duration = 0.9f;
            for (float e = 0f; e < Duration; e += Time.deltaTime)
            {
                float k = e / Duration;
                var p = t.localPosition;
                p.x = Mathf.Lerp(startX, _doorOutsideX, k);
                t.localPosition = p;
                SetHanyoungAlpha(1f - k);
                yield return null;
            }

            SetHanyoungAlpha(0f);
            _hanyoung.gameObject.SetActive(false);

            yield return new WaitForSeconds(0.35f);
            SetDoorOpen(false);
            yield return new WaitForSeconds(0.4f);

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
            _chajihan.WalkTo(_bedFootX, () => StartCoroutine(LieDown(onDone)), 0.6f);
            Debug.Log("[RoomNight] 차지한이 침대로 간다");
        }

        private IEnumerator LieDown(Action onDone)
        {
            var t = _chajihan.transform;
            yield return new WaitForSeconds(0.4f);

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

            if (_blanketOver != null) _blanketOver.SetActive(true);
            Debug.Log("[RoomNight] 누웠다");

            // 눈을 뜬 채 잠시 있다가, 몇 번 깜빡이고, 점점 무겁게 감긴다.
            yield return new WaitForSeconds(1.2f);
            yield return Blink(0.09f, 0.12f);
            yield return new WaitForSeconds(1.4f);
            yield return Blink(0.1f, 0.14f);
            yield return new WaitForSeconds(0.9f);
            yield return Blink(0.18f, 0.3f);           // 한 번 느리게. 졸음이 오기 시작한다
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

        private float _eyeOpen = 1f;

        private void SetEyes(float open)
        {
            _eyeOpen = open;
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
