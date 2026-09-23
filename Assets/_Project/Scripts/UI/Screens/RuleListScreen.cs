using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 규칙 추론 화면.
    ///
    /// 화면을 둘로 나눈다.
    ///   왼쪽 - 지금까지 모은 단서. 한 줄이 눌러서 고르는 칸이다.
    ///   오른쪽 - 그 단서로 세워 볼 수 있는 규칙 후보. 한 칸이 한 후보다.
    /// 아래에는 직전에 고른 결과 한 줄.
    ///
    /// 결론은 저절로 나오지 않는다. 플레이어가 왼쪽에서 근거로 삼을 단서를 직접 고르고,
    /// 그 근거로 오른쪽 규칙 하나를 짚어야 한다. 근거가 어긋나면 규칙이 맞아도 세워지지 않는다.
    /// 무엇이 맞는 짝인지는 화면이 모른다. 고른 것을 그대로 넘겨줄 뿐이다.
    ///
    /// 한 칸 안에서도 역할마다 글을 나눠 둔다.
    ///   규칙 문장 - 고르는 대상. 크고 밝게.
    ///   근거 단서 - 곁가지. 작고 흐리게.
    ///   확인함 표식 - 이미 골라 본 것에만 붙는 작은 딱지.
    /// 한 칸에 몰아 넣으면 크기가 같은 글이 줄만 바뀌어 이어져 읽히지 않는다.
    ///
    /// 판정은 하지 않는다. 어떤 후보를 보여줄지도, 고른 결과가 무엇인지도 CaseDirector가 정한다.
    /// 화면은 정답 여부를 알지 못한다.
    /// </summary>
    public class RuleListScreen : UIScreen
    {
        [Header("머리말")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _footerText;

        [Header("왼쪽: 모은 단서")]
        [Tooltip("단서 칸 전체. 결론이 나오면 이 칸도 물러난다.")]
        [SerializeField] private GameObject _clueCardRoot;

        [Tooltip("단서 칸의 제목. 화면이 제 문구로 적는다.")]
        [SerializeField] private TMP_Text _clueTitleText;

        [Tooltip("단서를 한 줄씩 늘어놓는 자리. 한 줄이 눌러서 고르는 칸이다.")]
        [SerializeField] private RectTransform _clueListRoot;

        [Tooltip("복제할 단서 한 칸.")]
        [SerializeField] private Button _clueTemplate;

        [Tooltip("단서를 하나도 얻지 못했을 때 그 자리에 적는 한 줄.")]
        [SerializeField] private TMP_Text _clueEmptyText;

        [Header("오른쪽: 규칙 후보")]
        [Tooltip("후보 칸의 제목.")]
        [SerializeField] private TMP_Text _listTitleText;

        [SerializeField] private RectTransform _listRoot;
        [SerializeField] private Button _itemTemplate;

        [Tooltip("세울 수 있는 후보가 하나도 없을 때 그 자리에 적는 한 줄.")]
        [SerializeField] private TMP_Text _emptyText;

        [Header("아래: 직전 결과")]
        [Tooltip("결과 한 줄을 담는 띠. 결과가 없으면 통째로 꺼진다.")]
        [SerializeField] private GameObject _resultRoot;

        [SerializeField] private TMP_Text _resultText;

        [Header("결론")]
        [Tooltip("맞는 규칙을 세우고 나서야 뜨는 칸. 그 전에는 꺼져 있다.")]
        [SerializeField] private GameObject _conclusionRoot;

        [SerializeField] private TMP_Text _conclusionTitleText;

        [Tooltip("이 괴담이 진짜인가 가짜인가.")]
        [SerializeField] private TMP_Text _verdictText;

        [Tooltip("이 괴담의 파훼법.")]
        [SerializeField] private TMP_Text _counterText;

        /// <summary>한 칸 안에서 각 글을 찾을 때 쓰는 이름. 씬을 짓는 쪽과 같아야 한다.</summary>
        private const string HeadName = "Text_ItemLabel";
        private const string NoteName = "Text_ItemNote";
        private const string BadgeName = "Badge";

        private const string ClueTitleTextId = "ui.slice.label_clue";
        private const string ListTitleTextId = "ui.rule.list_title";
        private const string EmptyTextId = "ui.rule.no_candidate";
        private const string AlreadyMarkTextId = "ui.rule.already_mark";

        private readonly List<Button> _spawned = new List<Button>();

        private string _titleId;
        private string _footerId;
        private IReadOnlyList<RuleSO> _rules;
        private Func<RuleSO, string> _headProvider;
        private Func<RuleSO, string> _noteProvider;
        private Func<RuleSO, bool> _deducedProvider;
        private Action<RuleSO> _onSelect;
        private Func<IReadOnlyList<ClueEntry>> _clueProvider;
        private Func<string> _resultProvider;

        /// <summary>단서 한 줄. 화면이 보여줄 것과 골랐을 때 돌려줄 것만 들고 있다.</summary>
        public class ClueEntry
        {
            public string ClueId;
            public string Text;

            /// <summary>규칙의 근거가 되는 단서인가. 표를 하나 더 붙인다.</summary>
            public bool IsKey;
        }

        private readonly List<Button> _clueButtons = new List<Button>();
        private readonly List<ClueEntry> _clueEntries = new List<ClueEntry>();

        /// <summary>지금 근거로 고른 단서들. 규칙을 짚을 때 이 목록이 함께 넘어간다.</summary>
        private readonly HashSet<string> _picked = new HashSet<string>();

        /// <summary>이 단서를 근거로 골라 두었는가.</summary>
        public bool IsPicked(string clueId) => _picked.Contains(clueId);

        /// <summary>근거로 고른 단서가 몇 개인가.</summary>
        public int PickedCount => _picked.Count;

        /// <summary>
        /// 후보 목록을 채운다. 결과 문구는 화면을 새로 열 때 지워진다.
        ///
        /// head 는 규칙 문장, note 는 그 근거가 된 단서, deduced 는 이미 골라 봤는지다.
        /// 셋을 따로 받는 이유는 한 칸 안에서 크기와 색을 달리 두기 때문이다.
        /// </summary>
        public void Bind(string titleTextId, string footerTextId,
            IReadOnlyList<RuleSO> rules,
            Func<RuleSO, string> headProvider,
            Func<RuleSO, string> noteProvider,
            Func<RuleSO, bool> deducedProvider,
            Action<RuleSO> onSelect,
            Func<IReadOnlyList<ClueEntry>> clueProvider = null)
        {
            _titleId = titleTextId;
            _footerId = footerTextId;
            _rules = rules;
            _headProvider = headProvider;
            _noteProvider = noteProvider;
            _deducedProvider = deducedProvider;
            _onSelect = onSelect;
            _clueProvider = clueProvider;
            _resultProvider = null;

            // 결론은 목록을 다시 채울 때 지우지 않는다.
            // 한 번 닿은 결론은 규칙을 더 눌러 봐도 그대로 남아 있어야 한다.
            Refresh();
        }


        /// <summary>
        /// 규칙이 아닌 답을 고르는 칸 하나.
        ///
        /// 규칙을 세우고 나면 아직 두 가지가 남는다. 진짜인가 가짜인가, 그리고 어떻게 끊는가.
        /// 그 두 물음도 같은 자리에서 같은 모양으로 묻는다. 화면은 무엇이 정답인지 모른다.
        /// </summary>
        public class Option
        {
            public string Id;
            public string Head;
            public string Note;

            /// <summary>이미 골라 봤다가 아닌 것으로 밝혀진 답인가.</summary>
            public bool Tried;
        }

        /// <summary>
        /// 오른쪽 칸을 규칙 후보 대신 답 후보로 바꾼다.
        ///
        /// 왼쪽의 단서 칸은 그대로 둔다. 무엇을 보고 그렇게 답하는지가 옆에 서 있어야 한다.
        /// </summary>
        public void BindOptions(string listTitleTextId, IReadOnlyList<Option> options, Action<string> onPick)
        {
            _listTitleOverrideId = listTitleTextId;
            _options = options;
            _onPickOption = onPick;
            Refresh();
        }

        /// <summary>답 고르기를 물리고 규칙 후보로 되돌린다.</summary>
        public void ClearOptions()
        {
            _listTitleOverrideId = null;
            _options = null;
            _onPickOption = null;
            Refresh();
        }

        private string _listTitleOverrideId;
        private IReadOnlyList<Option> _options;
        private Action<string> _onPickOption;

        /// <summary>직전 추론 결과를 알린다. 언어가 바뀌면 다시 조립되도록 만드는 방법을 받는다.</summary>
        public void ShowResult(Func<string> provider)
        {
            _resultProvider = provider;
            Refresh();
        }

        /// <summary>
        /// 조사가 닿은 결론을 알린다.
        ///
        /// 규칙을 맞히고 나서야 나온다. 답해야 하는 것은 둘뿐이다.
        ///   이 괴담이 진짜인가 가짜인가, 그리고 어떻게 끊는가.
        /// 둘 다 비워 주면 칸이 꺼진다.
        /// </summary>
        public void ShowConclusion(Func<string> verdict, Func<string> counter)
        {
            _verdictProvider = verdict;
            _counterProvider = counter;
            Refresh();
        }

        private Func<string> _verdictProvider;
        private Func<string> _counterProvider;

        protected override void OnOpen()
        {
            EventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);
            Refresh();
        }

        protected override void OnClose()
        {
            EventBus.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);
        }

        private void OnLanguageChanged(LanguageChangedEvent evt)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc)) return;

            if (_titleText != null) _titleText.text = string.IsNullOrEmpty(_titleId) ? string.Empty : loc.Get(_titleId);
            if (_footerText != null) _footerText.text = string.IsNullOrEmpty(_footerId) ? string.Empty : loc.Get(_footerId);

            if (_clueTitleText != null) _clueTitleText.text = loc.Get(ClueTitleTextId);
            if (_listTitleText != null)
            {
                _listTitleText.text = loc.Get(string.IsNullOrEmpty(_listTitleOverrideId)
                    ? ListTitleTextId : _listTitleOverrideId);
            }

            RebuildClues(loc);

            // 결과가 없을 때는 띠를 통째로 꺼 둔다. 빈 띠가 남아 있으면 자리만 차지한다.
            string result = _resultProvider != null ? _resultProvider() : string.Empty;
            if (_resultText != null) _resultText.text = result;
            if (_resultRoot != null) _resultRoot.SetActive(!string.IsNullOrEmpty(result));

            RefreshConclusion(loc);

            RebuildItems(loc);
        }

        /// <summary>
        /// 모은 단서를 한 줄씩 늘어놓는다. 한 줄이 눌러서 고르는 칸이다.
        ///
        /// 고른 것은 다시 채울 때도 지키려 하지만, 이미 없어진 단서는 놓는다.
        /// </summary>
        private void RebuildClues(LocalizationService loc)
        {
            if (_clueListRoot == null || _clueTemplate == null) return;

            for (int i = 0; i < _clueButtons.Count; i++)
            {
                if (_clueButtons[i] == null) continue;

                var old = _clueButtons[i].gameObject;
                old.transform.SetParent(null, false);
                old.SetActive(false);
                Destroy(old);
            }
            _clueButtons.Clear();

            _clueEntries.Clear();
            if (_clueProvider != null)
            {
                var got = _clueProvider();
                if (got != null) _clueEntries.AddRange(got);
            }

            // 목록에서 사라진 단서를 골라 둔 채로 두지 않는다.
            _picked.RemoveWhere(id => !_clueEntries.Exists(e => e != null && e.ClueId == id));

            for (int i = 0; i < _clueEntries.Count; i++)
            {
                var entry = _clueEntries[i];
                if (entry == null) continue;

                var item = Instantiate(_clueTemplate, _clueListRoot);
                item.gameObject.name = "Clue_" + entry.ClueId;
                item.gameObject.SetActive(true);

                string captured = entry.ClueId;
                item.onClick.RemoveAllListeners();
                item.onClick.AddListener(() => TogglePick(captured));

                _clueButtons.Add(item);
                PaintClue(item, entry);
            }

            if (_clueEmptyText != null)
            {
                _clueEmptyText.text = loc.Get(ClueEmptyTextId);
                _clueEmptyText.gameObject.SetActive(_clueEntries.Count == 0);
            }
        }

        private void TogglePick(string clueId)
        {
            if (!_picked.Remove(clueId)) _picked.Add(clueId);

            for (int i = 0; i < _clueButtons.Count && i < _clueEntries.Count; i++)
            {
                PaintClue(_clueButtons[i], _clueEntries[i]);
            }
        }

        /// <summary>고른 줄은 밝게, 고르지 않은 줄은 어둡게. 앞의 표로도 구분한다.</summary>
        private void PaintClue(Button item, ClueEntry entry)
        {
            if (item == null || entry == null) return;

            bool on = _picked.Contains(entry.ClueId);

            var image = item.targetGraphic as Image;
            if (image != null) image.color = on ? PickedClueColor : IdleClueColor;

            var label = item.GetComponentInChildren<TMP_Text>(true);
            if (label == null) return;

            // 앞의 네모가 고른 것을 말한다. 뒤의 ◆ 는 규칙의 근거가 되는 단서라는 뜻이다.
            label.text = (on ? "■ " : "□ ") + (entry.IsKey ? "◆ " : string.Empty) + entry.Text;
            label.color = on ? TextColor : DimClueColor;
        }

        private static readonly Color PickedClueColor = new Color(0.22f, 0.28f, 0.24f, 1f);
        private static readonly Color IdleClueColor = new Color(0.13f, 0.14f, 0.19f, 1f);
        private static readonly Color TextColor = new Color(0.93f, 0.93f, 0.96f);
        private static readonly Color DimClueColor = new Color(0.70f, 0.71f, 0.76f);

        private const string ClueEmptyTextId = "ui.rule.no_clue";

        /// <summary>결론 칸. 답이 둘 다 비어 있으면 통째로 꺼 둔다.</summary>
        private void RefreshConclusion(LocalizationService loc)
        {
            string verdict = _verdictProvider != null ? _verdictProvider() : string.Empty;
            string counter = _counterProvider != null ? _counterProvider() : string.Empty;

            bool has = !string.IsNullOrEmpty(verdict) || !string.IsNullOrEmpty(counter);

            // 물음을 먼저 적고 그 아래에 답과 까닭을 붙인다.
            // 처음 보는 사람이 "무엇을 묻고 있고, 답이 무엇이고, 왜 그런지" 순서로 읽게 한다.
            if (_conclusionTitleText != null) _conclusionTitleText.text = loc.Get(ConclusionTitleTextId);
            if (_verdictText != null) _verdictText.text = Ask(loc.Get(VerdictLabelTextId), verdict);
            if (_counterText != null) _counterText.text = Ask(loc.Get(CounterLabelTextId), counter);

            if (_conclusionRoot != null) _conclusionRoot.SetActive(has);

            // 결론이 나오면 후보 목록도 단서 칸도 물러난다.
            // 고를 일은 이미 끝났고, 모은 단서는 아래 "그렇게 본 까닭" 이 그대로 인용하고 있다.
            // 자리를 통째로 내주어야 결론이 줄을 넘치지 않고 다 들어간다.
            if (_listRoot != null) _listRoot.gameObject.SetActive(!has);
            if (_listTitleText != null) _listTitleText.gameObject.SetActive(!has);
            // 단서 칸은 결론이 나와도 그대로 둔다. 무엇을 근거로 그렇게 봤는지가 옆에 서 있어야 한다.
            if (_clueCardRoot != null) _clueCardRoot.SetActive(true);
            if (has && _emptyText != null) _emptyText.gameObject.SetActive(false);
        }

        /// <summary>"물음 / 답 / 까닭" 한 덩어리로 묶는다. 물음만 눈에 띄게 둔다.</summary>
        private static string Ask(string question, string body)
        {
            if (string.IsNullOrEmpty(body)) return string.Empty;
            return "<color=#DDB86E>" + question + "</color>\n" + body;
        }

        private const string ConclusionTitleTextId = "ui.rule.conclusion_title";
        private const string VerdictLabelTextId = "ui.rule.verdict_label";
        private const string CounterLabelTextId = "ui.rule.counter_label";

        private void RebuildItems(LocalizationService loc)
        {
            if (_listRoot == null || _itemTemplate == null) return;

            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] == null) continue;

                // Destroy는 프레임 끝에 처리된다. 같은 프레임에 다시 그리면 옛 항목이 남아 두 번 보인다.
                var old = _spawned[i].gameObject;
                old.transform.SetParent(null, false);
                old.SetActive(false);
                Destroy(old);
            }
            _spawned.Clear();

            int count = 0;

            // 답을 고르는 중이면 규칙 후보 대신 그 답들을 늘어놓는다.
            if (_options != null)
            {
                for (int i = 0; i < _options.Count; i++)
                {
                    if (_options[i] == null) continue;

                    SpawnOption(_options[i], loc);
                    count++;
                }
            }
            else if (_rules != null)
            {
                for (int i = 0; i < _rules.Count; i++)
                {
                    var rule = _rules[i];
                    if (rule == null) continue;

                    SpawnItem(rule, loc);
                    count++;
                }
            }

            // 아직 아무것도 세울 수 없으면 그 사실을 적는다. 빈 자리만 두면 고장으로 보인다.
            if (_emptyText != null)
            {
                _emptyText.text = loc.Get(EmptyTextId);
                _emptyText.gameObject.SetActive(count == 0);
            }
        }

        /// <summary>답 후보 한 칸. 규칙 칸과 같은 틀을 쓰되 정답 여부는 모른다.</summary>
        private void SpawnOption(Option option, LocalizationService loc)
        {
            var item = Instantiate(_itemTemplate, _listRoot);
            item.gameObject.name = "Option_" + option.Id;
            item.gameObject.SetActive(true);

            var head = item.transform.Find(HeadName)?.GetComponent<TMP_Text>();
            if (head != null) head.text = option.Head;

            var note = item.transform.Find(NoteName)?.GetComponent<TMP_Text>();
            if (note != null)
            {
                note.text = option.Note ?? string.Empty;
                note.gameObject.SetActive(!string.IsNullOrEmpty(option.Note));
            }

            // 한 번 짚어 봤다가 아니었던 답에는 딱지를 붙인다. 같은 자리를 두 번 헤매지 않게 한다.
            var badge = item.transform.Find(BadgeName);
            if (badge != null)
            {
                badge.gameObject.SetActive(option.Tried);

                var badgeText = badge.GetComponentInChildren<TMP_Text>(true);
                if (badgeText != null) badgeText.text = loc.Get(TriedMarkTextId);
            }

            string captured = option.Id;
            item.onClick.RemoveAllListeners();
            item.onClick.AddListener(() => _onPickOption?.Invoke(captured));

            _spawned.Add(item);
        }

        private const string TriedMarkTextId = "ui.rule.tried_mark";

        private void SpawnItem(RuleSO rule, LocalizationService loc)
        {
            var item = Instantiate(_itemTemplate, _listRoot);
            item.gameObject.name = "Item_" + rule.RuleId;
            item.gameObject.SetActive(true);

            var head = item.transform.Find(HeadName)?.GetComponent<TMP_Text>();
            if (head != null && _headProvider != null) head.text = _headProvider(rule);

            var note = item.transform.Find(NoteName)?.GetComponent<TMP_Text>();
            if (note != null)
            {
                string text = _noteProvider != null ? _noteProvider(rule) : string.Empty;
                note.text = text;
                note.gameObject.SetActive(!string.IsNullOrEmpty(text));
            }

            var badge = item.transform.Find(BadgeName);
            if (badge != null)
            {
                bool deduced = _deducedProvider != null && _deducedProvider(rule);
                badge.gameObject.SetActive(deduced);

                var badgeText = badge.GetComponentInChildren<TMP_Text>(true);
                if (badgeText != null) badgeText.text = loc.Get(AlreadyMarkTextId);
            }

            var captured = rule;
            item.onClick.RemoveAllListeners();
            item.onClick.AddListener(() => _onSelect?.Invoke(captured));

            _spawned.Add(item);
        }
    }
}
