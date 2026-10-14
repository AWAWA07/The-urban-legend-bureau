using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 단서를 얻으면 화면 오른쪽 위에 잠깐 뜨는 네모난 알림. "차내 CCTV 단서 획득" 처럼 어디서 얻었는지 적고, 그 아래에 단서 이름을 작게 적는다.
    /// 오른쪽에서 살짝 밀려 들어와 몇 초 머문 뒤 옅어지며 사라진다. 어느 화면 위에서든(가까이 보기 포함) 맨 앞에 뜬다.
    /// 누를 것이 없으므로 아래 화면을 막지 않는다. 연달아 얻으면 새 알림으로 갈아 끼운다.
    /// 처음 부를 때 스스로 만든다. 글꼴은 부르는 쪽이 넘긴 것을 쓴다(한글이 들어 있는 것).
    /// </summary>
    public class ClueToast : MonoBehaviour
    {
        private static ClueToast _instance;

        private RectTransform _box;
        private CanvasGroup _group;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _detail;
        private Coroutine _running;

        private const float Hold = 2.6f;

        /// <summary>알림을 띄운다. detail 이 비어 있으면 한 줄만 쓴다.</summary>
        public static void Show(string title, string detail, TMP_FontAsset font)
        {
            if (_instance == null) _instance = Create(font);
            _instance.Play(title, detail);
        }

        private static ClueToast Create(TMP_FontAsset font)
        {
            var root = new GameObject("ClueToast", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            DontDestroyOnLoad(root);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var toast = root.AddComponent<ClueToast>();

            // 상자. 어두운 판에 가는 테두리, 왼쪽에 금빛 띠.
            var box = new GameObject("Box", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            box.transform.SetParent(root.transform, false);
            toast._box = (RectTransform)box.transform;
            toast._box.anchorMin = toast._box.anchorMax = toast._box.pivot = new Vector2(1f, 1f);
            toast._box.sizeDelta = new Vector2(460f, 104f);
            var bg = box.GetComponent<Image>();
            bg.color = new Color(0.035f, 0.035f, 0.05f, 0.94f);
            bg.raycastTarget = false;
            toast._group = box.GetComponent<CanvasGroup>();
            toast._group.blocksRaycasts = false;
            toast._group.interactable = false;

            Edge(box.transform, "EdgeTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 2f), new Color(0.32f, 0.32f, 0.38f));
            Edge(box.transform, "EdgeBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 2f), new Color(0.32f, 0.32f, 0.38f));
            Edge(box.transform, "EdgeRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(2f, 0f), new Color(0.32f, 0.32f, 0.38f));
            Edge(box.transform, "Accent", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(6f, 0f), new Color(0.85f, 0.70f, 0.38f));

            toast._title = Text(box.transform, "Title", font, 30f, FontStyles.Bold, new Color(0.96f, 0.93f, 0.86f), new Vector2(28f, -16f));
            toast._detail = Text(box.transform, "Detail", font, 22f, FontStyles.Normal, new Color(0.85f, 0.70f, 0.38f), new Vector2(28f, -60f));

            box.SetActive(false);
            return toast;
        }

        private static void Edge(Transform parent, string name, Vector2 min, Vector2 max, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = new Vector2(min.x, min.y);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, TMP_FontAsset font, float size, FontStyles style, Color color, Vector2 topLeft)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = topLeft;
            rt.sizeDelta = new Vector2(410f, 40f);
            var t = go.GetComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = TextAlignmentOptions.TopLeft;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
            return t;
        }

        private void Play(string title, string detail)
        {
            _title.text = title;
            bool two = !string.IsNullOrEmpty(detail);
            _detail.text = two ? detail : string.Empty;
            _detail.gameObject.SetActive(two);
            _box.sizeDelta = new Vector2(460f, two ? 104f : 68f);
            if (_running != null) StopCoroutine(_running);
            _box.gameObject.SetActive(true);
            _running = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            // 화면 오른쪽 위. 맨 위 줄(경과 시간)과 가까이 보기의 돌아가기 단추 아래에 둔다.
            var rest = new Vector2(-40f, -140f);
            var from = rest + new Vector2(60f, 0f);
            for (float e = 0f; e < 0.28f; e += Time.unscaledDeltaTime)
            {
                float k = 1f - Mathf.Pow(1f - e / 0.28f, 3f);
                _box.anchoredPosition = Vector2.Lerp(from, rest, k);
                _group.alpha = k;
                yield return null;
            }
            _box.anchoredPosition = rest;
            _group.alpha = 1f;

            yield return new WaitForSecondsRealtime(Hold);

            for (float e = 0f; e < 0.5f; e += Time.unscaledDeltaTime)
            {
                _group.alpha = 1f - e / 0.5f;
                yield return null;
            }
            _box.gameObject.SetActive(false);
            _running = null;
        }
    }
}
