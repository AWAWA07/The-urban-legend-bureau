using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 글이나 댓글을 실제로 올리기 직전의 마지막 확인창. 괴담넷 창 위에 얹는다.
    ///
    /// 정답이나 믿음도가 얼마나 바뀔지는 보여 주지 않는다. 한 번 올리면 고치거나 지울 수 없다는 것만 알린다.
    /// 어느 단추든 한 번만 먹는다. 연달아 눌러도 올리기는 한 번만 일어나고, 누르면 창은 곧바로 사라진다.
    /// </summary>
    public class ConfirmDialog : MonoBehaviour
    {
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color Card = Color.white;
        private static readonly Color Ink = new Color(0.12f, 0.12f, 0.14f, 1f);
        private static readonly Color Primary = new Color(0.86f, 0.74f, 0.48f, 1f);
        private static readonly Color Edge = new Color(0.72f, 0.74f, 0.78f, 1f);

        private bool _used;

        /// <summary>지금 떠 있는가. 떠 있는 동안에는 새로 띄우지 않는다.</summary>
        public static bool IsOpen(RectTransform parent) => parent != null && parent.GetComponentInChildren<ConfirmDialog>(false) != null;

        public static void Show(RectTransform parent, TMP_FontAsset regular, TMP_FontAsset bold,
            string message, string yesLabel, string backLabel, Action onYes, Action onBack)
        {
            if (parent == null) { onYes?.Invoke(); return; }
            if (IsOpen(parent)) return;

            var root = NewRect("ConfirmDialog", parent);
            Stretch(root);
            root.SetAsLastSibling();
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = Dim;
            dim.raycastTarget = true;   // 뒤의 단추를 누르지 못하게 막는다
            var dialog = root.gameObject.AddComponent<ConfirmDialog>();

            var card = NewRect("Card", root);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(880f, 330f);
            card.gameObject.AddComponent<Image>().color = Card;

            var text = Label("Message", card, regular, 28f, TextAlignmentOptions.Center);
            text.rectTransform.anchorMin = new Vector2(0f, 0.4f);
            text.rectTransform.anchorMax = new Vector2(1f, 1f);
            text.rectTransform.offsetMin = new Vector2(56f, 0f);
            text.rectTransform.offsetMax = new Vector2(-56f, -40f);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.lineSpacing = 8f;
            text.text = message;

            Button(card, "Back", backLabel, false, new Vector2(-170f, 58f), bold ?? regular, () => dialog.Pick(root, onBack));
            Button(card, "Yes", yesLabel, true, new Vector2(170f, 58f), bold ?? regular, () => dialog.Pick(root, onYes));
        }

        private void Pick(RectTransform root, Action then)
        {
            if (_used) return;
            _used = true;
            root.gameObject.SetActive(false);
            Destroy(root.gameObject);
            then?.Invoke();
        }

        private static void Button(RectTransform parent, string name, string label, bool primary, Vector2 pos, TMP_FontAsset font, Action onClick)
        {
            var rt = NewRect(name, parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300f, 68f);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = primary ? Primary : Color.white;
            if (!primary)
            {
                var outline = rt.gameObject.AddComponent<Outline>();
                outline.effectColor = Edge;
                outline.effectDistance = new Vector2(1f, -1f);
            }
            var t = Label("Label", rt, font, 26f, TextAlignmentOptions.Center);
            Stretch(t.rectTransform);
            t.text = label;
            var button = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());
        }

        private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, float size, TextAlignmentOptions align)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font != null ? font : TMP_Settings.defaultFontAsset;
            t.fontSize = size;
            t.color = Ink;
            t.alignment = align;
            t.raycastTarget = false;
            return t;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
