using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UrbanLegendBureau.Core;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 사건 #002 엘리베이터 안의 움직이는 것들. 층 표시, 안쪽 문 두 짝, 문 너머 3층 복도와 깜빡이는 복도 등.
    ///
    /// 무엇을 언제 하는지는 진행 담당(CaseDirector)이 정한다. 여기서는 시킨 대로 움직이기만 한다.
    ///   Ride      층 표시가 한 층씩 오른다. 층마다 짧게 똑딱 소리가 난다.
    ///   OpenDoors 띵 소리와 함께 문 두 짝이 양옆으로 열린다. 너머의 복도가 드러난다.
    ///   CloseDoors 문이 다시 닫힌다.
    /// 문 너머 복도는 문이 열려 있을 때만 보인다. 복도 등은 열려 있는 동안 불규칙하게 깜빡인다.
    /// </summary>
    public class ElevatorCar : MonoBehaviour
    {
        [SerializeField] private Transform _leftDoor;
        [SerializeField] private Transform _rightDoor;
        [Tooltip("문이 열리면 양옆으로 이만큼 물러난다.")]
        [SerializeField] private float _openDistance = 1.5f;
        [SerializeField] private TextMeshPro _floorText;
        [Tooltip("층 표시 옆의 오름 화살표. 오르는 동안만 켠다.")]
        [SerializeField] private GameObject _upArrow;
        [Tooltip("문 너머 복도. 문이 열려 있을 때만 켠다.")]
        [SerializeField] private GameObject _corridor;
        [Tooltip("복도 천장 등의 빛. 깜빡인다.")]
        [SerializeField] private SpriteRenderer _corridorLight;
        [Tooltip("엘리베이터 안 천장 등의 빛. 3층에 서는 동안 한 번 크게 흔들린다.")]
        [SerializeField] private SpriteRenderer _cabinLight;

        private Vector3 _leftClosed, _rightClosed;
        private float _corridorAlpha, _cabinAlpha;
        private Coroutine _flicker;
        private bool _captured;

        public int Floor { get; private set; } = 1;
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            Capture();
            ResetCar();
        }

        /// <summary>
        /// 처음 놓인 문 자리와 빛의 세기를 한 번만 적어 둔다.
        /// 칸이 꺼져 있어 Awake 가 아직 돌지 않았을 때 ResetCar 가 먼저 불려도 문이 제자리를 잃지 않게 한다.
        /// </summary>
        private void Capture()
        {
            if (_captured) return;
            _captured = true;
            if (_leftDoor != null) _leftClosed = _leftDoor.localPosition;
            if (_rightDoor != null) _rightClosed = _rightDoor.localPosition;
            if (_corridorLight != null) _corridorAlpha = _corridorLight.color.a;
            if (_cabinLight != null) _cabinAlpha = _cabinLight.color.a;
        }

        /// <summary>처음 모습. 1층, 문 닫힘, 복도 숨김.</summary>
        public void ResetCar()
        {
            Capture();
            StopAllCoroutines();
            _flicker = null;
            Floor = 1;
            IsOpen = false;
            if (_leftDoor != null) _leftDoor.localPosition = _leftClosed;
            if (_rightDoor != null) _rightDoor.localPosition = _rightClosed;
            if (_corridor != null) _corridor.SetActive(false);
            if (_upArrow != null) _upArrow.SetActive(false);
            SetAlpha(_cabinLight, _cabinAlpha);
            ShowFloor();
        }

        /// <summary>층 표시를 toFloor 까지 한 층씩 올린다. 다 오르면 onArrive.</summary>
        public void Ride(int toFloor, float secondsPerFloor, Action onArrive)
        {
            StartCoroutine(RideRoutine(toFloor, secondsPerFloor, onArrive));
        }

        private IEnumerator RideRoutine(int toFloor, float secondsPerFloor, Action onArrive)
        {
            if (_upArrow != null) _upArrow.SetActive(true);
            while (Floor < toFloor)
            {
                yield return new WaitForSeconds(secondsPerFloor);
                Floor++;
                ShowFloor();
                Sfx.Play("ui_tick", 0.35f, 0.8f);
            }
            if (_upArrow != null) _upArrow.SetActive(false);
            onArrive?.Invoke();
        }

        public void OpenDoors(Action onOpen)
        {
            StartCoroutine(MoveDoors(true, onOpen));
        }

        public void CloseDoors(Action onClosed)
        {
            StartCoroutine(MoveDoors(false, onClosed));
        }

        private IEnumerator MoveDoors(bool open, Action done)
        {
            if (open)
            {
                Sfx.Play("elevator_ding", 0.55f);
                yield return new WaitForSeconds(0.5f);
                if (_corridor != null) _corridor.SetActive(true);
                _flicker = StartCoroutine(Flicker());
            }
            Sfx.Play("door_slide", 0.5f, open ? 1.05f : 0.95f);

            const float Duration = 1.1f;
            float t = 0f;
            while (t < Duration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / Duration));
                float d = (open ? k : 1f - k) * _openDistance;
                if (_leftDoor != null) _leftDoor.localPosition = _leftClosed + Vector3.left * d;
                if (_rightDoor != null) _rightDoor.localPosition = _rightClosed + Vector3.right * d;
                yield return null;
            }

            IsOpen = open;
            if (!open)
            {
                if (_flicker != null) StopCoroutine(_flicker);
                _flicker = null;
                if (_corridor != null) _corridor.SetActive(false);
            }
            done?.Invoke();
        }

        /// <summary>복도 등이 지직거린다. 켜졌다 꺼졌다를 고르지 않은 간격으로 되풀이한다.</summary>
        private IEnumerator Flicker()
        {
            var rnd = new System.Random(3);
            while (true)
            {
                SetAlpha(_corridorLight, _corridorAlpha * (float)(0.15 + rnd.NextDouble() * 0.2));
                yield return new WaitForSeconds(0.05f + (float)rnd.NextDouble() * 0.12f);
                SetAlpha(_corridorLight, _corridorAlpha);
                if (rnd.NextDouble() < 0.3) Sfx.Play("lamp_flicker", 0.18f);
                yield return new WaitForSeconds(0.4f + (float)rnd.NextDouble() * 1.4f);
            }
        }

        /// <summary>엘리베이터 안 불빛이 잠깐 흔들린다. 3층 문이 열리기 직전에 쓴다.</summary>
        public void ShakeCabinLight()
        {
            StartCoroutine(ShakeLight());
        }

        private IEnumerator ShakeLight()
        {
            for (int i = 0; i < 4; i++)
            {
                SetAlpha(_cabinLight, _cabinAlpha * 0.25f);
                yield return new WaitForSeconds(0.06f);
                SetAlpha(_cabinLight, _cabinAlpha);
                yield return new WaitForSeconds(0.1f + i * 0.05f);
            }
        }

        private void ShowFloor()
        {
            if (_floorText != null) _floorText.text = Floor.ToString();
        }

        private static void SetAlpha(SpriteRenderer sr, float a)
        {
            if (sr == null) return;
            var c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }
}
