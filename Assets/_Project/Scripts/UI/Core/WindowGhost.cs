using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 창밖의 그것. 창의 오른쪽 가장자리에서 슬며시 얼굴을 내밀었다가, 눈 깜짝할 새 사라진다.
    /// 다 드러나도 반쯤 비쳐 보일 듯 말 듯하다. 그림은 창 유리 안쪽에 잘려 보인다(유리에 RectMask2D). 평소에는 숨어 있다.
    /// </summary>
    public class WindowGhost : MonoBehaviour
    {
        [SerializeField] private Image _image;

        [Tooltip("숨어 있는 자리. 창 오른쪽 바깥이다.")]
        [SerializeField] private Vector2 _hiddenPosition;

        [Tooltip("얼굴을 내민 자리.")]
        [SerializeField] private Vector2 _peekPosition;

        [Tooltip("살핀 뒤 나타나기까지 기다리는 시간(초). 방금 본 글을 읽을 틈이다.")]
        [SerializeField] private float _delay = 1.1f;

        [Tooltip("슬며시 나오는 데 걸리는 시간(초).")]
        [SerializeField] private float _slideSeconds = 2.2f;

        [Tooltip("내민 채 멈춰 있는 시간(초).")]
        [SerializeField] private float _holdSeconds = 0.9f;

        [Tooltip("다 드러났을 때의 진하기. 낮을수록 어둠에 묻혀 보일 듯 말 듯하다.")]
        [SerializeField] private float _maxAlpha = 0.38f;

        [Tooltip("정전 뒤 어둠 앞에 옅게 따라 그리는 같은 모양. 어둠 속에서도 아주 희미하게 비친다. 비워 두면 쓰지 않는다.")]
        [SerializeField] private Image _echo;

        [Tooltip("어둠 속 모양의 진하기. 본래 진하기에 곱한다. 조금만 보인다.")]
        [SerializeField] private float _echoStrength = 0.1f;

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

        /// <summary>
        /// 어둠 앞의 모양을 본래 그림에 맞춘다. 자리, 켜짐, 진하기를 따라 하고, 자르는 틀은 흔들리는 유리를 따라간다.
        /// 어둠이 꺼져 있으면(정전 전) 본래 그림만으로 충분하므로 그리지 않는다.
        /// </summary>
        private void LateUpdate()
        {
            if (_echo == null || _image == null) return;
            var mask = _echo.rectTransform.parent as RectTransform;
            var glass = _image.rectTransform.parent as RectTransform;
            if (mask != null && glass != null) mask.position = glass.position;

            _echo.rectTransform.anchoredPosition = _image.rectTransform.anchoredPosition;
            var c = _image.color;
            c.a *= _echoStrength;
            _echo.color = c;
            _echo.enabled = _image.enabled && _image.color.a > 0f;
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

            // 슬며시. 처음에는 거의 움직이지 않다가 천천히 다가온다. 처음부터 끝까지 보일 듯 말 듯한 진하기다.
            SetAlpha(_maxAlpha);
            for (float e = 0f; e < _slideSeconds; e += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, e / _slideSeconds);
                rt.anchoredPosition = Vector2.Lerp(_hiddenPosition, _peekPosition, k);
                yield return null;
            }
            rt.anchoredPosition = _peekPosition;
            yield return new WaitForSecondsRealtime(_holdSeconds);

            // 갑자기. 한 번 끊겼다가 사라진다.
            _image.enabled = false;
            yield return new WaitForSecondsRealtime(0.05f);
            _image.enabled = true;
            yield return new WaitForSecondsRealtime(0.04f);
            Hide();

            yield return new WaitForSecondsRealtime(0.6f);
            onDone?.Invoke();
        }
    }
}
