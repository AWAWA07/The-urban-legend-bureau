using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 수동 저장 칸 화면(SAVE 01 ~ SAVE 20). RPG 쯔꾸르처럼 칸을 골라 저장하거나 불러온다.
    /// 숙소 컴퓨터에서는 저장과 불러오기를 오가고, 타이틀에서는 불러오기만 연다.
    /// 문구와 칸 내용은 부르는 쪽(CaseDirector)이 만들어 넘긴다. 여기서는 그리기만 한다.
    /// </summary>
    public class SaveSlotScreen : UIScreen
    {
        [SerializeField] private TMP_FontAsset _regular;
        [SerializeField] private TMP_FontAsset _medium;
        [SerializeField] private TMP_FontAsset _bold;

        public class SlotView
        {
            public string Number;   // SAVE 01
            public string Line1;    // 날짜 · 시각 · 근무 일차 · 플레이 시간
            public string Line2;    // 장소 · 사건 · 해결 수 · 저장 일시
            public bool Empty;
            public bool Latest;     // 가장 최근에 저장한 칸
        }

        public class View
        {
            public string Title;
            public string Sub;
            public bool SaveMode;
            public bool AllowSave;          // 저장 탭을 보일까(타이틀에서는 불러오기만)
            public string SaveTabLabel;
            public string LoadTabLabel;
            public string EmptyLabel;
            public string LatestLabel;
            public string Notice;
            public string CloseLabel;
            public List<SlotView> Slots = new List<SlotView>();
            public Action<int> OnPick;      // 1 부터
            public Action<bool> OnTab;      // true 면 저장
            public Action OnClose;
        }

        private static readonly Color Bg = Hex("101318");
        private static readonly Color Card = Hex("1E242C");
        private static readonly Color CardEmpty = Hex("171B21");
        private static readonly Color Line = Hex("39424E");
        private static readonly Color TextMain = Hex("E4E8ED");
        private static readonly Color TextSub = Hex("9BA7B5");
        private static readonly Color TextDim = Hex("5E6875");
        private static readonly Color Bureau = Hex("B65B61");
        private static readonly Color Soft = Hex("C9A86A");

        private RectTransform _root;
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private View _view;

        public TMP_FontAsset RegularFont => _regular;
        public TMP_FontAsset BoldFont => _bold;
        public RectTransform Root => (RectTransform)transform;

        public void Show(View view)
        {
            _view = view;
            if (IsOpen) Draw();
        }

        protected override void OnOpen() => Draw();

        private void Draw()
        {
            if (_view == null) return;
            if (_root == null)
            {
                _root = (RectTransform)transform;
                var bg = GetComponent<Image>();
                if (bg == null) bg = gameObject.AddComponent<Image>();
                bg.color = Bg;
                bg.raycastTarget = true;
            }
            foreach (var go in _spawned) if (go != null) { go.SetActive(false); Destroy(go); }
            _spawned.Clear();
            var v = _view;

            var edge = Box("Edge", _root, Bureau);
            Place(edge.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -132f), new Vector2(64f, -46f));
            Keep(edge.gameObject);
            Keep(Text(_root, v.Title, _bold, 44f, TextMain, TextAlignmentOptions.Left, new Vector2(84f, -104f), new Vector2(-60f, -46f)));
            Keep(Text(_root, v.Sub, _regular, 21f, TextSub, TextAlignmentOptions.Left, new Vector2(84f, -140f), new Vector2(-60f, -106f)));

            // 저장 / 불러오기 탭
            if (v.AllowSave)
            {
                Keep(Tab("TabSave", v.SaveTabLabel, v.SaveMode, new Vector2(-480f, -62f), () => v.OnTab?.Invoke(true)));
                Keep(Tab("TabLoad", v.LoadTabLabel, !v.SaveMode, new Vector2(-250f, -62f), () => v.OnTab?.Invoke(false)));
            }

            // 칸: 두 줄로 열 개씩
            const float top = 168f, gapX = 24f, gapY = 10f, cardH = 70f;
            float w = (1920f - 120f - gapX) / 2f;
            for (int i = 0; i < v.Slots.Count; i++)
            {
                var s = v.Slots[i];
                int col = i / 10, row = i % 10;
                float x = 60f + col * (w + gapX);
                float y = top + row * (cardH + gapY);
                var card = Box("Slot" + (i + 1), _root, s.Empty ? CardEmpty : Card);
                Place(card.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y - cardH), new Vector2(x + w, -y));
                Keep(card.gameObject);
                Border(card.rectTransform, s.Latest ? Soft : Line, s.Latest ? 2f : 1f);

                var num = Label("Number", card.rectTransform, _bold, 24f, s.Empty ? TextDim : TextMain, TextAlignmentOptions.Left);
                Place(num.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(22f, 0f), new Vector2(170f, 0f));
                num.text = s.Number;

                if (s.Empty)
                {
                    var empty = Label("Empty", card.rectTransform, _regular, 20f, TextDim, TextAlignmentOptions.Left);
                    Place(empty.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(180f, 0f), new Vector2(-20f, 0f));
                    empty.text = v.EmptyLabel;
                }
                else
                {
                    var l1 = Label("Line1", card.rectTransform, _medium, 20f, TextMain, TextAlignmentOptions.Left);
                    Place(l1.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(180f, 0f), new Vector2(-20f, -6f));
                    l1.text = s.Line1 + (s.Latest ? "   <color=#" + ColorUtility.ToHtmlStringRGB(Soft) + "><size=80%>" + v.LatestLabel + "</size></color>" : string.Empty);
                    var l2 = Label("Line2", card.rectTransform, _regular, 17f, TextSub, TextAlignmentOptions.Left);
                    Place(l2.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(180f, 6f), new Vector2(-20f, 0f));
                    l2.text = s.Line2;
                }

                // 불러오기에서는 빈 칸을 누를 수 없다.
                bool clickable = v.SaveMode || !s.Empty;
                if (!clickable) continue;
                card.raycastTarget = true;
                var b = card.gameObject.AddComponent<Button>();
                b.targetGraphic = card;
                int slot = i + 1;
                b.onClick.AddListener(() => v.OnPick?.Invoke(slot));
            }

            if (!string.IsNullOrEmpty(v.Notice))
                Keep(Text(_root, v.Notice, _regular, 20f, Soft, TextAlignmentOptions.Left, new Vector2(60f, 40f), new Vector2(-420f, 100f), true));

            var close = Box("Close", _root, Hex("3A4552"));
            close.raycastTarget = true;
            var crt = close.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(1f, 0f);
            crt.pivot = new Vector2(1f, 0f);
            crt.anchoredPosition = new Vector2(-60f, 34f);
            crt.sizeDelta = new Vector2(300f, 64f);
            Keep(close.gameObject);
            var ct = Label("Label", crt, _bold, 24f, TextMain, TextAlignmentOptions.Center);
            Stretch(ct.rectTransform);
            ct.text = v.CloseLabel;
            var cb = close.gameObject.AddComponent<Button>();
            cb.targetGraphic = close;
            cb.onClick.AddListener(() => v.OnClose?.Invoke());
        }

        private GameObject Tab(string name, string label, bool on, Vector2 pos, Action click)
        {
            var img = Box(name, _root, on ? Soft : Hex("2A313B"));
            img.raycastTarget = true;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(210f, 54f);
            var t = Label("Label", rt, _bold, 22f, on ? Hex("1B1E24") : TextSub, TextAlignmentOptions.Center);
            Stretch(t.rectTransform);
            t.text = label;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(() => click?.Invoke());
            return img.gameObject;
        }

        // ------------------------------------------------------------- 조각

        private void Keep(GameObject go) => _spawned.Add(go);

        private GameObject Text(RectTransform parent, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align,
            Vector2 offMin, Vector2 offMax, bool anchorBottom = false)
        {
            var t = Label("Text", parent, font, size, color, align);
            if (anchorBottom) Place(t.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), offMin, offMax);
            else Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), offMin, offMax);
            t.text = text ?? string.Empty;
            return t.gameObject;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image Box(string name, Transform parent, Color color)
        {
            var rt = NewRect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private TMP_Text Label(string name, Transform parent, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font != null ? font : (_regular != null ? _regular : TMP_Settings.defaultFontAsset);
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
            t.richText = true;
            return t;
        }

        private static void Place(RectTransform rt, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
        }

        private static void Stretch(RectTransform rt) => Place(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        private static void Border(RectTransform parent, Color color, float width)
        {
            Line1(parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -width), Vector2.zero, color);
            Line1(parent, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, width), color);
            Line1(parent, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(width, 0f), color);
            Line1(parent, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-width, 0f), Vector2.zero, color);
        }

        private static void Line1(RectTransform parent, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax, Color color)
        {
            var line = Box("Border", parent, color);
            Place(line.rectTransform, min, max, offMin, offMax);
        }
    }
}
