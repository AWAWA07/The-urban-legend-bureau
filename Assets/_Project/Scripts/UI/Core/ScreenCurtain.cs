using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 화면 전체를 잠깐 까맣게 덮었다 걷는 막. 장면이 한순간에 바뀌는 자리(열차 안에서 승강장으로)를 이어 준다.
    /// 처음 부를 때 스스로 만든다. 누를 것은 없고, 덮여 있는 동안에는 아래를 누르지 못하게 막는다.
    /// 단서 알림(ClueToast)보다 아래, 다른 화면보다 위에 있다.
    /// </summary>
    public class ScreenCurtain : MonoBehaviour
    {
        private static ScreenCurtain _instance;
        private CanvasGroup _group;
        private Coroutine _running;

        /// <summary>from 에서 to 까지(0 이 걷힘, 1 이 덮임) seconds 동안 바꾼 뒤 done 을 부른다.</summary>
        public static void Fade(float to, float seconds, Action done)
        {
            if (_instance == null) _instance = Create();
            _instance.Play(to, seconds, done);
        }

        private static ScreenCurtain Create()
        {
            var root = new GameObject("ScreenCurtain", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(root);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 850;

            var image = new GameObject("Black", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            image.transform.SetParent(root.transform, false);
            var rt = (RectTransform)image.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            image.GetComponent<Image>().color = Color.black;

            var curtain = root.AddComponent<ScreenCurtain>();
            curtain._group = image.GetComponent<CanvasGroup>();
            curtain._group.alpha = 0f;
            curtain._group.blocksRaycasts = false;
            return curtain;
        }

        private void Play(float to, float seconds, Action done)
        {
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Run(Mathf.Clamp01(to), seconds, done));
        }

        private IEnumerator Run(float to, float seconds, Action done)
        {
            float from = _group.alpha;
            _group.blocksRaycasts = true;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(0.01f, seconds))
            {
                _group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            _group.alpha = to;
            _group.blocksRaycasts = to > 0.01f;
            _running = null;
            done?.Invoke();
        }
    }
}
