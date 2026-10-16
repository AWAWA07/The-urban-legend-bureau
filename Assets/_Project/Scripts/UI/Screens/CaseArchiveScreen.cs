using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 숙소 컴퓨터의 수사 노트 앱 첫 화면. 맡았던 사건이 카드 한 장씩 도감처럼 남는다.
    ///
    /// 카드를 누르면 그 사건의 수사 노트(CaseFileScreen)가 펼쳐지고, 닫으면 이 목록으로 돌아온다.
    /// 보고서를 확정한 사건은 "해결" 도장과 판정이 찍힌 채 남는다. 아직 맡지 않은 자리는 ??? 로 잠겨 있다.
    /// 무엇을 보여줄지는 밖(CaseDirector)이 정한다. 기록은 저장본(수사 노트 기록)에서 읽으므로 따로 저장할 것이 없다.
    /// 처음 열 때 스스로 짓는다. 수사 노트와 같은 어두운 빛깔을 쓴다.
    /// </summary>
    public class CaseArchiveScreen : UIScreen
    {
        [SerializeField] private TMP_FontAsset _regular;
        [SerializeField] private TMP_FontAsset _medium;
        [SerializeField] private TMP_FontAsset _bold;

        /// <summary>카드 한 장. 문구는 이미 번역된 것이다.</summary>
        public class Entry
        {
            public string CaseId;
            public string Number;       // ULB-2026-001
            public string Name;         // 막차의 빈자리
            public string Legend;       // 괴담 · 막차 귀신
            public string Progress;     // 단서 5/5 · 분석 2/2 · 추론 3/3
            public string Verdict;      // 판정: 진짜 · 보고서 확정 (해결한 사건만)
            public bool Solved;
            public bool Active;         // 지금 맡고 있는 사건
        }

        private Func<List<Entry>> _entries;
        private Action<string> _onOpen;
        private Action _onClose;

        private static readonly Color Bg = Hex("101318");
        private static readonly Color CardColor = Hex("252C35");
        private static readonly Color CardLocked = Hex("181C22");
        private static readonly Color Line = Hex("39424E");
        private static readonly Color TextMain = Hex("E4E8ED");
        private static readonly Color TextSub = Hex("9BA7B5");
        private static readonly Color TextDim = Hex("66717E");
        private static readonly Color Accent = Hex("6B94A8");
        private static readonly Color Bureau = Hex("B65B61");
        private static readonly Color Soft = Hex("C9A86A");
        private static readonly Color Solved = Hex("5DB97C");

        private const float CardW = 520f, CardH = 320f, Gap = 40f;
        private const int Columns = 3;

        private RectTransform _content;
        private TMP_Text _countText;
        private readonly List<GameObject> _cards = new List<GameObject>();
        private bool _built;

        private const string BureauTextId = "ui.archive.bureau";
        private const string TitleTextId = "ui.archive.title";
        private const string SubTextId = "ui.archive.sub";
        private const string CountTextId = "ui.archive.count";
        private const string SolvedTextId = "ui.archive.solved";
        private const string ActiveTextId = "ui.archive.active";
        private const string StampTextId = "ui.archive.stamp";
        private const string OpenTextId = "ui.archive.open";
        private const string LockedNameTextId = "ui.archive.locked_name";
        private const string LockedDescTextId = "ui.archive.locked_desc";
        private const string CloseTextId = "ui.casefile.close";

        public void Bind(Func<List<Entry>> entries, Action<string> onOpen, Action onClose)
        {
            _entries = entries;
            _onOpen = onOpen;
            _onClose = onClose;
            if (IsOpen) Refresh();
        }

        protected override void OnOpen()
        {
            EventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);
            Refresh();
        }

        protected override void OnClose()
        {
            EventBus.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);
        }

        /// <summary>펼쳤던 수사 노트를 닫고 돌아오면 그 사이 바뀐 것(해결 도장, 진행)을 다시 그린다.</summary>
        protected override void OnCoveredChanged(bool covered)
        {
            if (!covered) Refresh();
        }

        private void OnLanguageChanged(LanguageChangedEvent evt) => Refresh();

        private string L(string id, params object[] args)
        {
            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc)) return id;
            return args == null || args.Length == 0 ? loc.Get(id) : loc.Get(id, args);
        }

        // ------------------------------------------------------------- 짓기

        private TMP_Text _bureau, _title, _sub, _closeLabel;

        private void Build()
        {
            if (_built) return;
            _built = true;

            var root = (RectTransform)transform;
            var bg = GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = Bg;

            // 머리: 왼쪽에 기관 이름과 제목, 오른쪽에 해결 수와 닫기.
            var edge = Box("Edge", root, Bureau);
            Place(edge.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -150f), new Vector2(64f, -48f));

            _bureau = Label("Bureau", root, _medium, 22f, Bureau, TextAlignmentOptions.Left);
            Place(_bureau.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(84f, -82f), new Vector2(-600f, -46f));
            _title = Label("Title", root, _bold, 46f, TextMain, TextAlignmentOptions.Left);
            Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(84f, -140f), new Vector2(-600f, -82f));
            _sub = Label("Sub", root, _regular, 21f, TextSub, TextAlignmentOptions.Left);
            Place(_sub.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(84f, -186f), new Vector2(-600f, -150f));

            _countText = Label("Count", root, _medium, 24f, TextSub, TextAlignmentOptions.Right);
            Place(_countText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-560f, -122f), new Vector2(-260f, -70f));

            var close = Box("Close", root, Hex("1B2028"));
            close.raycastTarget = true;
            Place(close.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-230f, -130f), new Vector2(-60f, -62f));
            Border(close.rectTransform, Line, 1f);
            _closeLabel = Label("Label", close.rectTransform, _bold, 24f, TextMain, TextAlignmentOptions.Center);
            Stretch(_closeLabel.rectTransform);
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = close;
            closeButton.onClick.AddListener(() => _onClose?.Invoke());

            var rule = Box("Rule", root, Line);
            Place(rule.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(60f, -211f), new Vector2(-60f, -210f));

            _content = NewRect("Cards", root);
            _content.anchorMin = _content.anchorMax = new Vector2(0.5f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = new Vector2(0f, -260f);
            _content.sizeDelta = new Vector2(Columns * CardW + (Columns - 1) * Gap, 2f * CardH + Gap);
        }

        public void Refresh()
        {
            Build();

            _bureau.text = L(BureauTextId);
            _title.text = L(TitleTextId);
            _sub.text = L(SubTextId);
            _closeLabel.text = L(CloseTextId);

            foreach (var c in _cards) if (c != null) { c.SetActive(false); Destroy(c); }
            _cards.Clear();

            var list = _entries != null ? _entries() : new List<Entry>();
            int solved = 0;
            foreach (var e in list) if (e.Solved) solved++;
            _countText.text = L(CountTextId, solved, list.Count);

            // 한 줄을 다 채우고, 맡지 않은 자리는 ??? 로 잠가 둔다. 도감처럼 앞으로 채워 갈 자리가 보인다.
            int slots = Mathf.Max(Columns, Mathf.CeilToInt(list.Count / (float)Columns) * Columns);
            for (int i = 0; i < slots; i++)
            {
                var card = i < list.Count ? BuildCard(list[i]) : BuildLocked();
                var rt = (RectTransform)card.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(CardW, CardH);
                rt.anchoredPosition = new Vector2((i % Columns) * (CardW + Gap), -(i / Columns) * (CardH + Gap));
                _cards.Add(card);
            }
        }

        private GameObject BuildCard(Entry e)
        {
            var bg = Box("Case_" + e.CaseId, _content, CardColor);
            bg.raycastTarget = true;
            var rt = bg.rectTransform;
            Border(rt, e.Active && !e.Solved ? Accent : Line, e.Active && !e.Solved ? 2f : 1f);

            // 왼쪽 가는 띠. 해결한 사건은 초록, 맡고 있는 사건은 파랑.
            var bar = Box("Bar", rt, e.Solved ? Solved : Accent);
            Place(bar.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(5f, 0f));

            var number = Label("Number", rt, _medium, 20f, TextDim, TextAlignmentOptions.Left);
            Place(number.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -56f), new Vector2(-170f, -24f));
            number.text = e.Number;

            var chip = Box("Status", rt, e.Solved ? Hex("1E3A2A") : Hex("3A3220"));
            Place(chip.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150f, -60f), new Vector2(-28f, -24f));
            var chipText = Label("Text", chip.rectTransform, _bold, 19f, e.Solved ? Solved : Soft, TextAlignmentOptions.Center);
            Stretch(chipText.rectTransform);
            chipText.text = L(e.Solved ? SolvedTextId : ActiveTextId);

            var name = Label("Name", rt, _bold, 36f, TextMain, TextAlignmentOptions.Left);
            Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -116f), new Vector2(-28f, -66f));
            name.text = e.Name;

            var legend = Label("Legend", rt, _regular, 21f, TextSub, TextAlignmentOptions.Left);
            Place(legend.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -152f), new Vector2(-28f, -120f));
            legend.text = e.Legend;

            var rule = Box("Rule", rt, Line);
            Place(rule.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -171f), new Vector2(-28f, -170f));

            var progress = Label("Progress", rt, _regular, 20f, TextSub, TextAlignmentOptions.Left);
            Place(progress.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -214f), new Vector2(-28f, -184f));
            progress.text = e.Progress;

            if (!string.IsNullOrEmpty(e.Verdict))
            {
                var verdict = Label("Verdict", rt, _medium, 21f, Soft, TextAlignmentOptions.Left);
                Place(verdict.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -250f), new Vector2(-28f, -218f));
                verdict.text = e.Verdict;
            }

            var open = Label("Open", rt, _bold, 22f, Accent, TextAlignmentOptions.Right);
            Place(open.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(32f, 20f), new Vector2(-28f, 56f));
            open.text = L(OpenTextId);

            // 해결한 사건에는 붉은 도장을 비스듬히 찍는다.
            if (e.Solved)
            {
                var stamp = NewRect("Stamp", rt);
                stamp.anchorMin = stamp.anchorMax = new Vector2(1f, 0f);
                stamp.pivot = new Vector2(0.5f, 0.5f);
                stamp.sizeDelta = new Vector2(150f, 62f);
                stamp.anchoredPosition = new Vector2(-120f, 112f);
                stamp.localRotation = Quaternion.Euler(0f, 0f, 12f);
                var ink = new Color(Bureau.r, Bureau.g, Bureau.b, 0.85f);
                Border(stamp, ink, 3f);
                var stampText = Label("Text", stamp, _bold, 32f, ink, TextAlignmentOptions.Center);
                Stretch(stampText.rectTransform);
                stampText.characterSpacing = 18f;
                stampText.text = L(StampTextId);
            }

            var button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            button.colors = colors;
            string id = e.CaseId;
            button.onClick.AddListener(() => _onOpen?.Invoke(id));
            return bg.gameObject;
        }

        private GameObject BuildLocked()
        {
            var bg = Box("Locked", _content, CardLocked);
            var rt = bg.rectTransform;
            Border(rt, Hex("262C34"), 1f);

            var name = Label("Name", rt, _bold, 44f, TextDim, TextAlignmentOptions.Center);
            Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(20f, 0f), new Vector2(-20f, 60f));
            name.text = L(LockedNameTextId);

            var desc = Label("Desc", rt, _regular, 21f, TextDim, TextAlignmentOptions.Center);
            Place(desc.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(20f, -44f), new Vector2(-20f, -4f));
            desc.text = L(LockedDescTextId);
            return bg.gameObject;
        }

        // ------------------------------------------------------------- 조각

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
