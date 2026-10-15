using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.Localization;
using UrbanLegendBureau.Save;
using UrbanLegendBureau.Systems;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 수사 노트. 사건 파일을 펼쳐 보는 검열국 단말기 화면이다.
    ///
    /// 왼쪽 줄에서 쪽을 고른다.
    ///   기록 - 수집한 단서 / 분석 기록 / 추론 기록
    ///   수사 - 단서 분석 / 최종 추론 / 조사 보고서
    ///
    /// 화면을 이루는 것들은 처음 열 때 스스로 짓는다. 씬에는 이 컴포넌트가 붙은 빈 판 하나와 글꼴만 걸려 있다.
    /// 무엇이 맞고 틀리는지는 사건 자료(CaseDeductionSO)가 정하고, 진행은 저장본(CaseNotebook)에 바로 적는다.
    /// 보고서를 확정한 뒤에 할 일(메모장, 숙소, 게시물)은 여는 쪽(CaseDirector)이 정한다.
    /// 시간, 확산도, 믿음도는 여기서 하나도 건드리지 않는다.
    /// </summary>
    public class CaseFileScreen : UIScreen
    {
        public enum Page { Clues, Analyses, Deductions, Analyze, Deduce, Report }

        /// <summary>여는 쪽이 넘겨주는 것들.</summary>
        public class Context
        {
            public CaseDeductionSO Data;
            public string CaseNameTextId;
            public Func<string, string> ClueTitle;
            public Func<string, string> ClueText;

            /// <summary>최종 추론을 시작해도 되는가(현장 조사가 끝났는가). 비워 두면 늘 된다.</summary>
            public Func<bool> FieldClosed;

            /// <summary>추론 하나를 맞혔을 때.</summary>
            public Action<CaseDeductionSO.Question> OnSolved;

            /// <summary>세 추론을 다 맞혀 보고서 쪽으로 처음 넘어갈 때.</summary>
            public Action OnAllSolved;

            /// <summary>조사 결과 확정을 눌렀을 때.</summary>
            public Action OnConfirm;

            /// <summary>화면이 닫힐 때(닫기 단추, ESC).</summary>
            public Action OnClosed;
        }

        [Header("글꼴 (Pretendard)")]
        [SerializeField] private TMP_FontAsset _regular;
        [SerializeField] private TMP_FontAsset _medium;
        [SerializeField] private TMP_FontAsset _semiBold;
        [SerializeField] private TMP_FontAsset _bold;

        // ------------------------------------------------------------- 색

        private static readonly Color Bg = Hex("101318");
        private static readonly Color PanelColor = Hex("1B2028");
        private static readonly Color CardColor = Hex("252C35");
        private static readonly Color CardHover = Hex("2C3540");
        private static readonly Color Line = Hex("39424E");
        private static readonly Color TextMain = Hex("E4E8ED");
        private static readonly Color TextSub = Hex("9BA7B5");
        private static readonly Color TextDim = Hex("66717E");
        private static readonly Color Accent = Hex("6B94A8");
        private static readonly Color AccentDeep = Hex("2E4552");
        private static readonly Color Bureau = Hex("B65B61");
        private static readonly Color Soft = Hex("C9A86A");

        private static Color Hex(string hex, float a = 1f)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            c.a = a;
            return c;
        }

        // ------------------------------------------------------------- 문구

        private const string T = "ui.casefile.";

        // ------------------------------------------------------------- 상태

        private Context _ctx;
        private Page _page;
        private string _detailClueId;
        private readonly List<string> _picked = new List<string>();
        private string _compareMessageId;
        private int _deduceChoice = -1;
        private bool _deduceWrong;
        private string _deduceHitId;
        private string _evidenceKey;

        private bool _built;
        private RectTransform _root;
        private TMP_Text _caseTitle;
        private TMP_Text _stats;
        private RectTransform _nav;
        private RectTransform _body;
        private TMP_Text _closeLabel;
        private TMP_Text _bureauLabel;

        private CanvasGroup _overlay;

        private LocalizationService _loc;
        private SaveService _save;

        private SaveData Data => _save != null ? _save.Current : null;
        private CaseDeductionSO Deduction => _ctx != null ? _ctx.Data : null;

        // ------------------------------------------------------------- 밖에서

        public void Bind(Context ctx, Page page)
        {
            _ctx = ctx;
            _page = page;
            _picked.Clear();
            _compareMessageId = null;
            _deduceChoice = -1;
            _deduceWrong = false;
            _deduceHitId = null;
            _detailClueId = null;
            _evidenceKey = null;
            if (IsOpen) Refresh();
        }

        public Page CurrentPage => _page;

        public void ShowPage(Page page)
        {
            _page = page;
            _compareMessageId = null;
            _deduceWrong = false;
            if (IsOpen) Refresh();
        }

        protected override void OnOpen()
        {
            ServiceRegistry.TryGet(out _loc);
            ServiceRegistry.TryGet(out _save);
            Build();
            EventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);
            Sfx.PlayVaried("paper_" + UnityEngine.Random.Range(0, 2), 0.6f);
            Refresh();
            StartCoroutine(FadeIn(_root, 0.22f));
        }

        protected override void OnClose()
        {
            EventBus.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);
            HideOverlay();
            _save?.AutoSave();
            _ctx?.OnClosed?.Invoke();
        }

        private void OnLanguageChanged(LanguageChangedEvent evt) => Refresh();

        private string L(string id) => _loc != null ? _loc.Get(id) : id;
        private string L(string id, params object[] args) => _loc != null ? _loc.Get(id, args) : id;

        // ------------------------------------------------------------- 뼈대

        private void Build()
        {
            if (_built) return;
            _built = true;

            var self = (RectTransform)transform;
            Stretch(self);

            var bg = Ensure<Image>(gameObject);
            bg.color = Bg;
            bg.raycastTarget = true;

            _root = NewRect("Root", self);
            Stretch(_root);
            _root.gameObject.AddComponent<CanvasGroup>();

            // 머리. 왼쪽에 검열국 띠와 사건명, 오른쪽에 진행도와 닫기.
            var header = NewRect("Header", _root);
            Place(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -120f), new Vector2(-40f, -24f));

            var mark = Box("BureauMark", header, Bureau);
            Place(mark.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 6f), new Vector2(6f, -6f));

            _bureauLabel = Label("Bureau", header, _semiBold, 20f, Bureau, TextAlignmentOptions.TopLeft);
            Place(_bureauLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0.6f, 1f), new Vector2(26f, -34f), new Vector2(0f, -4f));
            _bureauLabel.characterSpacing = 6f;

            _caseTitle = Label("CaseTitle", header, _bold, 40f, TextMain, TextAlignmentOptions.BottomLeft);
            Place(_caseTitle.rectTransform, new Vector2(0f, 0f), new Vector2(0.62f, 1f), new Vector2(24f, 2f), new Vector2(0f, -36f));

            var close = MakeButton("Close", header, PanelColor, out _closeLabel, _medium, 24f);
            Place((RectTransform)close.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-140f, -32f), new Vector2(0f, 32f));
            close.onClick.AddListener(RequestClose);
            Outline(close.gameObject, Line);

            _stats = Label("Stats", header, _medium, 22f, TextSub, TextAlignmentOptions.Right);
            Place(_stats.rectTransform, new Vector2(0.45f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(-170f, 0f));

            var rule = Box("HeaderLine", _root, Line);
            Place(rule.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -137f), new Vector2(-40f, -135f));

            _nav = NewRect("Nav", _root);
            Place(_nav, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(40f, 40f), new Vector2(360f, -160f));

            _body = NewRect("Body", _root);
            Place(_body, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(392f, 40f), new Vector2(-40f, -160f));

            var navLine = Box("NavLine", _root, Line);
            Place(navLine.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(375f, 40f), new Vector2(377f, -160f));
        }

        public void Refresh()
        {
            if (!_built || _ctx == null || Deduction == null) return;

            _bureauLabel.text = L(T + "bureau");
            _caseTitle.text = L(_ctx.CaseNameTextId);
            _closeLabel.text = L(T + "close");
            _stats.text = L(T + "stat_clues", ClueCount(), Deduction.FieldClues.Count) + "     " +
                          L(T + "stat_analyses", CaseNotebook.AnalysisCount(Data, Deduction), Deduction.Analyses.Count) + "     " +
                          L(T + "stat_deductions", CaseNotebook.SolvedCount(Data, Deduction), Deduction.Questions.Count);

            BuildNav();

            Clear(_body);
            switch (_page)
            {
                case Page.Clues: BuildCluesPage(); break;
                case Page.Analyses: BuildAnalysesPage(); break;
                case Page.Deductions: BuildDeductionsPage(); break;
                case Page.Analyze: BuildAnalyzePage(); break;
                case Page.Deduce: BuildDeducePage(); break;
                case Page.Report: BuildReportPage(); break;
            }
        }

        private int ClueCount() => CaseNotebook.AcquiredClues(Data, Deduction).Count;

        private bool DeduceUnlocked => CaseNotebook.AllAnalysesFound(Data, Deduction) && (_ctx.FieldClosed == null || _ctx.FieldClosed());
        private bool ReportUnlocked => CaseNotebook.AllSolved(Data, Deduction);

        // ------------------------------------------------------------- 왼쪽 줄

        private void BuildNav()
        {
            Clear(_nav);
            var col = Column(_nav, 10f, 0f);
            Stretch(col);

            NavGroup(col, L(T + "group_records"));
            NavItem(col, Page.Clues, L(T + "tab_clues"), ClueCount() + "/" + Deduction.FieldClues.Count, true);
            NavItem(col, Page.Analyses, L(T + "tab_analyses"), CaseNotebook.AnalysisCount(Data, Deduction) + "/" + Deduction.Analyses.Count, true);
            NavItem(col, Page.Deductions, L(T + "tab_deductions"), CaseNotebook.SolvedCount(Data, Deduction) + "/" + Deduction.Questions.Count, true);

            Spacer(col, 18f);
            NavGroup(col, L(T + "group_work"));
            NavItem(col, Page.Analyze, L(T + "tab_analyze"), null, true);
            NavItem(col, Page.Deduce, L(T + "tab_deduce"), DeduceUnlocked ? null : LockMark, true);
            NavItem(col, Page.Report, L(T + "tab_report"), ReportUnlocked ? null : LockMark, true);
        }

        private void NavGroup(RectTransform parent, string text)
        {
            var label = Label("Group", parent, _semiBold, 18f, TextDim, TextAlignmentOptions.BottomLeft);
            label.text = text;
            label.characterSpacing = 4f;
            Layout(label.gameObject, 34f);
        }

        private void NavItem(RectTransform parent, Page page, string text, string right, bool enabled)
        {
            bool on = _page == page;
            var button = MakeButton("Nav_" + page, parent, on ? AccentDeep : PanelColor, out var label, on ? _semiBold : _medium, 24f);
            Layout(button.gameObject, 66f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.margin = new Vector4(26f, 0f, 70f, 0f);
            label.text = text;
            label.enableAutoSizing = true;
            label.fontSizeMin = 17f;
            label.fontSizeMax = 24f;
            label.color = on ? TextMain : TextSub;

            if (on)
            {
                var bar = Box("Bar", button.transform, Accent);
                Place(bar.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(4f, 0f));
            }

            if (right == LockMark)
            {
                // 잠긴 쪽에는 글 대신 작은 자물쇠를 단다. 어느 말로 열어도 자리를 넘치지 않는다.
                var shackle = Box("Shackle", button.transform, TextDim);
                shackle.sprite = RingSprite();
                Place(shackle.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-37f, -1f), new Vector2(-25f, 11f));
                var body = Box("Lock", button.transform, TextDim);
                Place(body.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-39f, -10f), new Vector2(-23f, 2f));
                right = null;
            }

            if (!string.IsNullOrEmpty(right))
            {
                var r = Label("Count", button.transform, _medium, 19f, on ? TextMain : TextDim, TextAlignmentOptions.MidlineRight);
                Place(r.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(-18f, 0f));
                r.text = right;
            }

            button.interactable = enabled;
            button.onClick.AddListener(() => ShowPage(page));
        }

        // ------------------------------------------------------------- 수집한 단서

        private void BuildCluesPage()
        {
            var list = Scroll("ClueList", _body, out var content, 12f);
            Place((RectTransform)list.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(600f, 0f));

            var acquired = CaseNotebook.AcquiredClues(Data, Deduction);
            if (_detailClueId == null && acquired.Count > 0) _detailClueId = acquired[0].clueId;

            for (int i = 0; i < Deduction.FieldClues.Count; i++)
            {
                var clue = Deduction.FieldClues[i];
                if (clue == null) continue;
                bool has = CaseNotebook.HasClue(Data, clue.clueId);
                ClueCard(content, clue, has, has && clue.clueId == _detailClueId, () =>
                {
                    _detailClueId = clue.clueId;
                    Refresh();
                });
            }

            var detail = Panel("Detail", _body);
            Place(detail.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(624f, 0f), Vector2.zero);
            BuildClueDetail(detail.rectTransform);
            StartCoroutine(FadeIn(detail.rectTransform, 0.18f));
        }

        private void ClueCard(RectTransform parent, CaseDeductionSO.FieldClue clue, bool has, bool selected, Action onClick)
        {
            var button = MakeButton("Clue_" + clue.clueId, parent, selected ? CardHover : CardColor, out var label, _semiBold, 26f);
            Layout(button.gameObject, 116f);
            label.gameObject.SetActive(false);
            if (selected) Outline(button.gameObject, Accent);

            Icon(button.transform, new Vector2(42f, 0f), has ? Accent : TextDim);

            var number = Label("Number", button.transform, _bold, 20f, has ? Accent : TextDim, TextAlignmentOptions.TopLeft);
            Place(number.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(76f, -40f), new Vector2(-20f, -14f));
            number.text = "[" + ShortId(clue.clueId) + "]";

            var title = Label("Title", button.transform, _semiBold, 27f, has ? TextMain : TextDim, TextAlignmentOptions.TopLeft);
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(76f, -76f), new Vector2(-150f, -38f));
            title.text = has ? _ctx.ClueTitle(clue.clueId) : L(T + "undiscovered_clue");
            title.overflowMode = TextOverflowModes.Ellipsis;

            var place = Label("Place", button.transform, _regular, 20f, TextSub, TextAlignmentOptions.TopLeft);
            Place(place.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(76f, -106f), new Vector2(-150f, -78f));
            place.text = has ? L(T + "place", L(clue.placeTextId)) : "—";

            var state = Label("State", button.transform, _medium, 19f, has ? Accent : TextDim, TextAlignmentOptions.MidlineRight);
            Place(state.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-150f, 0f), new Vector2(-20f, 0f));
            state.text = has ? L(T + "state_found") : L(T + "undiscovered");

            button.interactable = has;
            if (has) button.onClick.AddListener(() => onClick());
        }

        private void BuildClueDetail(RectTransform panel)
        {
            if (string.IsNullOrEmpty(_detailClueId) || !CaseNotebook.HasClue(Data, _detailClueId))
            {
                var empty = Label("Empty", panel, _regular, 24f, TextDim, TextAlignmentOptions.Center);
                Stretch(empty.rectTransform);
                empty.text = L(T + "detail_empty");
                return;
            }

            var clue = Deduction.GetFieldClue(_detailClueId);
            var scroll = Scroll("DetailScroll", panel, out var content, 14f, new RectOffset(44, 44, 40, 40));
            Stretch((RectTransform)scroll.transform);

            Para(content, L(T + "detail_number", ShortId(_detailClueId)), _semiBold, 21f, Accent);
            Para(content, _ctx.ClueTitle(_detailClueId), _bold, 38f, TextMain);
            Para(content, L(T + "place", clue != null ? L(clue.placeTextId) : "—") + "     " + L(T + "detail_case", L(_ctx.CaseNameTextId)), _regular, 21f, TextSub);
            Divider(content);
            Para(content, _ctx.ClueText(_detailClueId), _regular, 28f, TextMain, 10f);
            Spacer(content, 12f);
            Divider(content);
            Para(content, L(T + "detail_related"), _semiBold, 22f, TextSub);

            int related = 0;
            foreach (var a in Deduction.Analyses)
            {
                if (a == null || (a.clueA != _detailClueId && a.clueB != _detailClueId)) continue;
                if (!CaseNotebook.HasAnalysis(Data, Deduction, a.analysisId)) continue;
                Para(content, "· " + L(a.titleTextId), _medium, 24f, TextMain);
                related++;
            }
            if (related == 0) Para(content, L(T + "detail_no_related"), _regular, 22f, TextDim);
        }

        // ------------------------------------------------------------- 분석 기록 / 추론 기록

        private void BuildAnalysesPage()
        {
            var scroll = Scroll("Analyses", _body, out var content, 16f);
            Place((RectTransform)scroll.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-260f, 0f));

            PageTitle(content, L(T + "tab_analyses"), L(T + "stat_analyses", CaseNotebook.AnalysisCount(Data, Deduction), Deduction.Analyses.Count));
            foreach (var a in Deduction.Analyses)
            {
                if (a == null) continue;
                AnalysisCard(content, a, CaseNotebook.HasAnalysis(Data, Deduction, a.analysisId));
            }
        }

        private void AnalysisCard(RectTransform parent, CaseDeductionSO.Analysis a, bool found)
        {
            var card = Panel("Analysis_" + a.analysisId, parent, found ? CardColor : PanelColor);
            var col = card.gameObject.AddComponent<VerticalLayoutGroup>();
            col.padding = new RectOffset(40, 40, 30, 30);
            col.spacing = 10f;
            col.childControlHeight = true;
            col.childControlWidth = true;
            col.childForceExpandHeight = false;
            Icon(card.transform, new Vector2(20f, -44f), found ? Accent : TextDim, true);

            if (!found)
            {
                Para(card.rectTransform, L(T + "analysis_locked"), _semiBold, 26f, TextDim);
                Para(card.rectTransform, L(T + "undiscovered"), _regular, 21f, TextDim);
                return;
            }

            Para(card.rectTransform, L(a.titleTextId), _bold, 30f, TextMain);
            Para(card.rectTransform, L(a.bodyTextId), _regular, 25f, TextMain, 8f);
            Para(card.rectTransform, L(T + "analysis_used", ClueTag(a.clueA) + "  +  " + ClueTag(a.clueB)), _medium, 21f, Accent);
        }

        private void BuildDeductionsPage()
        {
            var scroll = Scroll("Deductions", _body, out var content, 16f);
            Place((RectTransform)scroll.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-260f, 0f));

            PageTitle(content, L(T + "tab_deductions"), L(T + "stat_deductions", CaseNotebook.SolvedCount(Data, Deduction), Deduction.Questions.Count));
            for (int i = 0; i < Deduction.Questions.Count; i++)
            {
                var q = Deduction.Questions[i];
                if (q == null) continue;
                bool solved = CaseNotebook.IsSolved(Data, Deduction, q.questionId);

                var card = Panel("Deduction_" + i, content, solved ? CardColor : PanelColor);
                var col = card.gameObject.AddComponent<VerticalLayoutGroup>();
                col.padding = new RectOffset(40, 40, 28, 28);
                col.spacing = 8f;
                col.childControlHeight = true;
                col.childControlWidth = true;
                col.childForceExpandHeight = false;

                Para(card.rectTransform, L(T + "deduce_step", i + 1, L(q.labelTextId)), _semiBold, 21f, solved ? Accent : TextDim);
                Para(card.rectTransform, solved ? L(q.reportTextId) : L(T + "deduction_pending"), solved ? _bold : _medium, 28f, solved ? TextMain : TextDim);
            }
        }

        // ------------------------------------------------------------- 단서 분석

        private void BuildAnalyzePage()
        {
            // 왼쪽: 고를 수 있는 단서
            var left = Panel("Pick", _body);
            Place(left.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(420f, 0f));
            var leftHead = Label("Head", left.rectTransform, _semiBold, 22f, TextSub, TextAlignmentOptions.MidlineLeft);
            Place(leftHead.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -64f), new Vector2(-20f, -10f));
            leftHead.text = L(T + "tab_clues");

            var list = Scroll("List", left.rectTransform, out var content, 10f, new RectOffset(18, 18, 6, 18));
            Place((RectTransform)list.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -70f));

            foreach (var clue in CaseNotebook.AcquiredClues(Data, Deduction))
            {
                int slot = _picked.IndexOf(clue.clueId);
                var button = MakeButton("Pick_" + clue.clueId, content, slot >= 0 ? CardHover : CardColor, out var label, _semiBold, 22f);
                Layout(button.gameObject, 92f);
                if (slot >= 0) Outline(button.gameObject, Accent);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.margin = new Vector4(26f, 0f, 70f, 0f);
                label.text = "<size=80%><color=#" + ColorUtility.ToHtmlStringRGB(Accent) + ">" + ShortId(clue.clueId) + "</color></size>   " + _ctx.ClueTitle(clue.clueId);
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.enableWordWrapping = true;
                label.lineSpacing = -6f;

                // 고른 단서에는 몇 번째 칸에 들어갔는지 둥근 표를 단다.
                var check = Box("Check", button.transform, slot >= 0 ? Accent : Line);
                Place(check.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-58f, -17f), new Vector2(-24f, 17f));
                check.sprite = CircleSprite();
                var mark = Label("Mark", check.transform, _bold, 19f, slot >= 0 ? Bg : TextDim, TextAlignmentOptions.Center);
                Stretch(mark.rectTransform);
                mark.text = slot >= 0 ? (slot + 1).ToString() : string.Empty;

                string id = clue.clueId;
                button.onClick.AddListener(() => TogglePick(id));
            }

            // 오른쪽: 찾아낸 분석 기록
            var right = Panel("Found", _body);
            Place(right.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-360f, 0f), Vector2.zero);
            var rightScroll = Scroll("FoundList", right.rectTransform, out var found, 12f, new RectOffset(24, 24, 24, 24));
            Stretch((RectTransform)rightScroll.transform);
            Para(found, L(T + "found_records"), _semiBold, 22f, TextSub);
            Para(found, L(T + "stat_analyses", CaseNotebook.AnalysisCount(Data, Deduction), Deduction.Analyses.Count), _bold, 30f, TextMain);
            Divider(found);
            foreach (var a in Deduction.Analyses)
            {
                if (a == null) continue;
                bool has = CaseNotebook.HasAnalysis(Data, Deduction, a.analysisId);
                var card = Panel("Rec_" + a.analysisId, found, has ? CardColor : Bg);
                var col = card.gameObject.AddComponent<VerticalLayoutGroup>();
                col.padding = new RectOffset(22, 22, 16, 16);
                col.spacing = 4f;
                col.childControlHeight = true;
                col.childControlWidth = true;
                col.childForceExpandHeight = false;
                Para(card.rectTransform, has ? L(a.titleTextId) : L(T + "analysis_locked"), _semiBold, 22f, has ? TextMain : TextDim);
                Para(card.rectTransform, has ? ClueTag(a.clueA) + " + " + ClueTag(a.clueB) : L(T + "undiscovered"), _regular, 19f, has ? Accent : TextDim);
            }

            if (CaseNotebook.AllAnalysesFound(Data, Deduction))
            {
                Spacer(found, 8f);
                var go = MakeButton("ToDeduce", found, Accent, out var goLabel, _semiBold, 24f);
                Layout(go.gameObject, 72f);
                goLabel.text = L(T + "to_deduce");
                goLabel.color = Bg;
                go.onClick.AddListener(() => ShowPage(Page.Deduce));
            }

            // 가운데: 맞대어 볼 두 칸
            var mid = NewRect("Compare", _body);
            Place(mid, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(448f, 0f), new Vector2(-388f, 0f));

            var title = Label("Title", mid, _bold, 34f, TextMain, TextAlignmentOptions.TopLeft);
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -54f), Vector2.zero);
            title.text = L(T + "tab_analyze");
            var hint = Label("Hint", mid, _regular, 21f, TextSub, TextAlignmentOptions.TopLeft);
            Place(hint.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -122f), new Vector2(0f, -60f));
            hint.text = L(T + "analyze_hint");
            hint.enableWordWrapping = true;

            for (int i = 0; i < 2; i++) Slot(mid, i);

            var link = Box("Link", mid, Line);
            Place(link.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-30f, -346f), new Vector2(30f, -286f));
            link.sprite = CircleSprite();
            var plus = Label("Plus", link.transform, _bold, 30f, TextMain, TextAlignmentOptions.Center);
            Stretch(plus.rectTransform);
            plus.text = "+";

            var compare = MakeButton("CompareButton", mid, Accent, out var compareLabel, _semiBold, 28f);
            Place((RectTransform)compare.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-170f, -600f), new Vector2(170f, -524f));
            compareLabel.text = L(T + "compare");
            compareLabel.color = Bg;
            compare.interactable = _picked.Count == 2;
            compare.onClick.AddListener(OnCompare);

            if (!string.IsNullOrEmpty(_compareMessageId))
            {
                var msg = Label("Message", mid, _medium, 24f, Soft, TextAlignmentOptions.Top);
                Place(msg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -700f), new Vector2(0f, -620f));
                msg.text = L(_compareMessageId);
                StartCoroutine(FadeIn(msg.rectTransform, 0.25f));
            }
        }

        private void Slot(RectTransform parent, int index)
        {
            var slot = Panel("Slot_" + index, parent, PanelColor);
            float x0 = index == 0 ? 0f : 0.5f;
            Place(slot.rectTransform, new Vector2(x0, 1f), new Vector2(x0 + 0.5f, 1f),
                new Vector2(index == 0 ? 0f : 40f, -490f), new Vector2(index == 0 ? -40f : 0f, -140f));

            bool filled = index < _picked.Count;
            Outline(slot.gameObject, filled ? Accent : Line);

            var head = Label("Head", slot.rectTransform, _semiBold, 20f, filled ? Accent : TextDim, TextAlignmentOptions.TopLeft);
            Place(head.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -52f), new Vector2(-28f, -20f));
            head.text = L(T + "slot", index + 1) + (filled ? "  ·  " + ShortId(_picked[index]) : string.Empty);

            if (!filled)
            {
                var empty = Label("Empty", slot.rectTransform, _regular, 22f, TextDim, TextAlignmentOptions.Center);
                Stretch(empty.rectTransform);
                empty.text = L(T + "analyze_pick");
                return;
            }

            string id = _picked[index];
            var name = Label("Title", slot.rectTransform, _bold, 27f, TextMain, TextAlignmentOptions.TopLeft);
            Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -126f), new Vector2(-28f, -58f));
            name.text = _ctx.ClueTitle(id);
            name.enableWordWrapping = true;

            var body = Label("Body", slot.rectTransform, _regular, 20f, TextSub, TextAlignmentOptions.TopLeft);
            Place(body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(28f, 70f), new Vector2(-28f, -132f));
            body.text = _ctx.ClueText(id);
            body.enableWordWrapping = true;
            body.overflowMode = TextOverflowModes.Ellipsis;

            var clear = MakeButton("Clear", slot.rectTransform, CardColor, out var clearLabel, _medium, 20f);
            Place((RectTransform)clear.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-128f, 16f), new Vector2(-20f, 60f));
            clearLabel.text = L(T + "clear");
            clear.onClick.AddListener(() => { _picked.Remove(id); _compareMessageId = null; Refresh(); });
        }

        /// <summary>
        /// 단서를 고르거나 놓는다. 이미 고른 것을 다시 누르면 놓는다.
        /// 두 칸이 다 찼을 때 새 단서를 누르면 두 번째 칸을 그것으로 갈아 끼운다.
        /// </summary>
        private void TogglePick(string clueId)
        {
            if (_picked.Contains(clueId)) _picked.Remove(clueId);
            else if (_picked.Count < 2) _picked.Add(clueId);
            else _picked[1] = clueId;

            _compareMessageId = null;
            Refresh();
        }

        private void OnCompare()
        {
            if (_picked.Count != 2) return;

            var found = CaseNotebook.Compare(Data, Deduction, _picked[0], _picked[1], out bool isNew);
            if (found == null)
            {
                _compareMessageId = T + "compare_wrong";
                Debug.Log($"[CaseFile] 단서 분석 | {_picked[0]} + {_picked[1]} - 새로 알아낸 것 없음");
                Refresh();
                return;
            }

            if (!isNew)
            {
                _compareMessageId = T + "compare_known";
                Refresh();
                return;
            }

            _save?.MarkDirty();
            _save?.AutoSave();
            _picked.Clear();
            _compareMessageId = null;
            Sfx.Play("clue_get", 0.5f, 0.9f);
            Debug.Log($"[CaseFile] 단서 분석 | 분석 기록 발견 {found.analysisId} ({CaseNotebook.AnalysisCount(Data, Deduction)}/{Deduction.Analyses.Count})");
            Refresh();
            ShowDiscovery(found);
        }

        /// <summary>분석 기록을 찾았을 때 가운데에 잠깐 떠오르는 판. 확인을 누르면 걷힌다.</summary>
        private void ShowDiscovery(CaseDeductionSO.Analysis a)
        {
            HideOverlay();

            var dim = NewRect("Discovery", (RectTransform)transform);
            Stretch(dim);
            var dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.6f);
            _overlay = dim.gameObject.AddComponent<CanvasGroup>();

            var panel = Panel("Panel", dim, PanelColor);
            Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420f, -230f), new Vector2(420f, 230f));
            Outline(panel.gameObject, Accent);
            var top = Box("Top", panel.transform, Accent);
            Place(top.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -5f), Vector2.zero);

            var col = NewRect("Col", panel.rectTransform);
            Place(col, Vector2.zero, Vector2.one, new Vector2(48f, 110f), new Vector2(-48f, -40f));
            var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 14f;
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;

            Para(col, L(T + "found_title"), _semiBold, 22f, Accent);
            Para(col, L(a.titleTextId), _bold, 36f, TextMain);
            Para(col, L(a.bodyTextId), _regular, 25f, TextMain, 8f);
            Para(col, L(T + "analysis_used", ClueTag(a.clueA) + "  +  " + ClueTag(a.clueB)), _medium, 20f, TextSub);

            var ok = MakeButton("Ok", panel.rectTransform, Accent, out var okLabel, _semiBold, 26f);
            Place((RectTransform)ok.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-130f, 30f), new Vector2(130f, 94f));
            okLabel.text = L("ui.common.ok");
            okLabel.color = Bg;
            ok.onClick.AddListener(HideOverlay);

            StartCoroutine(PopIn(panel.rectTransform, _overlay, 0.4f));
        }

        private void HideOverlay()
        {
            if (_overlay != null) Destroy(_overlay.gameObject);
            _overlay = null;
        }

        // ------------------------------------------------------------- 최종 추론

        private void BuildDeducePage()
        {
            // 오른쪽: 근거 확인
            BuildEvidencePanel();

            var mid = NewRect("Deduce", _body);
            Place(mid, Vector2.zero, Vector2.one, new Vector2(24f, 0f), new Vector2(-480f, 0f));

            if (!DeduceUnlocked)
            {
                bool analysesDone = CaseNotebook.AllAnalysesFound(Data, Deduction);
                Locked(mid, analysesDone ? T + "deduce_locked_field" : T + "deduce_locked_analysis");
                return;
            }

            int index = CaseNotebook.CurrentQuestionIndex(Data, Deduction);
            int total = Deduction.Questions.Count;
            bool showingHit = !string.IsNullOrEmpty(_deduceHitId);

            // 방금 맞힌 물음은 해설을 보여 주는 동안 그 자리에 둔다.
            CaseDeductionSO.Question q = null;
            int shown = index;
            if (showingHit)
            {
                for (int i = 0; i < total; i++)
                {
                    if (Deduction.Questions[i] != null && Deduction.Questions[i].questionId == _deduceHitId) { q = Deduction.Questions[i]; shown = i; }
                }
            }
            else if (index < total) q = Deduction.Questions[index];

            if (q == null)
            {
                Locked(mid, T + "deduce_done");
                var toReport = MakeButton("ToReport", mid, Accent, out var trLabel, _semiBold, 26f);
                Place((RectTransform)toReport.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-180f, -120f), new Vector2(180f, -50f));
                trLabel.text = L(T + "to_report");
                trLabel.color = Bg;
                toReport.onClick.AddListener(() => ShowPage(Page.Report));
                return;
            }

            // 머리: 진행도와 얇은 막대
            var head = Label("Head", mid, _semiBold, 22f, TextSub, TextAlignmentOptions.TopLeft);
            Place(head.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -36f), Vector2.zero);
            head.text = L(T + "tab_deduce") + "   <color=#" + ColorUtility.ToHtmlStringRGB(TextMain) + ">" + (shown + 1) + " / " + total + "</color>";

            var track = Box("Track", mid, Line);
            Place(track.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -52f), new Vector2(0f, -46f));
            int done = CaseNotebook.SolvedCount(Data, Deduction);
            var fill = Box("Fill", track.transform, Accent);
            Place(fill.rectTransform, Vector2.zero, new Vector2(Mathf.Clamp01((float)done / Mathf.Max(1, total)), 1f), Vector2.zero, Vector2.zero);

            var label = Label("Label", mid, _semiBold, 22f, Accent, TextAlignmentOptions.TopLeft);
            Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -116f), new Vector2(0f, -80f));
            label.text = L(T + "deduce_step", shown + 1, L(q.labelTextId));

            var question = Label("Question", mid, _bold, 38f, TextMain, TextAlignmentOptions.TopLeft);
            Place(question.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -220f), new Vector2(0f, -122f));
            question.text = L(q.questionTextId);
            question.enableWordWrapping = true;

            // 보기 셋. 넓은 카드로 세운다.
            for (int i = 0; i < q.optionTextIds.Count; i++)
            {
                bool picked = showingHit ? i == q.correctIndex : i == _deduceChoice;
                var option = MakeButton("Option_" + i, mid, picked ? AccentDeep : CardColor, out var optLabel, _medium, 27f);
                float top = -244f - i * 112f;
                Place((RectTransform)option.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, top - 96f), new Vector2(0f, top));
                if (picked) Outline(option.gameObject, Accent);
                optLabel.alignment = TextAlignmentOptions.MidlineLeft;
                optLabel.margin = new Vector4(100f, 0f, 30f, 0f);
                optLabel.text = L(q.optionTextIds[i]);
                optLabel.color = picked ? TextMain : TextSub;
                optLabel.enableWordWrapping = true;

                var badge = Box("Badge", option.transform, picked ? Accent : Line);
                Place(badge.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, -22f), new Vector2(74f, 22f));
                badge.sprite = CircleSprite();
                var letter = Label("Letter", badge.transform, _bold, 22f, picked ? Bg : TextMain, TextAlignmentOptions.Center);
                Stretch(letter.rectTransform);
                letter.text = ((char)('A' + i)).ToString();

                int captured = i;
                option.interactable = !showingHit;
                option.onClick.AddListener(() => { _deduceChoice = captured; _deduceWrong = false; Refresh(); });
            }

            float below = -244f - q.optionTextIds.Count * 112f - 16f;

            if (showingHit)
            {
                var hit = Panel("Hit", mid, PanelColor);
                Place(hit.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, below - 220f), new Vector2(0f, below));
                Outline(hit.gameObject, Accent);
                var col = NewRect("Col", hit.rectTransform);
                Place(col, Vector2.zero, Vector2.one, new Vector2(36f, 20f), new Vector2(-300f, -20f));
                var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
                v.spacing = 8f;
                v.childControlHeight = true;
                v.childControlWidth = true;
                v.childForceExpandHeight = false;
                Para(col, L(T + "deduce_hit"), _bold, 26f, Accent);
                Para(col, L(q.explanationTextId), _regular, 23f, TextMain, 6f);

                bool last = CaseNotebook.AllSolved(Data, Deduction);
                var next = MakeButton("Next", hit.rectTransform, Accent, out var nextLabel, _semiBold, 24f);
                Place((RectTransform)next.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-270f, -34f), new Vector2(-30f, 34f));
                nextLabel.text = L(last ? T + "to_report" : T + "next");
                nextLabel.color = Bg;
                next.onClick.AddListener(() =>
                {
                    _deduceHitId = null;
                    _deduceChoice = -1;
                    if (last) ShowPage(Page.Report);
                    else Refresh();
                });
                StartCoroutine(PopIn(hit.rectTransform, null, 0.35f));
                return;
            }

            var decide = MakeButton("Decide", mid, Accent, out var decideLabel, _semiBold, 26f);
            Place((RectTransform)decide.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, below - 76f), new Vector2(320f, below));
            decideLabel.text = L(T + "decide");
            decideLabel.color = Bg;
            decide.interactable = _deduceChoice >= 0;
            decide.onClick.AddListener(() => OnDecide(q));

            if (_deduceWrong)
            {
                var wrong = Label("Wrong", mid, _medium, 24f, Soft, TextAlignmentOptions.MidlineLeft);
                Place(wrong.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(350f, below - 76f), new Vector2(0f, below));
                wrong.text = L(T + "deduce_wrong");
                StartCoroutine(FadeIn(wrong.rectTransform, 0.25f));
            }
        }

        private void OnDecide(CaseDeductionSO.Question q)
        {
            if (q == null || _deduceChoice < 0) return;

            if (_deduceChoice != q.correctIndex)
            {
                // 틀려도 아무것도 잃지 않는다. 근거를 다시 보고 다시 고르면 된다.
                _deduceWrong = true;
                Debug.Log($"[CaseFile] 최종 추론 {q.questionId} | 보기 {_deduceChoice} - 아니다");
                Refresh();
                return;
            }

            CaseNotebook.MarkSolved(Data, Deduction, q.questionId);
            _save?.MarkDirty();
            _save?.AutoSave();
            _deduceHitId = q.questionId;
            _deduceWrong = false;
            Sfx.Play("clue_get", 0.5f, 1.1f);
            Debug.Log($"[CaseFile] 최종 추론 {q.questionId} | 맞았다 ({CaseNotebook.SolvedCount(Data, Deduction)}/{Deduction.Questions.Count})");

            _ctx.OnSolved?.Invoke(q);
            Refresh();
            if (CaseNotebook.AllSolved(Data, Deduction)) _ctx.OnAllSolved?.Invoke();
        }

        /// <summary>
        /// 근거 확인. 얻은 단서와 찾아낸 분석 기록을 늘어놓고, 누르면 그 아래에 내용을 펼친다.
        /// 어느 것이 이 물음의 답을 받치는지는 표시하지 않는다. 읽고 고르는 것은 플레이어의 몫이다.
        /// </summary>
        private void BuildEvidencePanel()
        {
            var panel = Panel("Evidence", _body);
            Place(panel.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-440f, 0f), Vector2.zero);

            var scroll = Scroll("EvidenceScroll", panel.rectTransform, out var content, 8f, new RectOffset(24, 24, 24, 24));
            Stretch((RectTransform)scroll.transform);

            Para(content, L(T + "evidence"), _bold, 28f, TextMain);
            Para(content, L(T + "evidence_hint"), _regular, 19f, TextSub);
            Spacer(content, 6f);
            Para(content, L(T + "evidence_clues"), _semiBold, 19f, TextDim);

            foreach (var clue in CaseNotebook.AcquiredClues(Data, Deduction))
            {
                string key = "c:" + clue.clueId;
                EvidenceItem(content, key, ShortId(clue.clueId) + "  " + _ctx.ClueTitle(clue.clueId), _ctx.ClueText(clue.clueId));
            }

            Spacer(content, 6f);
            Para(content, L(T + "evidence_analyses"), _semiBold, 19f, TextDim);
            foreach (var a in Deduction.Analyses)
            {
                if (a == null || !CaseNotebook.HasAnalysis(Data, Deduction, a.analysisId)) continue;
                EvidenceItem(content, "a:" + a.analysisId, L(a.titleTextId), L(a.bodyTextId));
            }
        }

        private void EvidenceItem(RectTransform parent, string key, string head, string body)
        {
            bool open = _evidenceKey == key;
            var button = MakeButton("Ev_" + key, parent, open ? CardHover : CardColor, out var label, _medium, 21f);
            Layout(button.gameObject, 60f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.margin = new Vector4(18f, 0f, 12f, 0f);
            label.text = head;
            label.overflowMode = TextOverflowModes.Ellipsis;
            if (open) Outline(button.gameObject, Accent);
            button.onClick.AddListener(() => { _evidenceKey = open ? null : key; Refresh(); });

            if (!open) return;
            var reader = Panel("Reader", parent, Bg);
            var col = reader.gameObject.AddComponent<VerticalLayoutGroup>();
            col.padding = new RectOffset(18, 18, 14, 16);
            col.childControlHeight = true;
            col.childControlWidth = true;
            col.childForceExpandHeight = false;
            Para(reader.rectTransform, body, _regular, 21f, TextMain, 6f);
        }

        // ------------------------------------------------------------- 조사 보고서

        private void BuildReportPage()
        {
            if (!ReportUnlocked)
            {
                var mid = NewRect("Locked", _body);
                Stretch(mid);
                Locked(mid, T + "report_locked");
                return;
            }

            bool confirmed = CaseNotebook.IsReportConfirmed(Data, Deduction.CaseId);

            // 문서 한 장. 가운데에 세우고 아래에 확정 단추.
            var paper = Panel("Paper", _body, PanelColor);
            Place(paper.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-600f, 110f), new Vector2(600f, 0f));
            Outline(paper.gameObject, Line);
            var band = Box("Band", paper.transform, Bureau);
            Place(band.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -6f), Vector2.zero);

            var scroll = Scroll("Doc", paper.rectTransform, out var doc, 12f, new RectOffset(64, 64, 44, 48));
            Stretch((RectTransform)scroll.transform);

            // 머리
            var headRow = NewRect("HeadRow", doc);
            Layout(headRow.gameObject, 40f);
            var org = Label("Org", headRow, _medium, 20f, TextSub, TextAlignmentOptions.MidlineLeft);
            Stretch(org.rectTransform);
            org.text = L(T + "report_doc", Deduction.DocumentNumber);
            var stamp = Label("Stamp", headRow, _bold, 20f, Bureau, TextAlignmentOptions.MidlineRight);
            Stretch(stamp.rectTransform);
            stamp.text = L(T + "report_stamp");
            stamp.characterSpacing = 3f;

            Para(doc, L(T + "report_header"), _bold, 36f, TextMain);
            Divider(doc);

            ReportSection(doc, L(T + "report_case"), L(_ctx.CaseNameTextId));
            foreach (var q in Deduction.Questions)
            {
                if (q == null) continue;
                ReportSection(doc, L(q.labelTextId), L(q.reportTextId));
            }

            var analyses = new StringBuilder();
            foreach (var a in Deduction.Analyses)
            {
                if (a == null || !CaseNotebook.HasAnalysis(Data, Deduction, a.analysisId)) continue;
                if (analyses.Length > 0) analyses.Append('\n');
                analyses.Append("· ").Append(L(a.titleTextId));
            }
            ReportSection(doc, L(T + "report_analyses"), analyses.ToString());

            var clues = new StringBuilder();
            foreach (var clue in CaseNotebook.AcquiredClues(Data, Deduction))
            {
                if (clues.Length > 0) clues.Append('\n');
                clues.Append("· ").Append(ShortId(clue.clueId)).Append("  ").Append(_ctx.ClueTitle(clue.clueId));
            }
            ReportSection(doc, L(T + "report_clues"), clues.ToString(), false);

            var confirm = MakeButton("Confirm", _body, confirmed ? CardColor : Bureau, out var confirmLabel, _semiBold, 28f);
            Place((RectTransform)confirm.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-220f, 10f), new Vector2(220f, 90f));
            confirmLabel.text = L(confirmed ? T + "confirmed" : T + "confirm");
            confirm.interactable = !confirmed;
            confirm.onClick.AddListener(() =>
            {
                if (CaseNotebook.IsReportConfirmed(Data, Deduction.CaseId)) return;
                CaseNotebook.ConfirmReport(Data, Deduction.CaseId);
                _save?.MarkDirty();
                _save?.AutoSave();
                Debug.Log("[CaseFile] 조사 결과 확정 | " + Deduction.CaseId);
                Refresh();
                _ctx.OnConfirm?.Invoke();
            });

            StartCoroutine(FadeIn(paper.rectTransform, 0.25f));
        }

        private void ReportSection(RectTransform doc, string head, string body, bool divider = true)
        {
            Spacer(doc, 4f);
            Para(doc, head, _semiBold, 21f, Accent);
            Para(doc, string.IsNullOrEmpty(body) ? "—" : body, _medium, 27f, TextMain, 6f);
            if (divider) Divider(doc);
        }

        // ------------------------------------------------------------- 공용 조각

        private void PageTitle(RectTransform parent, string title, string sub)
        {
            Para(parent, title, _bold, 34f, TextMain);
            Para(parent, sub, _medium, 21f, TextSub);
            Spacer(parent, 6f);
        }

        private void Locked(RectTransform parent, string textId)
        {
            var box = Panel("LockedBox", parent, PanelColor);
            Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-440f, -10f), new Vector2(440f, 150f));
            Outline(box.gameObject, Line);
            var text = Label("Text", box.rectTransform, _medium, 26f, TextSub, TextAlignmentOptions.Center);
            Place(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(40f, 10f), new Vector2(-40f, -10f));
            text.text = L(textId);
            text.enableWordWrapping = true;
        }

        /// <summary>단서 번호만. clue_subway_001 이면 001.</summary>
        private static string ShortId(string clueId)
        {
            if (string.IsNullOrEmpty(clueId)) return string.Empty;
            int cut = clueId.LastIndexOf('_');
            return cut >= 0 ? clueId.Substring(cut + 1) : clueId;
        }

        private string ClueTag(string clueId) => ShortId(clueId) + " " + _ctx.ClueTitle(clueId);

        // ------------------------------------------------------------- 짓는 손

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
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void Place(RectTransform rt, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
        }

        private static Image Box(string name, Transform parent, Color color)
        {
            var rt = NewRect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image Panel(string name, Transform parent, Color? color = null)
        {
            var image = Box(name, parent, color ?? PanelColor);
            image.raycastTarget = true;
            return image;
        }

        private static void Outline(GameObject go, Color color)
        {
            var o = Ensure<UnityEngine.UI.Outline>(go);
            o.effectColor = color;
            o.effectDistance = new Vector2(2f, -2f);
            o.useGraphicAlpha = false;
        }

        private TMP_Text Label(string name, Transform parent, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font != null ? font : FallbackFont();
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.richText = true;
            return t;
        }

        private TMP_FontAsset FallbackFont()
        {
            if (_regular != null) return _regular;
            if (_medium != null) return _medium;
            return TMP_Settings.defaultFontAsset;
        }

        /// <summary>층에 쌓는 글 한 덩이. 폭을 따라 줄을 바꾸고 높이는 글 길이만큼 잡힌다.</summary>
        private void Para(RectTransform parent, string text, TMP_FontAsset font, float size, Color color, float lineSpacing = 0f)
        {
            var t = Label("Text", parent, font, size, color, TextAlignmentOptions.TopLeft);
            t.text = text;
            t.enableWordWrapping = true;
            t.lineSpacing = lineSpacing;
        }

        private static void Divider(RectTransform parent)
        {
            var line = Box("Divider", parent, Line);
            Layout(line.gameObject, 1f);
        }

        private static void Spacer(RectTransform parent, float height)
        {
            var rt = NewRect("Space", parent);
            Layout(rt.gameObject, height);
        }

        private static void Layout(GameObject go, float height)
        {
            var le = Ensure<LayoutElement>(go);
            le.minHeight = height;
            le.preferredHeight = height;
        }

        private static RectTransform Column(RectTransform parent, float spacing, float padding)
        {
            var rt = NewRect("Column", parent);
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            return rt;
        }

        private Button MakeButton(string name, Transform parent, Color color, out TMP_Text label, TMP_FontAsset font, float size)
        {
            var rt = NewRect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.disabledColor = new Color(0.62f, 0.62f, 0.62f, 1f);
            colors.colorMultiplier = 1.1f;
            colors.fadeDuration = 0.12f;
            button.colors = colors;

            label = Label("Label", rt, font, size, TextMain, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            return button;
        }

        /// <summary>세로로 굴러가는 목록. content 에 쌓으면 높이가 저절로 맞는다.</summary>
        private static ScrollRect Scroll(string name, Transform parent, out RectTransform content, float spacing, RectOffset padding = null)
        {
            var rt = NewRect(name, parent);
            var scroll = rt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            scroll.inertia = true;

            var viewport = NewRect("Viewport", rt);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            scroll.viewport = viewport;

            content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var v = content.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = padding ?? new RectOffset(0, 12, 0, 12);
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            return scroll;
        }

        /// <summary>작은 마름모 표. 단서와 기록 앞에 붙인다.</summary>
        private static void Icon(Transform parent, Vector2 pos, Color color, bool topLeft = false)
        {
            var icon = Box("Icon", parent, color);
            var rt = icon.rectTransform;
            rt.anchorMin = rt.anchorMax = topLeft ? new Vector2(0f, 1f) : new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(14f, 14f);
            rt.anchoredPosition = pos;
            rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var le = icon.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
        }

        /// <summary>왼쪽 줄에서 잠긴 쪽을 가리키는 표. 화면에는 자물쇠 그림으로 그린다.</summary>
        private const string LockMark = "<lock>";

        private static Sprite _ring;

        /// <summary>가는 고리. 자물쇠의 걸쇠로 쓴다.</summary>
        private static Sprite RingSprite()
        {
            if (_ring != null) return _ring;
            const int N = 32;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = x + 0.5f - N / 2f, dy = y + 0.5f - N / 2f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1.5f - Mathf.Abs(r - (N / 2f - 4f)) / 2.5f);
                px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            _ring = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
            return _ring;
        }

        private static Sprite _circle;

        private static Sprite CircleSprite()
        {
            if (_circle != null) return _circle;
            const int N = 64;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = x + 0.5f - N / 2f, dy = y + 0.5f - N / 2f;
                float a = Mathf.Clamp01(N / 2f - Mathf.Sqrt(dx * dx + dy * dy));
                px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            _circle = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
            return _circle;
        }

        private static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        // ------------------------------------------------------------- 움직임

        private static IEnumerator FadeIn(RectTransform rt, float seconds)
        {
            if (rt == null) yield break;
            var group = Ensure<CanvasGroup>(rt.gameObject);
            for (float e = 0f; e < seconds; e += Time.unscaledDeltaTime)
            {
                if (group == null) yield break;
                group.alpha = Mathf.SmoothStep(0f, 1f, e / seconds);
                yield return null;
            }
            if (group != null) group.alpha = 1f;
        }

        private static IEnumerator PopIn(RectTransform rt, CanvasGroup group, float seconds)
        {
            if (rt == null) yield break;
            if (group == null) group = Ensure<CanvasGroup>(rt.gameObject);
            for (float e = 0f; e < seconds; e += Time.unscaledDeltaTime)
            {
                if (rt == null || group == null) yield break;
                float k = 1f - Mathf.Pow(1f - e / seconds, 3f);
                group.alpha = k;
                rt.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, k);
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
            if (group != null) group.alpha = 1f;
        }

        private static TC Ensure<TC>(GameObject go) where TC : Component
        {
            var c = go.GetComponent<TC>();
            if (c == null) c = go.AddComponent<TC>();
            return c;
        }
    }
}
