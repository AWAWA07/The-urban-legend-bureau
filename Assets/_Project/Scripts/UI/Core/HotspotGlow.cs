using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 가까이 보기 화면에서 살필 곳에 마우스를 올리면 그 물건이 하얗게 밝아지고, 모양을 따라 고른 노란 외곽선이 둘러진다.
    /// 네모난 판을 덮지 않는다. 금, 흠집, 스티커 같은 그림 하나하나의 모양을 그대로 따른다.
    ///
    /// 그림 뒤에 같은 모양을 두 겹 깐다. 바깥은 크고 옅은 빛, 안쪽은 조금 큰 진한 외곽선이다.
    /// 한 묶음(금처럼 선 여럿) 안에서는 빛과 외곽선을 모두 선들보다 먼저 그려, 선이 겹치는 곳에서도 외곽선이 선을 덮지 않는다.
    /// 누르는 자리(투명한 단추)와 빛낼 그림은 따로 둔다.
    /// </summary>
    public class HotspotGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("빛낼 그림들. 이 아래 그림까지 모두 빛난다.")]
        [SerializeField] private GameObject[] _targets = new GameObject[0];

        [SerializeField] private Color _outlineColor = new Color(1f, 0.86f, 0.25f, 1f);
        [SerializeField] private float _outlineWidth = 3f;

        [Tooltip("켜면 외곽선을 그림 뒤에 큰 사본으로 까는 대신, 네 변을 그림 둘레 맨 앞에 그린다. 기둥이나 이음선 같은 것에 외곽선이 가려지는 자리(빈자리 방석)에 쓴다.")]
        [SerializeField] private bool _frameOnTop;

        [Tooltip("바깥 빛. 반투명한 것이 여러 겹 겹치면 얼룩이 져 기본은 끈다(투명도 0).")]
        [SerializeField] private Color _glowColor = new Color(1f, 0.85f, 0.35f, 0f);
        [SerializeField] private float _glowWidth = 7f;

        private class Entry
        {
            public Image source;
            public Color sourceColor;
            public GameObject glow;
            public GameObject outline;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private Button _button;
        /// <summary>빛낼 그림 가운데 첫 번째의 자리. 이 자리를 또렷이 표시할 때(CloseupScreen.PinSpot) 쓴다. 없으면 null.</summary>
        public RectTransform PrimaryTarget => _targets.Length > 0 && _targets[0] != null ? _targets[0].transform as RectTransform : null;

        private bool _hovered;
        private bool _pinned;

        /// <summary>
        /// 켜 두면 마우스를 올리지 않아도 노란 외곽선이 천천히 숨 쉬듯 깜빡이며 남는다. 지금 눌러야 할 자리를 또렷이 알릴 때 쓴다.
        /// </summary>
        public bool Pinned
        {
            get => _pinned;
            set { _pinned = value; Set(_hovered || _pinned); }
        }

        private void Awake()
        {
            _button = GetComponent<Button>();

            // 빛낼 그림을 모은다. 다른 그림 위에 얹힌 작은 그림(덮개의 나사 같은 것)은 빼고 바깥 모양만 쓴다.
            var images = new List<Image>();
            foreach (var root in _targets)
            {
                if (root == null) continue;
                foreach (var image in root.GetComponentsInChildren<Image>(true))
                {
                    var parent = image.transform.parent;
                    if (image.gameObject != root && parent != null && parent.GetComponent<Image>() != null) continue;
                    images.Add(image);
                }
            }

            // 부모마다 묶는다. 빛과 외곽선은 그 부모 안에서 맨 앞 그림의 자리에 몰아 끼운다.
            var byParent = new Dictionary<Transform, List<Image>>();
            foreach (var image in images)
            {
                var p = image.transform.parent;
                if (!byParent.TryGetValue(p, out var list)) byParent[p] = list = new List<Image>();
                list.Add(image);
            }

            foreach (var kv in byParent)
            {
                int index = int.MaxValue;
                foreach (var image in kv.Value) index = Mathf.Min(index, image.transform.GetSiblingIndex());

                var entries = new List<Entry>();
                foreach (var image in kv.Value)
                    entries.Add(new Entry { source = image, sourceColor = image.color });

                if (_glowColor.a > 0f)
                    foreach (var e in entries) e.glow = MakeCopy(e.source, _glowWidth, _glowColor, index++);
                foreach (var e in entries)
                    if (!IsDrawnLine(e.source)) e.outline = _frameOnTop ? MakeFrame(e.source, _outlineWidth, _outlineColor) : MakeCopy(e.source, _outlineWidth, _outlineColor, index++);
                _entries.AddRange(entries);
            }
        }

        /// <summary>그림 둘레를 바깥쪽으로 감싸는 네 변. 그 부모의 맨 앞에 그려 다른 그림에 가리지 않는다. 평소에는 꺼 둔다.</summary>
        private static GameObject MakeFrame(Image source, float width, Color color)
        {
            var src = source.rectTransform;
            var go = new GameObject(source.name + "_Frame", typeof(RectTransform));
            go.transform.SetParent(src.parent, false);
            go.transform.SetAsLastSibling();
            var rt = (RectTransform)go.transform;
            rt.anchorMin = src.anchorMin;
            rt.anchorMax = src.anchorMax;
            rt.pivot = src.pivot;
            rt.anchoredPosition = src.anchoredPosition;
            rt.localRotation = src.localRotation;
            rt.localScale = src.localScale;
            rt.sizeDelta = src.sizeDelta;
            void Edge(string n, Vector2 min, Vector2 max, Vector2 size, Vector2 pivot)
            {
                var e = new GameObject(n, typeof(RectTransform), typeof(Image));
                e.transform.SetParent(go.transform, false);
                var ert = (RectTransform)e.transform;
                ert.anchorMin = min; ert.anchorMax = max; ert.pivot = pivot;
                ert.anchoredPosition = Vector2.zero;
                ert.sizeDelta = size;
                var img = e.GetComponent<Image>();
                img.color = color;
                img.raycastTarget = false;
            }
            Edge("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(width * 2f, width), new Vector2(0.5f, 0f));
            Edge("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(width * 2f, width), new Vector2(0.5f, 1f));
            Edge("Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(width, 0f), new Vector2(1f, 0.5f));
            Edge("Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(width, 0f), new Vector2(0f, 0.5f));
            go.SetActive(false);
            return go;
        }

        /// <summary>그림과 같은 모양을 둘레만큼 크게 만들어 그 부모의 index 자리에 끼운다. 평소에는 꺼 둔다.</summary>
        private static GameObject MakeCopy(Image source, float width, Color color, int index)
        {
            var src = source.rectTransform;
            var go = new GameObject(source.name + "_Glow", typeof(RectTransform));
            go.transform.SetParent(src.parent, false);
            go.transform.SetSiblingIndex(index);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = src.anchorMin;
            rt.anchorMax = src.anchorMax;
            rt.pivot = src.pivot;
            rt.anchoredPosition = src.anchoredPosition;
            rt.localRotation = src.localRotation;
            rt.localScale = src.localScale;
            rt.sizeDelta = src.sizeDelta + new Vector2(width * 2f, width * 2f);

            var image = go.AddComponent<Image>();
            image.sprite = source.sprite;
            image.type = source.type;
            image.preserveAspect = source.preserveAspect;
            image.color = color;
            image.raycastTarget = false;
            go.SetActive(false);
            return go;
        }

        /// <summary>그림으로 그린 가는 선(머리카락 같은 것). 키운 사본이 선과 어긋나 외곽선을 두르지 않고 하얗게만 밝힌다. 그림 이름이 line_ 으로 시작한다.</summary>
        private static bool IsDrawnLine(Image image)
        {
            return image.sprite != null && image.sprite.name.StartsWith("line_");
        }

        private static bool IsLine(Image image)
        {
            if (IsDrawnLine(image)) return true;
            var size = image.rectTransform.sizeDelta;
            return Mathf.Min(size.x, size.y) <= 8f;
        }

        private void OnEnable()
        {
            if (_pinned) Set(true);
        }

        private void OnDisable()
        {
            _hovered = false;
            Set(false);
        }

        private void Update()
        {
            if (!_pinned || _hovered) return;
            float a = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f));
            foreach (var e in _entries)
            {
                if (e.outline == null) continue;
                var c = _outlineColor;
                c.a *= a;
                foreach (var image in e.outline.GetComponentsInChildren<Image>(true)) image.color = c;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable) return;
            _hovered = true;
            Set(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            Set(_pinned);
        }

        private void Set(bool on)
        {
            foreach (var e in _entries)
            {
                if (e.source == null) continue;
                // 그림이 꺼져 있으면(덮개를 연 뒤의 덮개처럼) 빛도 켜지 않는다.
                bool show = on && e.source.gameObject.activeInHierarchy;
                if (e.glow != null) e.glow.SetActive(show);
                if (e.outline != null) e.outline.SetActive(show);
                if (e.outline != null) foreach (var image in e.outline.GetComponentsInChildren<Image>(true)) image.color = _outlineColor;   // 깜빡이던 것을 진하게 되돌린다
                // 금이나 흠집 같은 가는 선은 하얗게 밝힌다. 스티커나 덮개처럼 면이 있는 그림은 제 색 그대로 둔다.
                // 그림으로 그린 선(머리카락)은 외곽선 대신 선 자체가 노란 외곽선 색으로 물든다. 다른 살필 곳의 노란 빛과 맞춘다.
                if (IsDrawnLine(e.source)) e.source.color = show ? _outlineColor : e.sourceColor;
                else if (IsLine(e.source)) e.source.color = show ? Color.white : e.sourceColor;
            }
        }
    }
}
