using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 가까이 보기 화면에서 살필 곳에 마우스를 올리면 그 물건이 하얗게 밝아지고, 모양을 따라 노란 외곽선과 은은한 빛이 둘러진다.
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
        [SerializeField] private float _outlineWidth = 2.5f;

        [SerializeField] private Color _glowColor = new Color(1f, 0.85f, 0.35f, 0.28f);
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

                foreach (var e in entries) e.glow = MakeCopy(e.source, _glowWidth, _glowColor, index++);
                foreach (var e in entries) e.outline = MakeCopy(e.source, _outlineWidth, _outlineColor, index++);
                _entries.AddRange(entries);
            }
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

        private static bool IsLine(Image image)
        {
            var size = image.rectTransform.sizeDelta;
            return Mathf.Min(size.x, size.y) <= 8f;
        }

        private void OnDisable()
        {
            Set(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable) return;
            Set(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Set(false);
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
                // 금이나 흠집 같은 가는 선은 하얗게 밝힌다. 스티커나 덮개처럼 면이 있는 그림은 제 색 그대로 둔다.
                if (IsLine(e.source)) e.source.color = show ? Color.white : e.sourceColor;
            }
        }
    }
}
