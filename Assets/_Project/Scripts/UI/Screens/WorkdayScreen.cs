using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 본편의 하루를 보여 주는 화면. 같은 틀로 세 가지를 띄운다.
    ///   업무 현황판      출근해서, 또는 컴퓨터에서 연다. 맡은 괴담과 믿음도·등급·전날 대비, 고른 사건의 자세한 내용.
    ///   야간 확산 보고   잠에서 깨어 18:00 에 출근하면 먼저 뜬다. 잠들기 전과 출근 뒤의 믿음도.
    ///   근무 종료        03:00 이 되면 뜬다. 오늘 근무 동안 바뀐 것.
    /// 문구는 부르는 쪽(CaseDirector)이 번역해 넘긴다. 여기서는 그리기만 한다. 수사 노트와 같은 어두운 조사기관 빛깔이다.
    /// </summary>
    public class WorkdayScreen : UIScreen
    {
        [SerializeField] private TMP_FontAsset _regular;
        [SerializeField] private TMP_FontAsset _medium;
        [SerializeField] private TMP_FontAsset _bold;

        public class Row
        {
            public string Key;          // 고를 때 돌려줄 값(사건 ID)
            public string Name;         // 사건 이름
            public string Sub;          // 괴담 이름
            public string[] Cells;      // 칸마다의 글
            public int GradeLevel;      // 1 관측 ~ 5 멸망, 6 ???. 등급 칸 빛깔
            public int GradeCell = -1;  // 등급이 든 칸
            public int DeltaCell = -1;  // 변화가 든 칸
            public int DeltaSign;       // 오르면 1, 내리면 -1
            public string Tag;          // 신규 / 긴급 / 해결
            public bool TagDanger;
        }

        /// <summary>자세히 칸 아래의 단추 하나. 누를 수 없으면 흐리게 보인다.</summary>
        public class ActionButton
        {
            public string Label;
            public Action OnClick;
            public bool Enabled = true;
        }

        public class View
        {
            public string Bureau;       // 괴담 검열국 — 오늘의 업무
            public string Title;
            public string Sub;
            public string[] Stats;      // 위쪽 숫자 칸들 "이름\n값"
            public int DangerStat = -1; // 붉게 칠할 숫자 칸
            public string[] Columns;
            public float[] ColumnWidths;
            public List<Row> Rows = new List<Row>();
            public string SelectedKey;
            public string DetailTitle;
            public List<string> Detail = new List<string>();
            public List<string> Notes = new List<string>();
            public string Banner;       // 붉은 띠(게임 오버 등). 비우면 없다
            public string PrimaryLabel;
            public Action OnPrimary;
            public Action<string> OnSelect;
            public List<ActionButton> Actions = new List<ActionButton>();   // 자세히 칸 아래에 세우는 단추들
        }

        private static readonly Color Bg = Hex("101318");
        private static readonly Color Panel = Hex("1B2028");
        private static readonly Color CardColor = Hex("252C35");
        private static readonly Color Line = Hex("39424E");
        private static readonly Color TextMain = Hex("E4E8ED");
        private static readonly Color TextSub = Hex("9BA7B5");
        private static readonly Color TextDim = Hex("66717E");
        private static readonly Color Accent = Hex("6B94A8");
        private static readonly Color Bureau = Hex("B65B61");
        private static readonly Color Soft = Hex("C9A86A");
        private static readonly Color Up = Hex("D9666C");
        private static readonly Color Down = Hex("5DB97C");

        private RectTransform _root;
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private View _view;

        public void Show(View view)
        {
            _view = view;
            if (IsOpen) Draw();
        }

        protected override void OnOpen() => Draw();

        public static Color GradeColor(int level)
        {
            switch (level)
            {
                case 1: return Hex("7FA7B8");
                case 2: return Hex("8FB27A");
                case 3: return Hex("D8B25A");
                case 4: return Hex("E0874F");
                case 5: return Hex("E05A5A");
                default: return Hex("B83A4B");
            }
        }

        private void Draw()
        {
            if (_view == null) return;
            if (_root == null)
            {
                _root = (RectTransform)transform;
                var bg = GetComponent<Image>();
                if (bg == null) bg = gameObject.AddComponent<Image>();
                bg.color = Bg;
            }
            foreach (var go in _spawned) if (go != null) { go.SetActive(false); Destroy(go); }
            _spawned.Clear();

            var v = _view;

            // 머리
            var edge = Box("Edge", _root, Bureau);
            Place(edge.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -150f), new Vector2(64f, -48f));
            Keep(edge.gameObject);
            Keep(Text(_root, v.Bureau, _medium, 22f, Bureau, TextAlignmentOptions.Left, new Vector2(84f, -82f), new Vector2(-60f, -46f)));
            Keep(Text(_root, v.Title, _bold, 44f, TextMain, TextAlignmentOptions.Left, new Vector2(84f, -140f), new Vector2(-60f, -82f)));
            Keep(Text(_root, v.Sub, _regular, 22f, TextSub, TextAlignmentOptions.Left, new Vector2(84f, -184f), new Vector2(-60f, -148f)));

            float top = 206f;
            if (!string.IsNullOrEmpty(v.Banner))
            {
                var banner = Box("Banner", _root, Hex("5A1E24"));
                Place(banner.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(60f, -top - 52f), new Vector2(-60f, -top));
                Keep(banner.gameObject);
                var bt = Label("Text", banner.rectTransform, _bold, 24f, Hex("F2C9CC"), TextAlignmentOptions.Center);
                Stretch(bt.rectTransform);
                bt.text = v.Banner;
                top += 66f;
            }

            // 숫자 칸
            if (v.Stats != null && v.Stats.Length > 0)
            {
                float w = (1920f - 120f - (v.Stats.Length - 1) * 16f) / v.Stats.Length;
                for (int i = 0; i < v.Stats.Length; i++)
                {
                    var card = Box("Stat" + i, _root, Panel);
                    Place(card.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(60f + i * (w + 16f), -top - 96f), new Vector2(60f + i * (w + 16f) + w, -top));
                    Border(card.rectTransform, i == v.DangerStat ? Up : Line, 1f);
                    Keep(card.gameObject);
                    var parts = (v.Stats[i] ?? string.Empty).Split('\n');
                    var name = Label("Name", card.rectTransform, _medium, 19f, TextDim, TextAlignmentOptions.TopLeft);
                    Place(name.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 10f), new Vector2(-20f, -12f));
                    name.text = parts[0];
                    var value = Label("Value", card.rectTransform, _bold, 32f, i == v.DangerStat ? Up : TextMain, TextAlignmentOptions.BottomLeft);
                    Place(value.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 12f), new Vector2(-20f, -10f));
                    value.text = parts.Length > 1 ? parts[1] : string.Empty;
                }
                top += 116f;
            }

            // 표(왼쪽) + 자세히(오른쪽)
            bool detail = !string.IsNullOrEmpty(v.DetailTitle) || v.Detail.Count > 0;
            float tableRight = detail ? 640f : 60f;
            var table = Box("Table", _root, Panel);
            Place(table.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(60f, 130f), new Vector2(-tableRight, -top));
            Border(table.rectTransform, Line, 1f);
            Keep(table.gameObject);
            DrawTable(table.rectTransform, v);

            if (detail)
            {
                var side = Box("Detail", _root, Panel);
                Place(side.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-620f, 130f), new Vector2(-60f, -top));
                Border(side.rectTransform, Line, 1f);
                Keep(side.gameObject);
                var title = Label("Title", side.rectTransform, _bold, 28f, TextMain, TextAlignmentOptions.TopLeft);
                Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(26f, -66f), new Vector2(-26f, -22f));
                title.text = v.DetailTitle;
                // 단추는 칸 맨 아래부터 위로 쌓는다. 글은 그 위까지만 쓴다.
                float btnH = 58f, gap = 10f;
                float buttonsTop = 20f + v.Actions.Count * (btnH + gap);
                for (int i = 0; i < v.Actions.Count; i++)
                {
                    var a = v.Actions[i];
                    if (a == null) continue;
                    float y = 20f + (v.Actions.Count - 1 - i) * (btnH + gap);
                    var btn = Box("Action" + i, side.rectTransform, a.Enabled ? (i == 0 ? Soft : Hex("3A4552")) : Hex("262C34"));
                    Place(btn.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(26f, y), new Vector2(-26f, y + btnH));
                    var t = Label("Label", btn.rectTransform, _bold, 23f,
                        !a.Enabled ? TextDim : i == 0 ? Hex("1B1E24") : TextMain, TextAlignmentOptions.Center);
                    Stretch(t.rectTransform);
                    t.text = a.Label;
                    if (!a.Enabled) continue;
                    btn.raycastTarget = true;
                    var b = btn.gameObject.AddComponent<Button>();
                    b.targetGraphic = btn;
                    var act = a.OnClick;
                    b.onClick.AddListener(() => act?.Invoke());
                }

                var body = Label("Body", side.rectTransform, _regular, 21f, TextSub, TextAlignmentOptions.TopLeft);
                Place(body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(26f, buttonsTop + 6f), new Vector2(-26f, -78f));
                body.textWrappingMode = TextWrappingModes.Normal;
                body.overflowMode = TextOverflowModes.Truncate;
                body.lineSpacing = 8f;
                body.text = string.Join("\n", v.Detail);
            }

            // 아래: 알림 줄과 단추
            if (v.Notes.Count > 0)
                Keep(Text(_root, string.Join("     ", v.Notes), _regular, 20f, Soft, TextAlignmentOptions.Left,
                    new Vector2(60f, 40f), new Vector2(-420f, 110f), anchorBottom: true));

            if (!string.IsNullOrEmpty(v.PrimaryLabel))
            {
                var btn = Box("Primary", _root, Hex("C9A86A"));
                btn.raycastTarget = true;
                var rt = btn.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-60f, 40f);
                rt.sizeDelta = new Vector2(340f, 66f);
                Keep(btn.gameObject);
                var t = Label("Label", rt, _bold, 25f, Hex("1B1E24"), TextAlignmentOptions.Center);
                Stretch(t.rectTransform);
                t.text = v.PrimaryLabel;
                var b = btn.gameObject.AddComponent<Button>();
                b.targetGraphic = btn;
                b.onClick.AddListener(() => v.OnPrimary?.Invoke());
            }
        }

        private void DrawTable(RectTransform table, View v)
        {
            float pad = 24f, rowH = 74f, headH = 50f;
            var cols = v.Columns ?? new string[0];
            var widths = v.ColumnWidths ?? new float[0];

            // 머리 줄
            float x = pad;
            for (int i = 0; i < cols.Length; i++)
            {
                float w = i < widths.Length ? widths[i] : 160f;
                var h = Label("Head" + i, table, _medium, 19f, TextDim, i == 0 ? TextAlignmentOptions.Left : TextAlignmentOptions.Right);
                h.rectTransform.anchorMin = h.rectTransform.anchorMax = new Vector2(0f, 1f);
                h.rectTransform.pivot = new Vector2(0f, 1f);
                h.rectTransform.anchoredPosition = new Vector2(x, -14f);
                h.rectTransform.sizeDelta = new Vector2(w, headH - 14f);
                h.text = cols[i];
                x += w;
            }
            var rule = Box("Rule", table, Line);
            Place(rule.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(pad, -headH - 1f), new Vector2(-pad, -headH));

            for (int r = 0; r < v.Rows.Count; r++)
            {
                var row = v.Rows[r];
                bool selected = !string.IsNullOrEmpty(v.SelectedKey) && row.Key == v.SelectedKey;
                var bg = Box("Row" + r, table, selected ? Hex("2C3540") : CardColor);
                var rt = bg.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.offsetMin = new Vector2(pad - 10f, -headH - 12f - (r + 1) * (rowH + 8f) + 8f);
                rt.offsetMax = new Vector2(-pad + 10f, -headH - 12f - r * (rowH + 8f));
                if (selected) Border(rt, Accent, 2f);

                float cx = 10f;
                for (int i = 0; i < cols.Length; i++)
                {
                    float w = i < widths.Length ? widths[i] : 160f;
                    string text = row.Cells != null && i < row.Cells.Length ? row.Cells[i] : string.Empty;
                    Color color = TextMain;
                    if (i == row.GradeCell) color = GradeColor(row.GradeLevel);
                    if (i == row.DeltaCell) color = row.DeltaSign > 0 ? Up : row.DeltaSign < 0 ? Down : TextDim;

                    if (i == 0)
                    {
                        var name = Label("Name", rt, _bold, 24f, TextMain, TextAlignmentOptions.Left);
                        Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(cx + 4f, -2f), new Vector2(cx + w, -6f));
                        name.text = row.Name + (string.IsNullOrEmpty(row.Tag) ? string.Empty
                            : "   <size=70%><color=#" + ColorUtility.ToHtmlStringRGB(row.TagDanger ? Up : Soft) + ">" + row.Tag + "</color></size>");
                        var sub = Label("Sub", rt, _regular, 18f, TextDim, TextAlignmentOptions.Left);
                        Place(sub.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(cx + 4f, 6f), new Vector2(cx + w, 2f));
                        sub.text = row.Sub;
                    }
                    else
                    {
                        var cell = Label("Cell" + i, rt, i == row.GradeCell ? _bold : _medium, 24f, color, TextAlignmentOptions.Right);
                        Place(cell.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(cx, 0f), new Vector2(cx + w, 0f));
                        cell.text = text;
                    }
                    cx += w;
                }

                if (v.OnSelect != null && !string.IsNullOrEmpty(row.Key))
                {
                    bg.raycastTarget = true;
                    var b = bg.gameObject.AddComponent<Button>();
                    b.targetGraphic = bg;
                    string key = row.Key;
                    b.onClick.AddListener(() => v.OnSelect(key));
                }
            }

            if (v.Rows.Count == 0)
            {
                var empty = Label("Empty", table, _regular, 22f, TextDim, TextAlignmentOptions.Center);
                Place(empty.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 20f), new Vector2(-20f, -headH - 20f));
                empty.text = "—";
            }
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
