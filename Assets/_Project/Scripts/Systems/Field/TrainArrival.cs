using System;
using System.Collections;
using UnityEngine;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 승강장으로 열차가 들어오는 장면.
    ///
    /// 열차는 오른쪽 어둠에서 미끄러져 들어와 천천히 선다.
    /// 서고 나면 스크린도어와 열차 문이 함께 열리고, 그제야 탈 수 있는 자리가 켜진다.
    ///
    /// 여기서는 보이는 것만 다룬다. 실제로 타는 것은 그 자리를 눌렀을 때 CaseDirector 가 한다.
    /// 문이 열리기 전에는 탈 자리가 꺼져 있으므로 미리 눌러 넘어갈 수 없다.
    /// </summary>
    public class TrainArrival : MonoBehaviour
    {
        [Header("들어오는 열차")]
        [Tooltip("통째로 미끄러져 들어오는 열차. 이 아래에 차체와 문이 들어간다.")]
        [SerializeField] private Transform _train;

        [Tooltip("열차가 나타나기 시작하는 x. 화면 오른쪽 바깥이다.")]
        [SerializeField] private float _enterFrom = 46f;

        [Tooltip("들어와서 설 때까지 걸리는 시간(초).")]
        [SerializeField] private float _enterSeconds = 2.6f;

        [Tooltip("서고 나서 문이 열리기까지 뜸을 들이는 시간(초).")]
        [SerializeField] private float _settleSeconds = 0.45f;

        [Header("문")]
        [Tooltip("왼쪽으로 물러나는 문짝. 스크린도어와 열차 문을 함께 넣는다.")]
        [SerializeField] private Transform[] _leftLeaves;

        [Tooltip("오른쪽으로 물러나는 문짝.")]
        [SerializeField] private Transform[] _rightLeaves;

        [Tooltip("문짝이 옆으로 물러나는 거리.")]
        [SerializeField] private float _doorSlide = 2.3f;

        [Tooltip("문이 다 열리는 데 걸리는 시간(초).")]
        [SerializeField] private float _doorSeconds = 0.9f;

        [Header("탈 자리")]
        [Tooltip("문이 열린 뒤에만 켜지는 조사 지점. 이것을 눌러 열차에 탄다.")]
        [SerializeField] private GameObject _boardingPoint;

        /// <summary>문이 열려 탈 수 있는 상태인가.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>문이 다 열렸을 때. 위쪽 한 줄을 바꾸는 데 쓴다.</summary>
        public event Action Opened;

        private Vector3[] _leftClosed;
        private Vector3[] _rightClosed;
        private Coroutine _running;

        private void Awake()
        {
            _leftClosed = Capture(_leftLeaves);
            _rightClosed = Capture(_rightLeaves);
            ResetToWaiting();
        }

        private void OnEnable()
        {
            // 현장을 닫았다 다시 열어도 마지막 상태를 그대로 보여준다.
            if (!IsOpen && _running == null) ResetToWaiting();
        }

        private static Vector3[] Capture(Transform[] leaves)
        {
            if (leaves == null) return Array.Empty<Vector3>();

            var positions = new Vector3[leaves.Length];
            for (int i = 0; i < leaves.Length; i++)
            {
                if (leaves[i] != null) positions[i] = leaves[i].localPosition;
            }
            return positions;
        }

        /// <summary>열차가 아직 오지 않은 상태로 되돌린다.</summary>
        public void ResetToWaiting()
        {
            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
            }

            IsOpen = false;
            if (_train != null) _train.localPosition = new Vector3(_enterFrom, 0f, 0f);
            ApplyDoors(0f);
            if (_boardingPoint != null) _boardingPoint.SetActive(false);
        }

        /// <summary>열차를 불러들인다. 이미 들어왔으면 아무 일도 없다.</summary>
        public void Play()
        {
            if (IsOpen || _running != null || !isActiveAndEnabled) return;
            _running = StartCoroutine(Run());
        }

        /// <summary>장면을 건너뛰고 문이 열린 상태로 만든다. 튜토리얼을 이미 본 경우에 쓴다.</summary>
        public void SkipToOpen()
        {
            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
            }

            if (_train != null) _train.localPosition = Vector3.zero;
            ApplyDoors(1f);
            if (_boardingPoint != null) _boardingPoint.SetActive(true);

            IsOpen = true;
            Opened?.Invoke();
        }

        private IEnumerator Run()
        {
            // 들어오는 동안. 끝에서 천천히 서게 뒤로 갈수록 느려지는 곡선을 쓴다.
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, _enterSeconds);
                float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
                if (_train != null) _train.localPosition = new Vector3(Mathf.Lerp(_enterFrom, 0f, eased), 0f, 0f);
                yield return null;
            }

            if (_train != null) _train.localPosition = Vector3.zero;

            // 서고 나서 한 박자 쉰다. 곧바로 열리면 선 것이 눈에 들어오지 않는다.
            yield return new WaitForSeconds(_settleSeconds);

            float d = 0f;
            while (d < 1f)
            {
                d += Time.deltaTime / Mathf.Max(0.01f, _doorSeconds);
                ApplyDoors(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(d)));
                yield return null;
            }

            ApplyDoors(1f);
            if (_boardingPoint != null) _boardingPoint.SetActive(true);

            IsOpen = true;
            _running = null;
            Opened?.Invoke();

            Debug.Log("[TrainArrival] 열차가 섰고 문이 열렸다");
        }

        /// <summary>문이 열린 정도. 0 이면 닫힘, 1 이면 활짝.</summary>
        private void ApplyDoors(float amount)
        {
            Slide(_leftLeaves, _leftClosed, -_doorSlide * amount);
            Slide(_rightLeaves, _rightClosed, _doorSlide * amount);
        }

        private static void Slide(Transform[] leaves, Vector3[] closed, float offset)
        {
            if (leaves == null || closed == null) return;

            for (int i = 0; i < leaves.Length && i < closed.Length; i++)
            {
                if (leaves[i] == null) continue;
                leaves[i].localPosition = closed[i] + new Vector3(offset, 0f, 0f);
            }
        }
    }
}
