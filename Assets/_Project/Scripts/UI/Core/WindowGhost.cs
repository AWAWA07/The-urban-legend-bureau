using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 창밖의 그것. 창의 오른쪽 가장자리에서 어둠에 섞여 서서히 떠오르듯 얼굴을 내밀었다가, 눈 깜짝할 새 사라진다.
    /// 다 드러나도 반쯤 비쳐 보일 듯 말 듯하다. 그림은 창 유리 안쪽에 잘려 보인다(유리에 RectMask2D). 평소에는 숨어 있다.
    /// </summary>
    public class WindowGhost : MonoBehaviour
    {
        [SerializeField] private Image _image;

        [Tooltip("숨어 있는 자리. 창 오른쪽 가장자리 근처다.")]
        [SerializeField] private Vector2 _hiddenPosition;

        [Tooltip("얼굴을 내민 자리.")]
        [SerializeField] private Vector2 _peekPosition;

        [Tooltip("살핀 뒤 나타나기까지 기다리는 시간(초). 방금 본 글을 읽을 틈이다.")]
        [SerializeField] private float _delay = 1.1f;

        [Tooltip("떠오르듯 나오는 데 걸리는 시간(초).")]
        [SerializeField] private float _slideSeconds = 2.8f;

        [Tooltip("내민 채 멈춰 있는 시간(초).")]
        [SerializeField] private float _holdSeconds = 1.0f;

        [Tooltip("다 드러났을 때의 진하기. 낮을수록 어둠에 묻혀 보일 듯 말 듯하다.")]
        [SerializeField] private float _maxAlpha = 0.38f;

        [Tooltip("그것의 빛깔. 창밖 어둠과 터널 불빛에 섞인 푸르스름한 회색이다.")]
        [SerializeField] private Color _tint = new Color(0.72f, 0.76f, 0.86f, 1f);

        private void Awake()
        {
            Hide();
        }

        private void Hide()
        {
            if (_image == null) return;
            _image.rectTransform.anchoredPosition = _hiddenPosition;
            SetAlpha(0f);
            _image.enabled = false;
        }

        private void SetAlpha(float a)
        {
            var c = _tint;
            c.a = a;
            _image.color = c;
        }

        /// <summary>나왔다 사라지는 것을 한 번 보여 준다. 끝나면 onDone 을 부른다.</summary>
        public void Play(Action onDone)
        {
            StopAllCoroutines();
            StartCoroutine(Run(onDone));
        }

        private IEnumerator Run(Action onDone)
        {
            yield return new WaitForSecondsRealtime(_delay);
            if (_image == null) { onDone?.Invoke(); yield break; }

            var rt = _image.rectTransform;
            rt.anchoredPosition = _hiddenPosition;
            SetAlpha(0f);
            _image.enabled = true;

            // 어둠 속에서 서서히 떠오른다. 움직임은 처음과 끝이 느리고, 진하기는 움직임보다 조금 늦게 따라온다.
            // 숨 쉬듯 위아래로 아주 조금 흔들린다.
            for (float e = 0f; e < _slideSeconds; e += Time.unscaledDeltaTime)
            {
                float k = e / _slideSeconds;
                float move = Mathf.SmoothStep(0f, 1f, k);
                float fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - 0.1f) / 0.8f));
                var drift = new Vector2(0f, Mathf.Sin(e * 2.4f) * 6f);
                rt.anchoredPosition = Vector2.Lerp(_hiddenPosition, _peekPosition, move) + drift;
                SetAlpha(_maxAlpha * fade);
                yield return null;
            }

            for (float e = 0f; e < _holdSeconds; e += Time.unscaledDeltaTime)
            {
                rt.anchoredPosition = _peekPosition + new Vector2(0f, Mathf.Sin((_slideSeconds + e) * 2.4f) * 6f);
                yield return null;
            }

            // 갑자기. 한 번 짙어졌다가 순식간에 꺼진다.
            SetAlpha(Mathf.Min(1f, _maxAlpha * 1.8f));
            yield return new WaitForSecondsRealtime(0.06f);
            for (float e = 0f; e < 0.08f; e += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(_maxAlpha, 0f, e / 0.08f));
                yield return null;
            }
            Hide();

            yield return new WaitForSecondsRealtime(0.6f);
            onDone?.Invoke();
        }
    }
}
