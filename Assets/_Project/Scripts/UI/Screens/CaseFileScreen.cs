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

        /// <summary>
        /// 왼쪽 줄. 한 칸이 이름과 그 아래 한 줄의 상태로 되어 있다(몇 개 모았는지, 왜 잠겼는지).
        /// 다 끝난 쪽에는 ✓, 아직 열리지 않은 쪽에는 자물쇠를 단다.
        /// </summary>
        private void BuildNav()
        {
            Clear(_nav);
            var col = Column(_nav, 8f, 0f);
            Stretch(col);

            int clues = ClueCount(), clueTotal = Deduction.FieldClues.Count;
            int found = CaseNotebook.AnalysisCount(Data, Deduction), foundTotal = Deduction.Analyses.Count;
            int solved = CaseNotebook.SolvedCount(Data, Deduction), solvedTotal = Deduction.Questions.Count;
            bool analysesDone = CaseNotebook.AllAnalysesFound(Data, Deduction);
            bool confirmed = CaseNotebook.IsReportConfirmed(Data, Deduction.CaseId);

            NavGroup(col, L(T + "group_records"));
            NavItem(col, Page.Clues, L(T + "tab_clues"), L(T + "nav_clues", clues, clueTotal), clues >= clueTotal ? NavMark.Done : NavMark.None);
            NavItem(col, Page.Analyses, L(T + "tab_analyses"), L(T + "nav_analyses", found, foundTotal), analysesDone ? NavMark.Done : NavMark.None);
            NavItem(col, Page.Deductions, L(T + "tab_deductions"), L(T + "nav_deductions", solved, solvedTotal), ReportUnlocked ? NavMark.Done : NavMark.None);

            Spacer(col, 14f);
            NavGroup(col, L(T + "group_work"));
            NavItem(col, Page.Analyze, L(T + "tab_analyze"),
                analysesDone ? L(T + "nav_analyze_done") : L(T + "nav_analyze"), analysesDone ? NavMark.Done : NavMark.None);

            string deduceSub = !analysesDone ? L(T + "nav_deduce_need", foundTotal - found)
                : !DeduceUnlocked ? L(T + "nav_deduce_field")
                : L(T + "nav_deductions", solved, solvedTotal);
            NavItem(col, Page.Deduce, L(T + "tab_deduce"), deduceSub,
                !DeduceUnlocked ? NavMark.Locked : ReportUnlocked ? NavMark.Done : NavMark.None);

            string reportSub = !ReportUnlocked ? L(T + "nav_report_need") : confirmed ? L(T + "confirmed") : L(T + "nav_report_ready");
            NavItem(col, Page.Report, L(T + "tab_report"), reportSub,
                !ReportUnlocked ? NavMark.Locked : confirmed ? NavMark.Done : NavMark.None);
        }

        private enum NavMark { None, Done, Locked }

        private void NavGroup(RectTransform parent, string text)
        {
            var label = Label("Group", parent, _semiBold, 17f, TextDim, TextAlignmentOptions.BottomLeft);
            label.text = text;
            label.characterSpacing = 5f;
            Layout(label.gameObject, 32f);
        }

        private void NavItem(RectTransform parent, Page page, string text, string sub, NavMark mark)
        {
            bool on = _page == page;
            bool locked = mark == NavMark.Locked;
            var button = MakeButton("Nav_" + page, parent, on ? AccentDeep : PanelColor, out var label, on ? _semiBold : _medium, 23f);
            Layout(button.gameObject, 78f);
            Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(24f, -2f), new Vector2(-54f, -8f));
            label.alignment = TextAlignmentOptions.BottomLeft;
            label.text = text;
            label.enableAutoSizing = true;
            label.fontSizeMin = 17f;
            label.fontSizeMax = 23f;
            label.color = on ? TextMain : locked ? TextSub : TextMain;

            var subLabel = Label("Sub", button.transform, _regular, 17f, on ? Hex("B9CAD6") : locked ? TextDim : TextSub, TextAlignmentOptions.TopLeft);
            Place(subLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(24f, 6f), new Vector2(-54f, -4f));
            subLabel.text = sub;
            subLabel.overflowMode = TextOverflowModes.Ellipsis;

            if (on)
            {
                var bar = Box("Bar", button.transform, Accent);
                Place(bar.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(4f, 0f));
            }

            if (mark == NavMark.Locked) LockIcon(button.transform, new Vector2(-31f, 0f), TextDim);
            else if (mark == NavMark.Done)
            {
                var check = Label("Done", button.transform, _bold, 22f, Accent, TextAlignmentOptions.Center);
                Place(check.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-48f, -16f), new Vector2(-16f, 16f));
                check.text = "✓";
            }

            button.onClick.AddListener(() => ShowPage(page));
        }

        /// <summary>작은 자물쇠. 고리 하나와 몸통 하나.</summary>
        private static void LockIcon(Transform parent, Vector2 center, Color color)
        {
            var shackle = Box("Shackle", parent, color);
            shackle.sprite = RingSprite();
            Place(shackle.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), center + new Vector2(-6f, -1f), center + new Vector2(6f, 11f));
            var body = Box("Lock", parent, color);
            Place(body.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), center + new Vector2(-8f, -10f), center + new Vector2(8f, 2f));
        }

        // ------------------------------------------------------------- 수집한 단서

        private void BuildCluesPage()
        {
            var list = Scroll("ClueList", _body, out var content, 10f);
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

        /// <summary>
        /// 단서 한 장. 왼쪽에 번호 배지, 가운데에 이름과 얻은 자리, 오른쪽에 상태.
        /// 아직 못 얻은 단서는 번호도 이름도 감춘다.
        /// </summary>
        private void ClueCard(RectTransform parent, CaseDeductionSO.FieldClue clue, bool has, bool selected, Action onClick)
        {
            var button = MakeButton("Clue_" + clue.clueId, parent, selected ? CardHover : has ? CardColor : PanelColor, out var label, _semiBold, 26f);
            Layout(button.gameObject, 104f);
            label.gameObject.SetActive(false);
            if (selected) Outline(button.gameObject, Accent);

            var badge = Box("Badge", button.transform, has ? AccentDeep : CardColor);
            Place(badge.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, -32f), new Vector2(92f, 32f));
            var number = Label("Number", badge.transform, _bold, 22f, has ? Hex("CFE3EE") : TextDim, TextAlignmentOptions.Center);
            Stretch(number.rectTransform);
            number.text = has ? ShortId(clue.clueId) : "???";

            var title = Label("Title", button.transform, _semiBold, 26f, has ? TextMain : TextDim, TextAlignmentOptions.BottomLeft);
            Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(112f, 0f), new Vector2(-130f, -14f));
            title.text = has ? _ctx.ClueTitle(clue.clueId) : L(T + "undiscovered_clue");
            title.overflowMode = TextOverflowModes.Ellipsis;

            var place = Label("Place", button.transform, _regular, 19f, has ? TextSub : TextDim, TextAlignmentOptions.TopLeft);
            Place(place.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(112f, 14f), new Vector2(-130f, -6f));
            place.text = has ? L(T + "place", L(clue.placeTextId)) : L(T + "place_unknown");
            place.overflowMode = TextOverflowModes.Ellipsis;

            Chip(button.transform, has ? L(T + "state_found") : L(T + "undiscovered"), has ? Accent : TextDim, new Vector2(-20f, 0f));

            button.interactable = has;
            if (has) button.onClick.AddListener(() => onClick());
        }

        /// <summary>오른쪽 가운데에 붙이는 작은 상태 표. 테두리만 있는 꼬리표다.</summary>
        private void Chip(Transform parent, string text, Color color, Vector2 rightCenter)
        {
            // 판을 먼저 깔고 글을 그 위에 얹는다. 글의 자식으로 두면 판이 글을 덮는다.
            var edge = Box("ChipEdge", parent, new Color(color.r, color.g, color.b, 0.12f));
            Border(edge.transform, new Color(color.r, color.g, color.b, 0.6f));
            var label = Label("Chip", parent, _medium, 17f, color, TextAlignmentOptions.Center);
            label.text = text;
            float w = Mathf.Max(80f, label.GetPreferredValues(text, 400f, 30f).x + 28f);
            var min = rightCenter + new Vector2(-w, -16f);
            var max = rightCenter + new Vector2(0f, 16f);
            Place(label.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), min, max);
            Place(edge.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), min, max);
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
            var scroll = Scroll("DetailScroll", panel, out var content, 14f, new RectOffset(44, 52, 40, 40));
            Stretch((RectTransform)scroll.transform);

            Para(content, L(T + "detail_number", ShortId(_detailClueId)), _semiBold, 20f, Accent).characterSpacing = 2f;
            Para(content, _ctx.ClueTitle(_detailClueId), _bold, 38f, TextMain);

            // 기록 카드처럼 항목과 값을 두 줄로 나눠 적는다.
            MetaRow(content, L(T + "meta_place"), clue != null ? L(clue.placeTextId) : "—");
            MetaRow(content, L(T + "meta_case"), L(_ctx.CaseNameTextId));
            Divider(content);
            Para(content, _ctx.ClueText(_detailClueId), _regular, 28f, TextMain, 12f);
            Spacer(content, 10f);
            Divider(content);
            Para(content, L(T + "detail_related"), _semiBold, 20f, TextSub).characterSpacing = 2f;

            int related = 0;
            for (int i = 0; i < Deduction.Analyses.Count; i++)
            {
                var a = Deduction.Analyses[i];
                if (a == null || (a.clueA != _detailClueId && a.clueB != _detailClueId)) continue;
                if (!CaseNotebook.HasAnalysis(Data, Deduction, a.analysisId)) continue;
                Para(content, "<color=#" + ColorUtility.ToHtmlStringRGB(Accent) + ">" + AnalysisNo(i) + "</color>   " + L(a.titleTextId), _medium, 24f, TextMain);
                related++;
            }
            if (related == 0) Para(content, L(T + "detail_no_related"), _regular, 21f, TextDim);
        }

        private void MetaRow(RectTransform parent, string key, string value)
        {
            Para(parent, "<color=#" + ColorUtility.ToHtmlStringRGB(TextDim) + ">" + key + "</color>   " + value, _regular, 21f, TextSub);
        }

        private string AnalysisNo(int index) => L(T + "analysis_no", (index + 1).ToString("00"));

        // ------------------------------------------------------------- 분석 기록 / 추론 기록

        private void BuildAnalysesPage()
        {
            var scroll = Scroll("Analyses", _body, out var content, 16f);
            Place((RectTransform)scroll.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-200f, 0f));

            PageTitle(content, L(T + "tab_analyses"), L(T + "stat_analyses", CaseNotebook.AnalysisCount(Data, Deduction), Deduction.Analyses.Count));
            for (int i = 0; i < Deduction.Analyses.Count; i++)
            {
                var a = Deduction.Analyses[i];
                if (a == null) continue;
                AnalysisCard(content, a, i, CaseNotebook.HasAnalysis(Data, Deduction, a.analysisId));
            }
        }

        /// <summary>분석 기록 한 장. 왼쪽 위에 "분석 01", 제목, 내용, 아래에 쓴 단서 꼬리표 둘.</summary>
        private void AnalysisCard(RectTransform parent, CaseDeductionSO.Analysis a, int index, bool found)
        {
            var card = Panel("Analysis_" + a.analysisId, parent, found ? CardColor : PanelColor);
            var col = card.gameObject.AddComponent<VerticalLayoutGroup>();
            col.padding = new RectOffset(40, 40, 28, 30);
            col.spacing = 10f;
            col.childControlHeight = true;
            col.childControlWidth = true;
            col.childForceExpandHeight = false;
            if (found) Edge(card.transform, Accent);

            Para(card.rectTransform, AnalysisNo(index), _semiBold, 18f, found ? Accent : TextDim).characterSpacing = 3f;

            if (!found)
            {
                Para(card.rectTransform, L(T + "analysis_locked"), _semiBold, 26f, TextDim);
                Para(card.rectTransform, L(T + "analysis_locked_hint"), _regular, 20f, TextDim);
                return;
            }

            Para(card.rectTransform, L(a.titleTextId), _bold, 30f, TextMain);
            Para(card.rectTransform, L(a.bodyTextId), _regular, 25f, TextMain, 8f);
            TagRow(card.rectTransform, a.clueA, a.clueB);
        }

        /// <summary>쓴 단서 둘을 꼬리표로 나란히 놓는다. "001 같은 칸의 엇갈린 진술" ↔ "005 역무실 신고 기록".</summary>
        private void TagRow(RectTransform parent, string a, string b)
        {
            var row = NewRect("Tags", parent);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.MiddleLeft;
            Layout(row.gameObject, 40f);

            Tag(row, a);
            var link = Label("Link", row, _bold, 20f, TextDim, TextAlignmentOptions.Center);
            link.text = "↔";
            Tag(row, b);
        }

        private void Tag(RectTransform row, string clueId)
        {
            var tag = Box("Tag", row, Hex("1E2A33"));
            Outline(tag.gameObject, Hex("2F4A58"));
            var h = tag.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(14, 14, 6, 6);
            h.childControlWidth = true;
            h.childControlHeight = true;
            var t = Label("Text", tag.transform, _medium, 18f, Hex("B9CAD6"), TextAlignmentOptions.Center);
            t.text = "<color=#" + ColorUtility.ToHtmlStringRGB(Accent) + ">" + ShortId(clueId) + "</color>  " + _ctx.ClueTitle(clueId);
        }

        private void BuildDeductionsPage()
        {
            var scroll = Scroll("Deductions", _body, out var content, 16f);
            Place((RectTransform)scroll.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-200f, 0f));

            PageTitle(content, L(T + "tab_deductions"), L(T + "stat_deductions", CaseNotebook.SolvedCount(Data, Deduction), Deduction.Questions.Count));
            for (int i = 0; i < Deduction.Questions.Count; i++)
            {
                var q = Deduction.Questions[i];
                if (q == null) continue;
                bool solved = CaseNotebook.IsSolved(Data, Deduction, q.questionId);

                var card = Panel("Deduction_" + i, content, solved ? CardColor : PanelColor);
                var col = card.gameObject.AddComponent<VerticalLayoutGroup>();
                col.padding = new RectOffset(120, 40, 26, 28);
                col.spacing = 6f;
                col.childControlHeight = true;
                col.childControlWidth = true;
                col.childForceExpandHeight = false;

                // 왼쪽 둥근 번호. 확정한 결론은 청록, 아직이면 흐리게.
                var badge = Box("No", card.transform, solved ? Accent : CardColor);
                badge.sprite = CircleSprite();
                badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                badge.rectTransform.sizeDelta = new Vector2(52f, 52f);
                badge.rectTransform.anchoredPosition = new Vector2(64f, 0f);
                badge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var no = Label("Text", badge.transform, _bold, 22f, solved ? Bg : TextDim, TextAlignmentOptions.Center);
                Stretch(no.rectTransform);
                no.text = solved ? "✓" : (i + 1).ToString();

                Para(card.rectTransform, L(T + "deduce_step", i + 1, L(q.labelTextId)), _semiBold, 20f, solved ? Accent : TextDim).characterSpacing = 1f;
                Para(card.rectTransform, solved ? L(q.reportTextId) : L(T + "deduction_pending"), solved ? _bold : _medium, 28f, solved ? TextMain : TextDim);
            }
        }

        // ------------------------------------------------------------- 단서 분석

        private void BuildAnalyzePage()
        {
            bool allFound = CaseNotebook.AllAnalysesFound(Data, Deduction);

            // 왼쪽: 고를 수 있는 단서
            var left = Panel("Pick", _body);
            Place(left.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(400f, 0f));
            var leftHead = Label("Head", left.rectTransform, _semiBold, 20f, TextSub, TextAlignmentOptions.MidlineLeft);
            Place(leftHead.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(26f, -62f), new Vector2(-20f, -10f));
            leftHead.text = L(T + "tab_clues") + "   <color=#" + ColorUtility.ToHtmlStringRGB(TextDim) + ">" + ClueCount() + "/" + Deduction.FieldClues.Count + "</color>";
            leftHead.characterSpacing = 1f;

            var list = Scroll("List", left.rectTransform, out var content, 10f, new RectOffset(16, 22, 4, 18));
            Place((RectTransform)list.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -66f));

            foreach (var clue in CaseNotebook.AcquiredClues(Data, Deduction))
            {
                int slot = _picked.IndexOf(clue.clueId);
                var button = MakeButton("Pick_" + clue.clueId, content, slot >= 0 ? CardHover : CardColor, out var label, _semiBold, 22f);
                Layout(button.gameObject, 88f);
                if (slot >= 0) Outline(button.gameObject, Accent);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.margin = new Vector4(22f, 0f, 64f, 0f);
                label.text = "<size=80%><color=#" + ColorUtility.ToHtmlStringRGB(Accent) + ">" + ShortId(clue.clueId) + "</color></size>   " + _ctx.ClueTitle(clue.clueId);
                label.enableWordWrapping = true;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.lineSpacing = -6f;

                // 고른 단서에는 몇 번째 칸에 들어갔는지 둥근 표를 단다.
                var check = Box("Check", button.transform, slot >= 0 ? Accent : Line);
                Place(check.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-54f, -16f), new Vector2(-22f, 16f));
                check.sprite = CircleSprite();
                var mark = Label("Mark", check.transform, _bold, 18f, slot >= 0 ? Bg : TextDim, TextAlignmentOptions.Center);
                Stretch(mark.rectTransform);
                mark.text = slot >= 0 ? (slot + 1).ToString() : string.Empty;

                string id = clue.clueId;
                button.interactable = !allFound;
                button.onClick.AddListener(() => TogglePick(id));
            }

            // 오른쪽: 찾아낸 분석 기록
            var right = Panel("Found", _body);
            Place(right.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-340f, 0f), Vector2.zero);
            var rightScroll = Scroll("FoundList", right.rectTransform, out var found, 12f, new RectOffset(24, 30, 24, 24));
            Stretch((RectTransform)rightScroll.transform);
            Para(found, L(T + "found_records"), _semiBold, 20f, TextSub).characterSpacing = 1f;
            Para(found, L(T + "stat_analyses", CaseNotebook.AnalysisCount(Data, Deduction), Deduction.Analyses.Count), _bold, 30f, TextMain);
            Divider(found);
            for (int i = 0; i < Deduction.Analyses.Count; i++)
            {
                var a = Deduction.Analyses[i];
                if (a == null) continue;
                bool has = CaseNotebook.HasAnalysis(Data, Deduction, a.analysisId);
                var card = Panel("Rec_" + a.analysisId, found, has ? CardColor : Bg);
                var col = card.gameObject.AddComponent<VerticalLayoutGroup>();
                col.padding = new RectOffset(22, 22, 14, 16);
                col.spacing = 4f;
                col.childControlHeight = true;
                col.childControlWidth = true;
                col.childForceExpandHeight = false;
                if (has) Edge(card.transform, Accent);
                Para(card.rectTransform, AnalysisNo(i), _semiBold, 16f, has ? Accent : TextDim).characterSpacing = 2f;
                Para(card.rectTransform, has ? L(a.titleTextId) : L(T + "analysis_locked"), _semiBold, 21f, has ? TextMain : TextDim);
                if (has) Para(card.rectTransform, ShortId(a.clueA) + " ↔ " + ShortId(a.clueB), _medium, 18f, Accent);
            }

            // 가운데
            var mid = NewRect("Compare", _body);
            Place(mid, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(428f, 0f), new Vector2(-368f, 0f));

            var title = Label("Title", mid, _bold, 34f, TextMain, TextAlignmentOptions.TopLeft);
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -54f), Vector2.zero);
            title.text = L(T + "tab_analyze");

            if (allFound)
            {
                BuildAnalyzeDone(mid);
                return;
            }

            var hint = Label("Hint", mid, _regular, 21f, TextSub, TextAlignmentOptions.TopLeft);
            Place(hint.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -122f), new Vector2(0f, -60f));
            hint.text = L(T + "analyze_hint");
            hint.enableWordWrapping = true;

            Slot(mid, 0, -140f);
            Slot(mid, 1, -350f);

            // 두 칸 사이를 잇는 가는 선과 맞대어 본다는 표(↔)
            var line = Box("LinkLine", mid, Line);
            Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-1f, -350f), new Vector2(1f, -290f));
            var link = Box("Link", mid, PanelColor);
            Place(link.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-24f, -344f), new Vector2(24f, -296f));
            link.sprite = CircleSprite();
            Outline(link.gameObject, Line);
            var arrow = Label("Arrow", link.transform, _bold, 24f, _picked.Count == 2 ? Accent : TextSub, TextAlignmentOptions.Center);
            Stretch(arrow.rectTransform);
            arrow.text = "↔";
            link.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            bool ready = _picked.Count == 2;
            var compare = MakeButton("CompareButton", mid, Accent, out var compareLabel, _semiBold, 27f);
            Place((RectTransform)compare.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-170f, -600f), new Vector2(170f, -528f));
            compareLabel.text = L(T + "compare");
            SetEnabledLook(compare, compareLabel, ready);
            compare.onClick.AddListener(OnCompare);

            if (!string.IsNullOrEmpty(_compareMessageId))
            {
                var box = Notice(mid, -620f, 78f, Soft, L(_compareMessageId), L(T + "compare_retry"));
                StartCoroutine(FadeIn(box, 0.25f));
            }
        }

        /// <summary>분석을 다 마친 뒤 가운데 칸. 빈 비교 칸 대신 다 됐다는 판을 세운다.</summary>
        private void BuildAnalyzeDone(RectTransform mid)
        {
            var box = Panel("Done", mid, PanelColor);
            Place(box.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -420f), new Vector2(0f, -90f));
            Outline(box.gameObject, Line);

            var badge = Box("Check", box.transform, Accent);
            badge.sprite = CircleSprite();
            Place(badge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-34f, -104f), new Vector2(34f, -36f));
            var check = Label("Mark", badge.transform, _bold, 34f, Bg, TextAlignmentOptions.Center);
            Stretch(check.rectTransform);
            check.text = "✓";

            var head = Label("Head", box.rectTransform, _bold, 30f, TextMain, TextAlignmentOptions.Top);
            Place(head.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -166f), new Vector2(-30f, -120f));
            head.text = L(T + "analyze_done");

            var text = Label("Text", box.rectTransform, _regular, 22f, TextSub, TextAlignmentOptions.Top);
            Place(text.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -240f), new Vector2(-40f, -176f));
            text.enableWordWrapping = true;
            text.text = DeduceUnlocked ? L(T + "analyze_done_go") : L(T + "analyze_done_wait");

            if (!DeduceUnlocked) return;
            var go = MakeButton("ToDeduce", box.rectTransform, Accent, out var goLabel, _semiBold, 25f);
            Place((RectTransform)go.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-160f, 28f), new Vector2(160f, 94f));
            goLabel.text = L(T + "to_deduce") + "  →";
            SetEnabledLook(go, goLabel, true);
            go.onClick.AddListener(() => ShowPage(Page.Deduce));
        }

        /// <summary>
        /// 맞대어 볼 칸 하나. 가로로 넓게 두고 이름과 내용 두 줄만 보인다(전문은 수집한 단서에서 읽는다).
        /// 오른쪽 위 × 로 비운다.
        /// </summary>
        private void Slot(RectTransform parent, int index, float top)
        {
            bool filled = index < _picked.Count;
            var slot = Panel("Slot_" + index, parent, filled ? CardColor : PanelColor);
            Place(slot.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, top - 150f), new Vector2(0f, top));
            Outline(slot.gameObject, filled ? Accent : Line);

            var head = Label("Head", slot.rectTransform, _semiBold, 18f, filled ? Accent : TextDim, TextAlignmentOptions.TopLeft);
            Place(head.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(26f, -44f), new Vector2(-80f, -16f));
            head.text = L(T + "slot", index + 1) + (filled ? "  ·  " + ShortId(_picked[index]) : string.Empty);
            head.characterSpacing = 2f;

            if (!filled)
            {
                var empty = Label("Empty", slot.rectTransform, _regular, 21f, TextDim, TextAlignmentOptions.Center);
                Place(empty.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, -20f));
                empty.text = L(T + "analyze_pick");
                return;
            }

            string id = _picked[index];
            var name = Label("Title", slot.rectTransform, _bold, 26f, TextMain, TextAlignmentOptions.TopLeft);
            Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(26f, -84f), new Vector2(-80f, -46f));
            name.text = _ctx.ClueTitle(id);
            name.overflowMode = TextOverflowModes.Ellipsis;

            var body = Label("Body", slot.rectTransform, _regular, 19f, TextSub, TextAlignmentOptions.TopLeft);
            Place(body.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(26f, 12f), new Vector2(-30f, -88f));
            body.text = _ctx.ClueText(id);
            body.enableWordWrapping = true;
            body.overflowMode = TextOverflowModes.Ellipsis;

            var clear = MakeButton("Clear", slot.rectTransform, PanelColor, out var clearLabel, _medium, 24f);
            Place((RectTransform)clear.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-62f, -58f), new Vector2(-14f, -12f));
            clearLabel.text = "×";
            Outline(clear.gameObject, Line);
            clear.onClick.AddListener(() => { _picked.Remove(id); _compareMessageId = null; Refresh(); });
        }

        /// <summary>
        /// 결과를 알리는 작은 판. 왼쪽에 색 띠, 굵은 한 줄과 흐린 안내 한 줄.
        /// 과하게 붉히지 않는다. 틀렸다기보다 다시 보자는 말이다.
        /// </summary>
        private RectTransform Notice(RectTransform parent, float top, float height, Color color, string head, string sub)
        {
            var box = Panel("Notice", parent, Hex("1E1C18"));
            Place(box.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, top - height), new Vector2(0f, top));
            var bar = Box("Bar", box.transform, color);
            Place(bar.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(4f, 0f));

            var h = Label("Head", box.rectTransform, _semiBold, 22f, color, TextAlignmentOptions.BottomLeft);
            Place(h.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(26f, 0f), new Vector2(-20f, -8f));
            h.text = head;
            h.overflowMode = TextOverflowModes.Ellipsis;

            var s = Label("Sub", box.rectTransform, _regular, 18f, TextSub, TextAlignmentOptions.TopLeft);
            Place(s.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(26f, 6f), new Vector2(-20f, -4f));
            s.text = sub;
            s.overflowMode = TextOverflowModes.Ellipsis;
            return box.rectTransform;
        }

        /// <summary>
        /// 단서를 고르거나 놓는다. 이미 고른 것을 다시 누르면 놓는다.
        /// 두 칸이 다 찼을 때 새 단서를 누르면 두 번째 칸을 그것으로 갈아 끼운다. 셋 이상은 고를 수 없다.
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
                // 이미 찾은 조합이다. 발견 연출은 다시 띄우지 않는다.
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

        /// <summary>분석 기록을 찾았을 때 가운데에 잠깐 떠오르는 판. 내용만큼만 높다. 확인을 누르면 걷힌다.</summary>
        private void ShowDiscovery(CaseDeductionSO.Analysis a)
        {
            HideOverlay();

            var dim = NewRect("Discovery", (RectTransform)transform);
            Stretch(dim);
            var dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.62f);
            _overlay = dim.gameObject.AddComponent<CanvasGroup>();

            var panel = Panel("Panel", dim, PanelColor);
            var rt = panel.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(820f, 0f);
            Outline(panel.gameObject, Accent);
            var v = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(52, 52, 44, 36);
            v.spacing = 14f;
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            var fit = panel.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var top = Box("Top", panel.transform, Accent);
            Place(top.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -5f), Vector2.zero);
            top.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            int index = 0;
            for (int i = 0; i < Deduction.Analyses.Count; i++) if (Deduction.Analyses[i] == a) index = i;
            Para(rt, "◆  " + L(T + "found_title") + "   <color=#" + ColorUtility.ToHtmlStringRGB(TextDim) + ">" + AnalysisNo(index) + "</color>", _semiBold, 20f, Accent).characterSpacing = 1f;
            Para(rt, L(a.titleTextId), _bold, 36f, TextMain);
            Para(rt, L(a.bodyTextId), _regular, 25f, TextMain, 8f);
            TagRow(rt, a.clueA, a.clueB);
            Spacer(rt, 6f);

            var row = NewRect("ButtonRow", rt);
            Layout(row.gameObject, 64f);
            var ok = MakeButton("Ok", row, Accent, out var okLabel, _semiBold, 25f);
            Place((RectTransform)ok.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-120f, 0f), new Vector2(120f, 0f));
            okLabel.text = L("ui.common.ok");
            SetEnabledLook(ok, okLabel, true);
            ok.onClick.AddListener(HideOverlay);

            StartCoroutine(PopIn(rt, _overlay, 0.4f));
        }

        private void HideOverlay()
        {
            if (_overlay != null) Destroy(_overlay.gameObject);
            _overlay = null;
        }

        // ------------------------------------------------------------- 최종 추론

        /// <summary>지금 물음에서 고르고 틀렸던 보기들. 물음이 바뀌면 비운다. 같은 오답을 거듭 내지 않게 한다.</summary>
        private readonly HashSet<int> _wrongPicks = new HashSet<int>();
        private string _wrongFor;

        private void BuildDeducePage()
        {
            BuildEvidencePanel();

            var mid = NewRect("Deduce", _body);
            Place(mid, Vector2.zero, Vector2.one, new Vector2(24f, 0f), new Vector2(-500f, 0f));

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
                trLabel.text = L(T + "to_report") + "  →";
                SetEnabledLook(toReport, trLabel, true);
                toReport.onClick.AddListener(() => ShowPage(Page.Report));
                return;
            }

            if (_wrongFor != q.questionId) { _wrongPicks.Clear(); _wrongFor = q.questionId; }

            // 머리: 진행도와 얇은 막대
            var head = Label("Head", mid, _semiBold, 20f, TextSub, TextAlignmentOptions.TopLeft);
            Place(head.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -32f), Vector2.zero);
            head.text = L(T + "tab_deduce") + "   <color=#" + ColorUtility.ToHtmlStringRGB(TextMain) + ">" + (shown + 1) + " / " + total + "</color>";
            head.characterSpacing = 1f;

            var track = Box("Track", mid, Line);
            Place(track.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -46f), new Vector2(0f, -41f));
            int done = CaseNotebook.SolvedCount(Data, Deduction);
            var fill = Box("Fill", track.transform, Accent);
            Place(fill.rectTransform, Vector2.zero, new Vector2(Mathf.Clamp01((float)done / Mathf.Max(1, total)), 1f), Vector2.zero, Vector2.zero);

            var label = Label("Label", mid, _semiBold, 21f, Accent, TextAlignmentOptions.TopLeft);
            Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -100f), new Vector2(0f, -70f));
            label.text = L(T + "deduce_step", shown + 1, L(q.labelTextId));

            var question = Label("Question", mid, _bold, 36f, TextMain, TextAlignmentOptions.TopLeft);
            Place(question.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -190f), new Vector2(0f, -104f));
            question.text = L(q.questionTextId);
            question.enableWordWrapping = true;

            const float OptionTop = -200f;
            const float OptionHeight = 90f;
            const float OptionStep = 102f;

            if (showingHit)
            {
                // 맞힌 뒤에는 보기를 접고 고른 답 하나만 남긴다. 해설이 눈에 들어오게 한다.
                Option(mid, q, q.correctIndex, OptionTop, OptionHeight, OptionState.Correct);
                float hitTop = OptionTop - OptionHeight - 18f;

                var hit = Panel("Hit", mid, PanelColor);
                Place(hit.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, hitTop - 230f), new Vector2(0f, hitTop));
                Outline(hit.gameObject, Accent);
                Edge(hit.transform, Accent);
                var col = NewRect("Col", hit.rectTransform);
                Place(col, Vector2.zero, Vector2.one, new Vector2(40f, 96f), new Vector2(-36f, -24f));
                var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
                v.spacing = 10f;
                v.childControlHeight = true;
                v.childControlWidth = true;
                v.childForceExpandHeight = false;
                Para(col, "✓  " + L(T + "deduce_hit"), _bold, 26f, Accent);
                Para(col, L(q.explanationTextId), _regular, 23f, TextMain, 6f);

                bool last = CaseNotebook.AllSolved(Data, Deduction);
                var next = MakeButton("Next", hit.rectTransform, Accent, out var nextLabel, _semiBold, 24f);
                Place((RectTransform)next.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-300f, 22f), new Vector2(-30f, 86f));
                nextLabel.text = L(last ? T + "to_report" : T + "next") + "  →";
                SetEnabledLook(next, nextLabel, true);
                next.onClick.AddListener(() =>
                {
                    _deduceHitId = null;
                    _deduceChoice = -1;
                    _deduceWrong = false;
                    if (last) ShowPage(Page.Report);
                    else Refresh();
                });
                StartCoroutine(PopIn(hit.rectTransform, null, 0.35f));
                return;
            }

            for (int i = 0; i < q.optionTextIds.Count; i++)
            {
                var state = i == _deduceChoice ? OptionState.Picked : _wrongPicks.Contains(i) ? OptionState.Tried : OptionState.Idle;
                Option(mid, q, i, OptionTop - i * OptionStep, OptionHeight, state);
            }

            float below = OptionTop - q.optionTextIds.Count * OptionStep - 10f;
            bool canDecide = _deduceChoice >= 0 && !_wrongPicks.Contains(_deduceChoice);
            var decide = MakeButton("Decide", mid, Accent, out var decideLabel, _semiBold, 25f);
            Place((RectTransform)decide.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, below - 72f), new Vector2(320f, below));
            decideLabel.text = L(T + "decide");
            SetEnabledLook(decide, decideLabel, canDecide);
            decide.onClick.AddListener(() => OnDecide(q));

            if (_deduceWrong)
            {
                var box = Notice(mid, below - 92f, 84f, Soft, L(T + "deduce_wrong"), L(T + "deduce_wrong_hint"));
                StartCoroutine(FadeIn(box, 0.25f));
            }
        }

        private enum OptionState { Idle, Picked, Tried, Correct }

        /// <summary>보기 한 장. 왼쪽 A·B·C 배지와 넓은 글. 골랐으면 청록 테두리, 틀렸던 것은 흐리게 × 를 단다.</summary>
        private void Option(RectTransform parent, CaseDeductionSO.Question q, int i, float top, float height, OptionState state)
        {
            bool lit = state == OptionState.Picked || state == OptionState.Correct;
            var option = MakeButton("Option_" + i, parent, lit ? AccentDeep : state == OptionState.Tried ? PanelColor : CardColor, out var optLabel, _medium, 26f);
            Place((RectTransform)option.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, top - height), new Vector2(0f, top));
            if (lit) Outline(option.gameObject, Accent);
            optLabel.alignment = TextAlignmentOptions.MidlineLeft;
            optLabel.margin = new Vector4(96f, 0f, 70f, 0f);
            optLabel.text = L(q.optionTextIds[i]);
            optLabel.color = lit ? TextMain : state == OptionState.Tried ? TextDim : TextSub;
            optLabel.enableWordWrapping = true;

            var badge = Box("Badge", option.transform, lit ? Accent : Line);
            Place(badge.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, -22f), new Vector2(72f, 22f));
            badge.sprite = CircleSprite();
            var letter = Label("Letter", badge.transform, _bold, 21f, lit ? Bg : TextMain, TextAlignmentOptions.Center);
            Stretch(letter.rectTransform);
            letter.text = state == OptionState.Correct ? "✓" : ((char)('A' + i)).ToString();

            if (state == OptionState.Tried)
            {
                var x = Label("Tried", option.transform, _bold, 24f, TextDim, TextAlignmentOptions.Center);
                Place(x.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-56f, -18f), new Vector2(-20f, 18f));
                x.text = "×";
            }

            int captured = i;
            option.interactable = state != OptionState.Correct;
            option.onClick.AddListener(() => { _deduceChoice = captured; _deduceWrong = false; Refresh(); });
        }

        private void OnDecide(CaseDeductionSO.Question q)
        {
            if (q == null || _deduceChoice < 0 || _wrongPicks.Contains(_deduceChoice)) return;

            if (_deduceChoice != q.correctIndex)
            {
                // 틀려도 아무것도 잃지 않는다. 근거를 다시 보고 다른 답을 고르면 된다.
                _wrongPicks.Add(_deduceChoice);
                _wrongFor = q.questionId;
                _deduceWrong = true;
                Debug.Log($"[CaseFile] 최종 추론 {q.questionId} | 보기 {_deduceChoice} - 아니다");
                _deduceChoice = -1;
                Refresh();
                return;
            }

            CaseNotebook.MarkSolved(Data, Deduction, q.questionId);
            _save?.MarkDirty();
            _save?.AutoSave();
            _deduceHitId = q.questionId;
            _deduceWrong = false;
            _wrongPicks.Clear();
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
            Place(panel.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-470f, 0f), Vector2.zero);

            var scroll = Scroll("EvidenceScroll", panel.rectTransform, out var content, 8f, new RectOffset(24, 30, 24, 24));
            Stretch((RectTransform)scroll.transform);

            Para(content, L(T + "evidence"), _bold, 28f, TextMain);
            Para(content, L(T + "evidence_hint"), _regular, 19f, TextSub);
            Spacer(content, 6f);
            Para(content, L(T + "evidence_clues"), _semiBold, 17f, TextDim).characterSpacing = 4f;

            foreach (var clue in CaseNotebook.AcquiredClues(Data, Deduction))
            {
                EvidenceItem(content, "c:" + clue.clueId, "<color=#" + ColorUtility.ToHtmlStringRGB(Accent) + ">" + ShortId(clue.clueId) + "</color>  " + _ctx.ClueTitle(clue.clueId), _ctx.ClueText(clue.clueId));
            }

            Spacer(content, 6f);
            Para(content, L(T + "evidence_analyses"), _semiBold, 17f, TextDim).characterSpacing = 4f;
            for (int i = 0; i < Deduction.Analyses.Count; i++)
            {
                var a = Deduction.Analyses[i];
                if (a == null || !CaseNotebook.HasAnalysis(Data, Deduction, a.analysisId)) continue;
                EvidenceItem(content, "a:" + a.analysisId, "<color=#" + ColorUtility.ToHtmlStringRGB(Accent) + ">" + AnalysisNo(i) + "</color>  " + L(a.titleTextId), L(a.bodyTextId));
            }
        }

        /// <summary>근거 한 줄. 닫혀 있으면 ▶, 펼치면 ▼ 와 함께 아래에 어두운 판으로 내용이 열린다.</summary>
        private void EvidenceItem(RectTransform parent, string key, string head, string body)
        {
            bool open = _evidenceKey == key;
            var button = MakeButton("Ev_" + key, parent, open ? CardHover : CardColor, out var label, _medium, 20f);
            Layout(button.gameObject, 58f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.margin = new Vector4(46f, 0f, 12f, 0f);
            label.text = head;
            label.overflowMode = TextOverflowModes.Ellipsis;
            if (open) Outline(button.gameObject, Accent);

            var arrow = Label("Arrow", button.transform, _bold, 13f, open ? Accent : TextDim, TextAlignmentOptions.Center);
            Place(arrow.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, -12f), new Vector2(38f, 12f));
            arrow.text = open ? "▼" : "▶";
            button.onClick.AddListener(() => { _evidenceKey = open ? null : key; Refresh(); });

            if (!open) return;
            var reader = Panel("Reader", parent, Hex("141A21"));
            var col = reader.gameObject.AddComponent<VerticalLayoutGroup>();
            col.padding = new RectOffset(22, 18, 16, 18);
            col.childControlHeight = true;
            col.childControlWidth = true;
            col.childForceExpandHeight = false;
            Edge(reader.transform, Accent);
            Para(reader.rectTransform, body, _regular, 21f, TextMain, 8f);
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
            Place(paper.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-600f, 132f), new Vector2(600f, 0f));
            Outline(paper.gameObject, Line);
            var band = Box("Band", paper.transform, Bureau);
            Place(band.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -6f), Vector2.zero);

            var scroll = Scroll("Doc", paper.rectTransform, out var doc, 12f, new RectOffset(64, 72, 44, 48));
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

            if (!string.IsNullOrEmpty(Deduction.SummaryTextId))
            {
                ReportSection(doc, L(T + "report_summary"), L(Deduction.SummaryTextId), true, _regular, 25f);
            }

            var analyses = new StringBuilder();
            for (int i = 0; i < Deduction.Analyses.Count; i++)
            {
                var a = Deduction.Analyses[i];
                if (a == null || !CaseNotebook.HasAnalysis(Data, Deduction, a.analysisId)) continue;
                if (analyses.Length > 0) analyses.Append('\n');
                analyses.Append(AnalysisNo(i)).Append("   ").Append(L(a.titleTextId))
                    .Append("  <color=#").Append(ColorUtility.ToHtmlStringRGB(TextSub)).Append("><size=80%>(")
                    .Append(ShortId(a.clueA)).Append(" ↔ ").Append(ShortId(a.clueB)).Append(")</size></color>");
            }
            ReportSection(doc, L(T + "report_analyses"), analyses.ToString());

            var clues = new StringBuilder();
            foreach (var clue in CaseNotebook.AcquiredClues(Data, Deduction))
            {
                if (clues.Length > 0) clues.Append('\n');
                clues.Append(ShortId(clue.clueId)).Append("   ").Append(_ctx.ClueTitle(clue.clueId))
                    .Append("  <color=#").Append(ColorUtility.ToHtmlStringRGB(TextSub)).Append("><size=80%>(")
                    .Append(L(clue.placeTextId)).Append(")</size></color>");
            }
            ReportSection(doc, L(T + "report_clues"), clues.ToString(), false);

            // 아래로 더 있다는 것을 알린다. 끝까지 읽어야만 확정할 수 있는 것은 아니다.
            var more = Label("More", _body, _regular, 18f, TextDim, TextAlignmentOptions.Center);
            Place(more.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-600f, 100f), new Vector2(600f, 128f));
            more.text = "▼  " + L(T + "report_scroll");

            var confirm = MakeButton("Confirm", _body, Bureau, out var confirmLabel, _semiBold, 27f);
            Place((RectTransform)confirm.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-220f, 10f), new Vector2(220f, 88f));
            confirmLabel.text = confirmed ? "✓  " + L(T + "confirmed") : L(T + "confirm");
            if (confirmed) SetEnabledLook(confirm, confirmLabel, false);
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

        private void ReportSection(RectTransform doc, string head, string body, bool divider = true, TMP_FontAsset font = null, float size = 27f)
        {
            Spacer(doc, 4f);
            Para(doc, head, _semiBold, 20f, Accent).characterSpacing = 1f;
            Para(doc, string.IsNullOrEmpty(body) ? "—" : body, font ?? _medium, size, TextMain, 8f);
            if (divider) Divider(doc);
        }

        /// <summary>
        /// 단추를 켜고 끈 모습. 꺼진 단추는 강조색을 빼고 어두운 판에 흐린 글로 둔다.
        /// 켜진 강조 단추와 한눈에 구분돼야 한다.
        /// </summary>
        private static void SetEnabledLook(Button button, TMP_Text label, bool on)
        {
            button.interactable = on;
            var image = button.targetGraphic as Image;
            if (!on)
            {
                if (image != null) image.color = CardColor;
                label.color = TextDim;
                return;
            }
            label.color = Bg;
        }

        /// <summary>
        /// 반투명한 판의 테두리. Outline 효과는 그림을 통째로 한 벌 더 그려서, 판이 비치면 테두리가 아니라 꽉 찬 색으로 보인다.
        /// 그래서 가는 선 넷을 둘레에 붙인다.
        /// </summary>
        private static void Border(Transform parent, Color color)
        {
            Line1(parent, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -1f), Vector2.zero, color);
            Line1(parent, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 1f), color);
            Line1(parent, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(1f, 0f), color);
            Line1(parent, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-1f, 0f), Vector2.zero, color);
        }

        private static void Line1(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax, Color color)
        {
            var line = Box(name, parent, color);
            Place(line.rectTransform, min, max, offMin, offMax);
        }

        /// <summary>판의 왼쪽에 세우는 가는 색 띠.</summary>
        private static void Edge(Transform parent, Color color)
        {
            var bar = Box("Edge", parent, color);
            Place(bar.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(4f, 0f));
            bar.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
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
        private TMP_Text Para(RectTransform parent, string text, TMP_FontAsset font, float size, Color color, float lineSpacing = 0f)
        {
            var t = Label("Text", parent, font, size, color, TextAlignmentOptions.TopLeft);
            t.text = text;
            t.enableWordWrapping = true;
            t.lineSpacing = lineSpacing;
            return t;
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
            v.padding = padding ?? new RectOffset(0, 20, 0, 12);
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;

            // 오른쪽 끝의 가는 막대. 굴러갈 것이 있을 때만 보인다. 아래에 글이 더 있다는 것을 알린다.
            var bar = NewRect("Scrollbar", rt);
            bar.anchorMin = new Vector2(1f, 0f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(1f, 0.5f);
            bar.sizeDelta = new Vector2(6f, -16f);
            bar.anchoredPosition = new Vector2(-4f, 0f);
            var track = bar.gameObject.AddComponent<Image>();
            track.color = new Color(1f, 1f, 1f, 0.04f);
            var area = NewRect("Area", bar);
            Stretch(area);
            var handle = NewRect("Handle", area);
            Stretch(handle);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Hex("4A5A68");
            var sb = bar.gameObject.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;
            sb.handleRect = handle;
            sb.targetGraphic = handleImage;
            scroll.verticalScrollbar = sb;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

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
