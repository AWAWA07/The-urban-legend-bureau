using System;
using System.Collections;
using UnityEngine;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 열차 안 천장의 CCTV. 평소에는 부자연스럽게 천장 쪽을 올려다보고 있다.
    /// 부르면 움찔하다가 갑자기 고개를 꺾어 누군가를 내려다본다. 그때 빨간 불이 켜진다.
    ///
    /// 머리(렌즈가 달린 몸통)는 팔 끝 관절을 축으로 돈다. 렌즈는 머리의 오른쪽(+x)을 본다.
    /// </summary>
    public class CctvGlance : MonoBehaviour
    {
        [Tooltip("관절을 축으로 도는 머리. 렌즈가 오른쪽을 보게 그려 둔다.")]
        [SerializeField] private Transform _head;

        [Tooltip("녹화 중임을 알리는 빨간 불. 고개를 돌린 뒤에 켜진다.")]
        [SerializeField] private SpriteRenderer _light;

        [Tooltip("평소 머리 각도(도). 위를 올려다보는 각이다.")]
        [SerializeField] private float _idleAngle = 34f;

        [Tooltip("고개를 꺾는 데 걸리는 시간(초). 짧을수록 섬뜩하다.")]
        [SerializeField] private float _snapSeconds = 0.16f;

        private Color _lightOn;
        private Coroutine _turn;

        /// <summary>지금 내려다보고 있는가. 한 번 돌아간 뒤로는 그대로 둔다.</summary>
        public bool IsWatching { get; private set; }

        private void Awake()
        {
            if (_light != null) _lightOn = _light.color;
            ResetPose();
        }

        /// <summary>처음 모습으로 되돌린다. 위를 올려다보고, 불은 꺼져 있다.</summary>
        public void ResetPose()
        {
            if (_turn != null) { StopCoroutine(_turn); _turn = null; }
            IsWatching = false;
            if (_head != null) _head.localRotation = Quaternion.Euler(0f, 0f, _idleAngle);
            SetLight(0f);
        }

        /// <summary>
        /// 대상을 내려다본다. 움찔 → 잠깐 멈춤 → 확 꺾어 내려다봄 → 빨간 불. 끝나면 onDone 을 부른다.
        /// </summary>
        public void LookAt(Transform target, Action onDone)
        {
            if (_head == null || target == null) { onDone?.Invoke(); return; }
            if (_turn != null) StopCoroutine(_turn);
            _turn = StartCoroutine(Turn(target, onDone));
        }

        private IEnumerator Turn(Transform target, Action onDone)
        {
            // 움찔. 위를 본 채로 두어 번 떨린다. 고장 난 기계가 다시 살아나는 것 같다.
            for (int i = 0; i < 3; i++)
            {
                _head.localRotation = Quaternion.Euler(0f, 0f, _idleAngle + (i % 2 == 0 ? -4f : 3f));
                yield return new WaitForSeconds(0.05f);
            }
            _head.localRotation = Quaternion.Euler(0f, 0f, _idleAngle);
            yield return new WaitForSeconds(0.35f);

            // 대상의 머리 높이를 겨눈다. 사람은 발이 기준점이라 키의 위쪽을 본다.
            Vector3 aim = target.position + new Vector3(0f, 2.2f * Mathf.Abs(target.lossyScale.y) / 1.5f, 0f);
            Vector2 d = aim - _head.position;
            float parent = _head.parent != null ? _head.parent.eulerAngles.z : 0f;
            float to = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - parent;

            // 확 꺾는다. 끝에서 조금 지나쳤다가 돌아와 멈춘다.
            float from = _idleAngle;
            float over = to - 6f;
            for (float e = 0f; e < _snapSeconds; e += Time.deltaTime)
            {
                float k = e / _snapSeconds;
                k = 1f - (1f - k) * (1f - k) * (1f - k);
                _head.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(from, over, k));
                yield return null;
            }
            for (float e = 0f; e < 0.12f; e += Time.deltaTime)
            {
                _head.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(over, to, e / 0.12f));
                yield return null;
            }
            _head.localRotation = Quaternion.Euler(0f, 0f, to);
            IsWatching = true;

            // 빨간 불이 두 번 깜빡이고 켜진 채로 남는다.
            for (int i = 0; i < 2; i++)
            {
                SetLight(1f); yield return new WaitForSeconds(0.08f);
                SetLight(0f); yield return new WaitForSeconds(0.08f);
            }
            SetLight(1f);

            _turn = null;
            onDone?.Invoke();
        }

        private void SetLight(float on)
        {
            if (_light == null) return;
            var c = _lightOn;
            c.a *= on;
            _light.color = c;
        }
    }
}
