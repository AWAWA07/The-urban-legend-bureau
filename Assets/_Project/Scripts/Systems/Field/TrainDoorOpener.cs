using System.Collections;
using UnityEngine;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 열차 안 문 하나를 여닫는다. 문짝 둘(Leaf_0, Leaf_1)과 그 창(LeafGlass_0, LeafGlass_1)이 양옆으로 미끄러진다.
    /// 종점에서 내릴 때 가운데 문을 연다. 처음 붙을 때 닫힌 자리를 기억해 두고, 사건을 다시 시작하면 닫는다.
    /// </summary>
    public class TrainDoorOpener : MonoBehaviour
    {
        private const float Slide = 1.9f;

        private Transform[] _left;
        private Transform[] _right;
        private Vector3[] _leftClosed;
        private Vector3[] _rightClosed;
        private Coroutine _running;

        private void Awake() => Capture();

        private void Capture()
        {
            if (_left != null) return;
            _left = new[] { transform.Find("Leaf_0"), transform.Find("LeafGlass_0") };
            _right = new[] { transform.Find("Leaf_1"), transform.Find("LeafGlass_1") };
            _leftClosed = Positions(_left);
            _rightClosed = Positions(_right);
        }

        private static Vector3[] Positions(Transform[] items)
        {
            var list = new Vector3[items.Length];
            for (int i = 0; i < items.Length; i++) if (items[i] != null) list[i] = items[i].localPosition;
            return list;
        }

        public void Open(float seconds)
        {
            Capture();
            if (_running != null) StopCoroutine(_running);
            if (!isActiveAndEnabled) { Apply(1f); return; }
            _running = StartCoroutine(Run(seconds));
        }

        public void Close()
        {
            Capture();
            if (_running != null) StopCoroutine(_running);
            _running = null;
            Apply(0f);
        }

        private IEnumerator Run(float seconds)
        {
            UrbanLegendBureau.Core.Sfx.Play("door_slide", 0.55f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, seconds))
            {
                Apply(Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            Apply(1f);
            _running = null;
        }

        private void Apply(float amount)
        {
            for (int i = 0; i < _left.Length; i++) if (_left[i] != null) _left[i].localPosition = _leftClosed[i] + new Vector3(-Slide * amount, 0f, 0f);
            for (int i = 0; i < _right.Length; i++) if (_right[i] != null) _right[i].localPosition = _rightClosed[i] + new Vector3(Slide * amount, 0f, 0f);
        }
    }
}
