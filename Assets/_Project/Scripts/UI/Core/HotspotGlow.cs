using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 가까이 보기 화면에서 살필 곳에 마우스를 올리면 그 물건 모양 그대로 둘레가 옅게 빛난다.
    /// 네모난 판을 덮지 않는다. 금, 흠집, 스티커 같은 그림 하나하나 뒤에 같은 모양을 조금 크게 깔아 빛으로 쓴다.
    /// 같은 그림을 여러 번 비껴 찍는 테두리(Outline)는 가는 선에서 줄무늬가 지므로 쓰지 않는다.
    /// 누르는 자리(투명한 단추)와 빛낼 그림은 따로 둔다.
    /// </summary>
    public class HotspotGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("빛낼 그림들. 이 아래 그림까지 모두 빛난다.")]
        [SerializeField] private GameObject[] _targets = new GameObject[0];

        [SerializeField] private Color _glowColor = new Color(1f, 0.93f, 0.7f, 0.5f);

        [Tooltip("그림 둘레로 빛이 퍼지는 두께(화면 단위).")]
        [SerializeField] private float _glowWidth = 4f;

        /// <summary>빛과 그 빛이 따라가는 그림.</summary>
        private readonly List<KeyValuePair<GameObject, GameObject>> _glows = new List<KeyValuePair<GameObject, GameObject>>();
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            foreach (var root in _targets)
            {
                if (root == null) continue;
                foreach (var image in root.GetComponentsInChildren<Image>(true))
                {
                    // 다른 그림 위에 얹힌 작은 그림(덮개의 나사 같은 것)은 빛을 따로 두지 않는다. 바깥 모양만 빛난다.
                    var parent = image.transform.parent;
                    if (parent != null && parent != root.transform.parent && parent.GetComponent<Image>() != null && image.gameObject != root) continue;
                    _glows.Add(new KeyValuePair<GameObject, GameObject>(MakeGlow(image), image.gameObject));
                }
            }
        }

        /// <summary>그림 바로 뒤에 같은 모양을 둘레만큼 크게 깐다. 평소에는 꺼 둔다.</summary>
        private GameObject MakeGlow(Image source)
        {
            var src = source.rectTransform;
            var go = new GameObject(source.name + "_Glow", typeof(RectTransform));
            go.transform.SetParent(src.parent, false);
            go.transform.SetSiblingIndex(src.GetSiblingIndex());   // 그림보다 먼저 = 뒤에 그린다
            var rt = (RectTransform)go.transform;
            rt.anchorMin = src.anchorMin;
            rt.anchorMax = src.anchorMax;
            rt.pivot = src.pivot;
            rt.anchoredPosition = src.anchoredPosition;
            rt.localRotation = src.localRotation;
            rt.localScale = src.localScale;
            rt.sizeDelta = src.sizeDelta + new Vector2(_glowWidth * 2f, _glowWidth * 2f);

            var image = go.AddComponent<Image>();
            image.sprite = source.sprite;
            image.type = source.type;
            image.preserveAspect = source.preserveAspect;
            image.color = _glowColor;
            image.raycastTarget = false;
            go.SetActive(false);
            return go;
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
            foreach (var pair in _glows)
            {
                if (pair.Key == null) continue;
                // 그림이 꺼져 있으면(덮개를 연 뒤의 덮개처럼) 빛도 켜지 않는다.
                pair.Key.SetActive(on && pair.Value != null && pair.Value.activeInHierarchy);
            }
        }
    }
}
