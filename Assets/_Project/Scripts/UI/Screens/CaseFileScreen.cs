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

        /// <summary>게시물 기록 한 줄. 제목, 작성자·조회 같은 한 줄, 본문, 이미 열어 본 글인가.</summary>
        public class PostRecord
        {
            public string Title;
            public string Meta;
            public string Body;
            public bool Viewed;
        }

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

            /// <summary>보고서의 게시물 기록 확인에 늘어놓을 글들. 읽기만 한다.</summary>
            public Func<List<PostRecord>> PostRecords;
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
            _blankResults.Clear();
            _reviewed = false;
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
            FlushReportInputs();
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
                label.textWrappingMode = TextWrappingModes.Normal;
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
            hint.textWrappingMode = TextWrappingModes.Normal;

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
            text.textWrappingMode = TextWrappingModes.Normal;
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
            body.textWrappingMode = TextWrappingModes.Normal;
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
            question.textWrappingMode = TextWrappingModes.Normal;

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
            optLabel.textWrappingMode = TextWrappingModes.Normal;

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

            // 빈칸 자료가 있는 사건은 문장 속 빈칸을 직접 적어 채우는 보고서다.
            if (Deduction.BlankCount > 0)
            {
                BuildFillReport(confirmed);
                return;
            }

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

        // ------------------------------------------------------------- 빈칸 보고서

        /// <summary>문단 속 빈칸 하나의 화면 조각. 판정 색을 바꿀 때 다시 짓지 않고 이것만 칠한다.</summary>
        private class BlankView
        {
            public CaseDeductionSO.ReportBlank Blank;
            public RectTransform Section;
            public TMP_InputField Field;
            public Image Underline;
            public TMP_Text Mark;
            public int LastLength;
        }

        /// <summary>빈칸의 판정. 고쳐 쓰기 시작하면 판정 전으로 돌아간다.</summary>
        private enum BlankState { Unjudged, Right, Wrong, Empty }

        private readonly List<BlankView> _blankViews = new List<BlankView>();
        private readonly Dictionary<string, BlankState> _blankResults = new Dictionary<string, BlankState>();

        private TMP_Text _reportFeedback;
        private Button _confirmButton;
        private TMP_Text _confirmLabel;
        private ScrollRect _reportScroll;
        private TMP_Text _measure;
        private bool _reviewed;
        private float _lastKeySound;
        private string _lastComposition = string.Empty;

        // 종이 색. 새하얗지 않은 오래된 문서 빛. 글은 먹색, 빈칸 글씨는 짙은 남색 잉크.
        private static readonly Color PaperColor = Hex("D9D4C8");
        private static readonly Color PaperRule = Hex("B3AC9C");
        private static readonly Color Ink = Hex("24272C");
        private static readonly Color InkSub = Hex("575B62");
        private static readonly Color InkRed = Hex("8E3237");
        private static readonly Color BlankInk = Hex("1C3D66");
        private static readonly Color Under = Hex("857F70");
        private static readonly Color UnderFocus = Hex("2F6E9C");
        private static readonly Color RightColor = Hex("2E7D4B");
        private static readonly Color WrongColor = Hex("B1363C");

        // 화면 아래(어두운 판 위) 알림 줄의 초록과 빨강. 어두운 바탕에서 읽히도록 밝게.
        private static readonly Color RightOnDark = Hex("5DB97C");
        private static readonly Color WrongOnDark = Hex("D9666C");

        private const float PaperWidth = 1240f;
        private const float PaperPadLeft = 72f;
        private const float PaperPadRight = 86f;
        private const float BodySize = 25f;
        private const float LineHeight = 50f;

        /// <summary>
        /// 핵심 낱말만 채우는 사건 보고서.
        ///
        /// 이미 적혀 있는 보고서 문단을 읽어 내려가다가, 문장 사이의 밑줄 빈칸에 짧은 낱말을 적는다.
        /// 적는 동안에는 판정하지 않는다. "보고서 검토"를 누르면 한꺼번에 견주어
        /// 맞은 빈칸은 초록 밑줄(✓), 틀린 빈칸은 빨간 밑줄(×), 비운 빈칸은 회색으로 둔다. 정답은 알려 주지 않는다.
        /// 맞은 칸은 그대로 두고 틀린 칸만 고치면 된다. 다 맞아야 "조사 결과 확정"이 켜진다.
        /// 확정한 뒤에는 빈칸을 고칠 수 없고 적은 낱말이 초록 밑줄 위에 남는다.
        /// </summary>
        private void BuildFillReport(bool confirmed)
        {
            _blankViews.Clear();
            if (confirmed)
            {
                foreach (var b in Deduction.ReportBlanks) if (b != null && b.HasBlank) _blankResults[b.blankId] = BlankState.Right;
            }

            var paper = Panel("Paper", _body, PaperColor);
            Place(paper.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-PaperWidth / 2f, 150f), new Vector2(PaperWidth / 2f, 0f));
            var band = Box("Band", paper.transform, InkRed);
            Place(band.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -6f), Vector2.zero);

            _reportScroll = Scroll("Doc", paper.rectTransform, out var doc, 10f, new RectOffset((int)PaperPadLeft, (int)PaperPadRight, 40, 60));
            Stretch((RectTransform)_reportScroll.transform);
            ((RectTransform)_reportScroll.transform).offsetMax = new Vector2(0f, -6f);   // 위쪽 붉은 띠 아래에서부터 굴러간다
            PaperScrollbar(_reportScroll);
            _reportScroll.onValueChanged.AddListener(_ => PaintReportFooter());

            // 낱말 폭을 재는 데만 쓰는 글. 화면에는 보이지 않는다.
            _measure = Label("Measure", paper.rectTransform, _medium, BodySize, Ink, TextAlignmentOptions.TopLeft);
            _measure.color = new Color(0f, 0f, 0f, 0f);

            // 문서 머리: 기관과 문서 종류, 기밀 표시, 사건 번호와 사건명
            var headRow = NewRect("HeadRow", doc);
            Layout(headRow.gameObject, 34f);
            var org = Label("Org", headRow, _bold, 21f, Ink, TextAlignmentOptions.MidlineLeft);
            Stretch(org.rectTransform);
            org.text = L(T + "paper_org");
            org.characterSpacing = 2f;
            var stamp = Label("Stamp", headRow, _bold, 20f, InkRed, TextAlignmentOptions.MidlineRight);
            Stretch(stamp.rectTransform);
            stamp.text = "CONFIDENTIAL";
            stamp.characterSpacing = 6f;

            Para(doc, L(T + "paper_title"), _bold, 38f, Ink);
            Para(doc, L(T + "paper_meta", Deduction.DocumentNumber, L(_ctx.CaseNameTextId)), _medium, 21f, InkSub);
            PaperDivider(doc, 2f);
            if (!confirmed) Para(doc, L(T + "paper_guide"), _regular, 20f, InkSub, 4f);

            int no = 0;
            foreach (var section in Deduction.ReportBlanks)
            {
                if (section == null) continue;
                no++;
                Spacer(doc, 8f);
                Para(doc, "<color=#" + ColorUtility.ToHtmlStringRGB(InkRed) + ">" + no + ".</color>  " + L(section.titleTextId), _bold, 24f, Ink);
                FlowParagraph(doc, section, confirmed);
            }

            // 확정한 뒤에만 종합 판단을 덧붙인다. 그 전에 보이면 답을 적어 주는 셈이다.
            if (confirmed && !string.IsNullOrEmpty(Deduction.SummaryTextId))
            {
                Spacer(doc, 8f);
                PaperDivider(doc, 1f);
                Para(doc, L(T + "report_summary"), _bold, 22f, Ink);
                Para(doc, L(Deduction.SummaryTextId), _regular, 23f, Ink, 10f);
            }

            Spacer(doc, 8f);
            PaperDivider(doc, 1f);
            var refs = new StringBuilder();
            for (int i = 0; i < Deduction.Analyses.Count; i++)
            {
                var a = Deduction.Analyses[i];
                if (a == null || !CaseNotebook.HasAnalysis(Data, Deduction, a.analysisId)) continue;
                refs.Append(refs.Length > 0 ? "\n" : string.Empty).Append(AnalysisNo(i)).Append("   ").Append(L(a.titleTextId));
            }
            Para(doc, L(T + "report_analyses"), _bold, 20f, InkSub);
            Para(doc, refs.Length > 0 ? refs.ToString() : "—", _regular, 21f, InkSub, 6f);

            // 관련 단서는 번호 순으로 적는다. 자료의 순서(얻는 순서)는 건드리지 않는다.
            var sorted = CaseNotebook.AcquiredClues(Data, Deduction);
            sorted.Sort((x, y) => string.CompareOrdinal(ShortId(x.clueId), ShortId(y.clueId)));
            var clues = new StringBuilder();
            foreach (var clue in sorted)
            {
                clues.Append(clues.Length > 0 ? "\n" : string.Empty).Append(ShortId(clue.clueId)).Append("   ").Append(_ctx.ClueTitle(clue.clueId));
            }
            Spacer(doc, 4f);
            Para(doc, L(T + "report_clues"), _bold, 20f, InkSub);
            Para(doc, clues.Length > 0 ? clues.ToString() : "—", _regular, 21f, InkSub, 6f);

            // 아래 고정 줄(어두운 판 위): 진행 알림 한 줄, 그 아래 단추 셋
            _reportFeedback = Label("Feedback", _body, _semiBold, 21f, TextSub, TextAlignmentOptions.Center);
            Place(_reportFeedback.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-620f, 100f), new Vector2(620f, 142f));
            _reportFeedback.overflowMode = TextOverflowModes.Ellipsis;

            var posts = MakeButton("Posts", _body, CardColor, out var postsLabel, _semiBold, 22f);
            Place((RectTransform)posts.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-560f, 12f), new Vector2(-196f, 86f));
            postsLabel.text = L(T + "posts_button");
            Outline(posts.gameObject, Line);
            posts.onClick.AddListener(ShowPostRecords);

            var review = MakeButton("Review", _body, Accent, out var reviewLabel, _semiBold, 23f);
            Place((RectTransform)review.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-182f, 12f), new Vector2(182f, 86f));
            reviewLabel.text = L(T + "report_review");
            SetEnabledLook(review, reviewLabel, !confirmed);
            review.onClick.AddListener(ReviewReport);

            _confirmButton = MakeButton("Confirm", _body, Bureau, out _confirmLabel, _semiBold, 23f);
            Place((RectTransform)_confirmButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(196f, 12f), new Vector2(560f, 86f));
            _confirmButton.onClick.AddListener(ConfirmFillReport);

            PaintReport();
            StartCoroutine(FadeIn(paper.rectTransform, 0.25f));
            StartCoroutine(RepaintFooterAfterLayout());
        }

        /// <summary>문서 높이는 배치가 한 번 돈 뒤에야 정해진다. 그때 아래에 칸이 더 남았는지 다시 알린다.</summary>
        private IEnumerator RepaintFooterAfterLayout()
        {
            // 한 프레임으로는 문서 높이가 아직 0 일 때가 있다. 배치가 끝나도록 잠깐 둔다.
            yield return new WaitForSecondsRealtime(0.1f);
            PaintReportFooter();
        }

        /// <summary>
        /// 문단 하나를 종이 위에 흘려 쓴다. 낱말 폭을 재어 줄을 바꾸고, {_} 자리에 밑줄 빈칸을 끼운다.
        /// Unity 의 글 상자는 글 사이에 입력칸을 끼우지 못하므로 줄을 직접 나눈다.
        /// 빈칸이 줄 끝에 걸리면 통째로 다음 줄로 넘긴다. 문단 높이는 줄 수만큼 잡는다.
        /// </summary>
        private void FlowParagraph(RectTransform doc, CaseDeductionSO.ReportBlank section, bool confirmed)
        {
            var box = NewRect("Paragraph_" + (section.HasBlank ? section.blankId : section.titleTextId), doc);
            float width = PaperWidth - PaperPadLeft - PaperPadRight - 10f;

            string text = L(section.bodyTextId);
            int at = section.HasBlank ? text.IndexOf("{_}", StringComparison.Ordinal) : -1;
            string before = at >= 0 ? text.Substring(0, at) : text;
            string after = at >= 0 ? text.Substring(at + 3) : string.Empty;

            _measure.font = _medium;
            _measure.fontSize = BodySize;
            float space = Mathf.Max(6f, _measure.GetPreferredValues("가 가").x - _measure.GetPreferredValues("가가").x);

            float x = 0f, y = 0f;
            var seg = new StringBuilder();
            float segX = 0f;

            void Flush()
            {
                if (seg.Length == 0) return;
                var label = Label("Line", box, _medium, BodySize, Ink, TextAlignmentOptions.MidlineLeft);
                label.text = seg.ToString();
                var rt = label.rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(_measure.GetPreferredValues(label.text).x + 8f, LineHeight);
                rt.anchoredPosition = new Vector2(segX, -y);
                seg.Clear();
            }

            void Words(string part)
            {
                if (string.IsNullOrEmpty(part)) return;
                var words = part.Split(' ');
                for (int i = 0; i < words.Length; i++)
                {
                    string word = words[i];
                    bool spaceAfter = i < words.Length - 1;
                    if (word.Length == 0)
                    {
                        if (spaceAfter) { if (seg.Length == 0) segX = x + space; else seg.Append(' '); x += space; }
                        continue;
                    }
                    float w = _measure.GetPreferredValues(word).x;
                    if (x + w > width && x > 0f)
                    {
                        Flush();
                        x = 0f;
                        y += LineHeight;
                    }
                    if (seg.Length == 0) segX = x;
                    seg.Append(word);
                    x += w;
                    if (spaceAfter) { seg.Append(' '); x += space; }
                }
            }

            Words(before);

            if (at >= 0)
            {
                float w = Mathf.Max(80f, section.blankWidth);
                if (x + w > width && x > 0f)
                {
                    Flush();
                    x = 0f;
                    y += LineHeight;
                }
                Flush();
                BlankField(box, section, x, y, w, confirmed);
                x += w + 2f;
                segX = x;
                Words(after);
            }
            Flush();

            Layout(box.gameObject, y + LineHeight + 6f);
        }

        /// <summary>
        /// 문장 사이의 밑줄 빈칸. 클릭하면 깜빡이는 세로 커서가 들어오고(입력란이 갖춘 캐럿을 쓴다) 짧은 낱말을 적는다.
        /// 고르면 밑줄이 파랗게 짙어지고, 검토하면 초록·빨강으로 바뀐다. 오른쪽 위 작은 표가 ✓·× 다.
        /// </summary>
        private void BlankField(RectTransform box, CaseDeductionSO.ReportBlank section, float x, float y, float width, bool confirmed)
        {
            var fieldRt = NewRect("Blank_" + section.blankId, box);
            fieldRt.anchorMin = fieldRt.anchorMax = fieldRt.pivot = new Vector2(0f, 1f);
            fieldRt.sizeDelta = new Vector2(width, LineHeight);
            fieldRt.anchoredPosition = new Vector2(x, -y);

            // 눌리는 판. 거의 보이지 않는 옅은 빛만 깔아 빈칸 자리를 드러낸다.
            var hit = fieldRt.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0.16f);

            var underline = Box("Underline", fieldRt, Under);
            var ur = underline.rectTransform;
            ur.anchorMin = new Vector2(0f, 0f);
            ur.anchorMax = new Vector2(1f, 0f);
            ur.pivot = new Vector2(0.5f, 0f);
            ur.sizeDelta = new Vector2(0f, 2f);
            ur.anchoredPosition = new Vector2(0f, 4f);

            var area = NewRect("TextArea", fieldRt);
            Place(area, Vector2.zero, Vector2.one, new Vector2(8f, 2f), new Vector2(-24f, -2f));   // 글 줄 높이와 같은 한가운데에 놓여 앞뒤 문장과 나란하다
            area.gameObject.AddComponent<RectMask2D>();

            var placeholder = Label("Placeholder", area, _regular, BodySize - 2f, new Color(InkSub.r, InkSub.g, InkSub.b, 0.0f), TextAlignmentOptions.MidlineLeft);
            Stretch(placeholder.rectTransform);
            placeholder.text = string.Empty;

            var text = Label("Text", area, _semiBold, BodySize, BlankInk, TextAlignmentOptions.MidlineLeft);
            Stretch(text.rectTransform);
            text.textWrappingMode = TextWrappingModes.NoWrap;

            var field = fieldRt.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = area;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.fontAsset = text.font;
            field.pointSize = BodySize;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.characterLimit = 24;
            field.richText = false;
            field.onFocusSelectAll = false;
            field.resetOnDeActivation = true;
            // 깜빡이는 세로 커서: 입력란이 갖춘 캐럿을 쓴다. 잉크 빛으로, 조금 굵게.
            field.customCaretColor = true;
            field.caretColor = BlankInk;
            field.caretWidth = 2;
            field.caretBlinkRate = 1.0f;
            field.selectionColor = new Color(UnderFocus.r, UnderFocus.g, UnderFocus.b, 0.3f);
            field.targetGraphic = hit;
            var colors = field.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1.6f);
            colors.selectedColor = new Color(1f, 1f, 1f, 1.8f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.5f);
            colors.colorMultiplier = 1f;
            field.colors = colors;
            field.SetTextWithoutNotify(CaseNotebook.GetInput(Data, Deduction.CaseId, section.blankId));
            field.interactable = !confirmed;

            var mark = Label("Mark", fieldRt, _bold, 18f, Under, TextAlignmentOptions.TopRight);
            Place(mark.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(-2f, 0f));

            var view = new BlankView { Blank = section, Section = box, Field = field, Underline = underline, Mark = mark, LastLength = field.text.Length };
            _blankViews.Add(view);

            string id = section.blankId;
            field.onValueChanged.AddListener(value =>
            {
                // 적는 대로 저장본(메모리)에 옮겨 둔다. 파일로 쓰는 것은 칸을 떠날 때, 화면을 닫을 때, 앱이 멈출 때다.
                CaseNotebook.SetInput(Data, Deduction.CaseId, id, value);
                _save?.MarkDirty();

                // 실제로 글자가 늘거나 줄었을 때만 타건음. 붙여넣기처럼 한꺼번에 늘어도 한 번이다.
                int delta = value.Length - view.LastLength;
                view.LastLength = value.Length;
                if (delta != 0) KeySound(delta < 0);

                // 고쳐 쓰기 시작하면 그 칸은 판정 전으로 돌아간다. 맞은 칸을 건드리지 않으면 그대로 맞은 채다.
                if (_blankResults.Remove(id)) PaintReport();
                else PaintReportFooter();
            });
            field.onEndEdit.AddListener(_ => _save?.AutoSave());
            field.onSubmit.AddListener(_ => FocusBlank(_blankViews.IndexOf(view) + 1));
        }

        /// <summary>
        /// 타건음 한 번. 오래된 단말기의 낮은 키 소리를 작게. 지울 때는 더 작고 낮게.
        /// 빠르게 치거나 한글 조합으로 한 번에 여러 번 불려도 0.045초에 한 번만 낸다.
        /// 크기는 Sfx 가 설정의 효과음 크기를 곱해 내므로 효과음을 끄면 나지 않는다.
        /// </summary>
        private void KeySound(bool erase)
        {
            float now = Time.unscaledTime;
            if (now - _lastKeySound < 0.045f) return;
            _lastKeySound = now;
            string clip = "key_" + UnityEngine.Random.Range(0, 4);
            if (erase) Sfx.Play(clip, 0.14f, 0.82f);
            else Sfx.Play(clip, 0.26f, UnityEngine.Random.Range(0.94f, 1.08f));
        }

        private void PaperDivider(RectTransform parent, float thickness)
        {
            var line = Box("Rule", parent, PaperRule);
            Layout(line.gameObject, thickness);
        }

        /// <summary>종이 위 스크롤 막대. 늘 보이게, 종이 빛에 맞춘 회색으로. 아래에 더 있다는 것이 보여야 한다.</summary>
        private static void PaperScrollbar(ScrollRect scroll)
        {
            var bar = scroll.verticalScrollbar;
            if (bar == null) return;
            var rt = (RectTransform)bar.transform;
            rt.sizeDelta = new Vector2(8f, -16f);
            rt.anchoredPosition = new Vector2(-10f, 0f);
            bar.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.08f);
            if (bar.targetGraphic is Image handle) handle.color = Hex("8C8574");
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }

        /// <summary>빈칸들의 밑줄 색과 표, 아래 알림, 확정 단추를 지금 판정대로 칠한다. 다시 짓지 않는다(적고 있는 칸의 커서를 지키려고).</summary>
        private void PaintReport()
        {
            foreach (var v in _blankViews) PaintBlank(v);

            bool confirmed = CaseNotebook.IsReportConfirmed(Data, Deduction.CaseId);
            if (_confirmButton != null)
            {
                _confirmLabel.text = confirmed ? "✓  " + L(T + "confirmed") : L(T + "confirm");
                if (confirmed || !AllBlanksRight()) SetEnabledLook(_confirmButton, _confirmLabel, false);
                else
                {
                    _confirmButton.interactable = true;
                    ((Image)_confirmButton.targetGraphic).color = Bureau;
                    _confirmLabel.color = TextMain;
                }
            }
            PaintReportFooter();
        }

        private void PaintBlank(BlankView v)
        {
            if (v == null || v.Field == null) return;
            _blankResults.TryGetValue(v.Blank.blankId, out var state);
            bool focused = v.Field.isFocused;
            Color line = state == BlankState.Right ? RightColor : state == BlankState.Wrong ? WrongColor : focused ? UnderFocus : Under;
            v.Underline.color = line;
            v.Underline.rectTransform.sizeDelta = new Vector2(0f, state != BlankState.Unjudged && state != BlankState.Empty || focused ? 3f : 2f);
            v.Mark.text = state == BlankState.Right ? "✓" : state == BlankState.Wrong ? "×" : string.Empty;
            v.Mark.color = line;
        }

        /// <summary>
        /// 아래 한 줄. 확정 뒤에는 "조사 보고서 확정 완료", 다 맞으면 완성 알림,
        /// 검토 뒤에는 "정답 2/4 · 수정 필요 1 · 미입력 1", 그 전에는 "작성 3/4" 와 아래에 칸이 남았다는 안내.
        /// </summary>
        private void PaintReportFooter()
        {
            if (_reportFeedback == null || Deduction == null) return;
            int total = Deduction.BlankCount;

            if (CaseNotebook.IsReportConfirmed(Data, Deduction.CaseId))
            {
                _reportFeedback.text = "✓  " + L(T + "report_confirmed_done");
                _reportFeedback.color = RightOnDark;
                return;
            }

            int right = 0, wrong = 0, empty = 0, written = 0;
            foreach (var v in _blankViews)
            {
                if (v?.Field == null) continue;
                if (CaseNotebook.Normalize(v.Field.text).Length > 0) written++;
                _blankResults.TryGetValue(v.Blank.blankId, out var state);
                if (state == BlankState.Right) right++;
                else if (state == BlankState.Wrong) wrong++;
                else if (state == BlankState.Empty) empty++;
            }

            if (AllBlanksRight())
            {
                _reportFeedback.text = L(T + "report_all_right", total);
                _reportFeedback.color = RightOnDark;
                return;
            }

            if (_reviewed && right + wrong + empty > 0)
            {
                _reportFeedback.text = L(T + "report_progress", right, total, wrong, empty);
                _reportFeedback.color = wrong > 0 ? WrongOnDark : Soft;
                return;
            }

            // 아직 검토하지 않았다. 몇 칸을 적었는지, 아래에 더 있는지만 알린다.
            bool more = _reportScroll != null && _reportScroll.verticalNormalizedPosition > 0.03f
                        && _reportScroll.content.rect.height > ((RectTransform)_reportScroll.transform).rect.height;
            _reportFeedback.text = L(T + "report_written", written, total) + (more ? "   ▼ " + L(T + "report_more_below") : string.Empty);
            _reportFeedback.color = TextSub;
        }

        private bool AllBlanksRight()
        {
            if (Deduction.BlankCount == 0) return false;
            foreach (var b in Deduction.ReportBlanks)
            {
                if (b == null || !b.HasBlank) continue;
                if (!_blankResults.TryGetValue(b.blankId, out var state) || state != BlankState.Right) return false;
            }
            return true;
        }

        /// <summary>
        /// 보고서 검토. 모든 빈칸을 한꺼번에 견준다. 틀린 칸이 어디인지만 알리고 정답은 말하지 않는다.
        /// 고칠 칸이 있으면 그 문단이 보이도록 문서를 굴린다. 적고 있던 칸에서 커서를 빼앗지는 않는다.
        /// </summary>
        private void ReviewReport()
        {
            if (CaseNotebook.IsReportConfirmed(Data, Deduction.CaseId)) return;

            BlankView firstProblem = null;
            foreach (var v in _blankViews)
            {
                string input = v.Field != null ? v.Field.text : string.Empty;
                BlankState state = CaseNotebook.Normalize(input).Length == 0 ? BlankState.Empty
                    : CaseNotebook.CheckAnswer(v.Blank, input) ? BlankState.Right : BlankState.Wrong;
                _blankResults[v.Blank.blankId] = state;
                if (state != BlankState.Right && firstProblem == null) firstProblem = v;
            }

            _reviewed = true;
            _save?.AutoSave();
            Sfx.PlayVaried("paper_" + UnityEngine.Random.Range(0, 2), 0.5f);
            PaintReport();
            if (firstProblem != null) ScrollToSection(firstProblem.Section);

            Debug.Log("[CaseFile] 보고서 검토 | " + _reportFeedback.text);
        }

        /// <summary>그 문단이 문서 창 위쪽에 오도록 굴린다.</summary>
        private void ScrollToSection(RectTransform section)
        {
            if (_reportScroll == null || section == null) return;
            Canvas.ForceUpdateCanvases();
            var content = _reportScroll.content;
            float viewport = ((RectTransform)_reportScroll.transform).rect.height;
            float over = content.rect.height - viewport;
            if (over <= 0f) return;
            float top = -section.anchoredPosition.y - section.rect.height * section.pivot.y - 80f;
            _reportScroll.verticalNormalizedPosition = 1f - Mathf.Clamp01(top / over);
        }

        /// <summary>n 번째 빈칸으로 옮긴다. 마지막 칸 다음이면 아무 칸에도 두지 않는다. Tab, Shift+Tab, Enter 가 이것을 쓴다.</summary>
        private void FocusBlank(int index)
        {
            if (index < 0 || index >= _blankViews.Count)
            {
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
                return;
            }
            var field = _blankViews[index].Field;
            if (field == null || !field.interactable) return;
            field.Select();
            field.ActivateInputField();
            ScrollToSection(_blankViews[index].Section);
        }

        private void Update()
        {
            if (_blankViews.Count == 0) return;

            // 고른 빈칸은 밑줄이 짙어진다. 판정 색이 있으면 그 색을 지킨다.
            BlankView focused = null;
            foreach (var v in _blankViews)
            {
                if (v?.Field == null) continue;
                if (v.Field.isFocused) focused = v;
                PaintBlank(v);
            }
            if (focused == null) { _lastComposition = string.Empty; return; }

            // 한글을 조합하는 동안에는 글이 칸에 들어가기 전이라 값이 바뀌지 않는다. 조합 글자가 늘 때도 타건음을 낸다.
            var module = UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.currentInputModule : null;
            string composing = module != null && module.input != null ? module.input.compositionString ?? string.Empty : string.Empty;
            if (composing != _lastComposition)
            {
                if (composing.Length > 0) KeySound(composing.Length < _lastComposition.Length);
                _lastComposition = composing;
            }

            // 빈칸 사이를 Tab / Shift+Tab 으로 오간다. TMP 입력란은 스스로 하지 않는다.
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame) return;
            int i = _blankViews.IndexOf(focused);
            FocusBlank(keyboard.shiftKey.isPressed ? Mathf.Max(0, i - 1) : i + 1);
        }

        /// <summary>
        /// 적고 있던 글을 빠짐없이 저장본에 옮기고 파일로 쓴다. 화면을 닫을 때, 앱이 멈추거나 가려질 때 부른다.
        /// 한글을 조합하는 중이면 아직 칸에 들어가지 않은 글자(조합 중인 글자)도 함께 붙여 둔다.
        /// </summary>
        private void FlushReportInputs()
        {
            if (Deduction == null || _blankViews.Count == 0) return;
            string composing = null;
            var module = UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.currentInputModule : null;
            if (module != null && module.input != null) composing = module.input.compositionString;

            foreach (var v in _blankViews)
            {
                if (v?.Field == null) continue;
                string text = v.Field.text;
                if (v.Field.isFocused && !string.IsNullOrEmpty(composing)) text += composing;
                CaseNotebook.SetInput(Data, Deduction.CaseId, v.Blank.blankId, text);
            }
            _save?.MarkDirty();
            _save?.AutoSave();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && IsOpen) FlushReportInputs();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && IsOpen) FlushReportInputs();
        }

        private void ConfirmFillReport()
        {
            if (CaseNotebook.IsReportConfirmed(Data, Deduction.CaseId) || !AllBlanksRight()) return;

            FlushReportInputs();
            CaseNotebook.ConfirmReport(Data, Deduction.CaseId);
            _save?.MarkDirty();
            _save?.AutoSave();
            Debug.Log("[CaseFile] 조사 결과 확정 | " + Deduction.CaseId);
            Refresh();
            _ctx.OnConfirm?.Invoke();
        }

        // ------------------------------------------------------------- 게시물 기록

        /// <summary>
        /// 게시물 기록 확인. 이 괴담에 관한 게시물을 읽기만 하는 창을 보고서 위에 띄운다.
        /// 글을 새로 쓰거나 검열하지 않고, 시간·믿음도·확산도도 바뀌지 않는다.
        /// 보고서는 다시 짓지 않으므로 적던 글과 커서 자리는 그대로 남는다.
        /// </summary>
        private void ShowPostRecords()
        {
            FlushReportInputs();
            var records = _ctx.PostRecords != null ? _ctx.PostRecords() : new List<PostRecord>();
            _postIndex = Mathf.Clamp(_postIndex, 0, Mathf.Max(0, records.Count - 1));
            BuildPostOverlay(records);
        }

        private int _postIndex;

        private void BuildPostOverlay(List<PostRecord> records)
        {
            HideOverlay();

            var dim = NewRect("Posts", (RectTransform)transform);
            Stretch(dim);
            dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.66f);
            _overlay = dim.gameObject.AddComponent<CanvasGroup>();

            var panel = Panel("Panel", dim, PanelColor);
            Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-760f, -420f), new Vector2(760f, 420f));
            Outline(panel.gameObject, Line);
            var top = Box("Top", panel.transform, Accent);
            Place(top.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -5f), Vector2.zero);

            var head = Label("Head", panel.rectTransform, _bold, 30f, TextMain, TextAlignmentOptions.TopLeft);
            Place(head.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -76f), new Vector2(-260f, -30f));
            head.text = L(T + "posts_title");
            var sub = Label("Sub", panel.rectTransform, _regular, 19f, TextSub, TextAlignmentOptions.TopLeft);
            Place(sub.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -110f), new Vector2(-260f, -78f));
            sub.text = L(T + "posts_hint");

            var back = MakeButton("Back", panel.rectTransform, CardColor, out var backLabel, _semiBold, 21f);
            Place((RectTransform)back.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-250f, -86f), new Vector2(-30f, -30f));
            backLabel.text = L(T + "posts_back");
            Outline(back.gameObject, Line);
            back.onClick.AddListener(HideOverlay);

            // 왼쪽 목록
            var listScroll = Scroll("List", panel.rectTransform, out var list, 8f, new RectOffset(0, 18, 0, 10));
            Place((RectTransform)listScroll.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(30f, 30f), new Vector2(560f, -130f));

            if (records.Count == 0)
            {
                Para(list, L(T + "posts_empty"), _regular, 21f, TextDim);
            }

            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                bool on = i == _postIndex;
                var item = MakeButton("Post_" + i, list, on ? CardHover : CardColor, out var itemLabel, _semiBold, 21f);
                Layout(item.gameObject, 84f);
                if (on) Outline(item.gameObject, Accent);
                Place(itemLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(20f, -2f), new Vector2(-18f, -8f));
                itemLabel.alignment = TextAlignmentOptions.BottomLeft;
                itemLabel.text = r.Title;
                itemLabel.overflowMode = TextOverflowModes.Ellipsis;
                var meta = Label("Meta", item.transform, _regular, 16f, r.Viewed ? Accent : TextDim, TextAlignmentOptions.TopLeft);
                Place(meta.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(20f, 6f), new Vector2(-18f, -4f));
                meta.text = (r.Viewed ? L(T + "posts_viewed") + "  ·  " : string.Empty) + r.Meta;
                meta.overflowMode = TextOverflowModes.Ellipsis;
                int captured = i;
                item.onClick.AddListener(() => { _postIndex = captured; BuildPostOverlay(records); });
            }

            // 오른쪽 본문
            var reader = Panel("Reader", panel.rectTransform, Hex("141A21"));
            Place(reader.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(586f, 30f), new Vector2(-30f, -130f));
            if (records.Count > 0)
            {
                var r = records[_postIndex];
                var readScroll = Scroll("Body", reader.rectTransform, out var body, 12f, new RectOffset(32, 40, 28, 28));
                Stretch((RectTransform)readScroll.transform);
                Para(body, r.Title, _bold, 28f, TextMain);
                Para(body, r.Meta, _regular, 18f, TextSub);
                Divider(body);
                Para(body, r.Body, _regular, 22f, TextMain, 10f);
            }

            StartCoroutine(PopIn(panel.rectTransform, _overlay, 0.25f));
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
            text.textWrappingMode = TextWrappingModes.Normal;
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
            t.textWrappingMode = TextWrappingModes.NoWrap;
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
            t.textWrappingMode = TextWrappingModes.Normal;
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
