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
    /// 확보한 단서를 보여주고, 그 단서로 세울 수 있는 규칙 후보를 버튼으로 나열한다.
    ///
    /// ActionListScreen 과 같은 방식이다. 항목은 씬의 템플릿을 복제해 만든다.
    ///
    /// 판정은 하지 않는다. 어떤 후보를 보여줄지도, 고른 결과가 무엇인지도 CaseDirector가 정한다.
    /// 화면은 정답 여부를 알지 못한다.
    /// </summary>
    public class RuleListScreen : UIScreen
    {
        [Header("표시 대상")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _footerText;

        [Tooltip("확보한 단서 목록.")]
        [SerializeField] private TMP_Text _clueText;

        [Tooltip("직전 추론 결과. 화면을 새로 열면 지워진다.")]
        [SerializeField] private TMP_Text _resultText;

        [Header("목록")]
        [SerializeField] private RectTransform _listRoot;
        [SerializeField] private Button _itemTemplate;

        private readonly List<Button> _spawned = new List<Button>();

        private string _titleId;
        private string _footerId;
        private IReadOnlyList<RuleSO> _rules;
        private Func<RuleSO, string> _labelProvider;
        private Action<RuleSO> _onSelect;
        private Func<string> _clueProvider;
        private Func<string> _resultProvider;

        /// <summary>후보 목록을 채운다. 결과 문구는 화면을 새로 열 때 지워진다.</summary>
        public void Bind(string titleTextId, string footerTextId,
            IReadOnlyList<RuleSO> rules,
            Func<RuleSO, string> labelProvider,
            Action<RuleSO> onSelect,
            Func<string> clueProvider = null)
        {
            _titleId = titleTextId;
            _footerId = footerTextId;
            _rules = rules;
            _labelProvider = labelProvider;
            _onSelect = onSelect;
            _clueProvider = clueProvider;
            _resultProvider = null;
            Refresh();
        }

        /// <summary>직전 추론 결과를 알린다. 언어가 바뀌면 다시 조립되도록 만드는 방법을 받는다.</summary>
        public void ShowResult(Func<string> provider)
        {
            _resultProvider = provider;
            Refresh();
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

        private void OnLanguageChanged(LanguageChangedEvent evt)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc)) return;

            if (_titleText != null) _titleText.text = string.IsNullOrEmpty(_titleId) ? string.Empty : loc.Get(_titleId);
            if (_footerText != null) _footerText.text = string.IsNullOrEmpty(_footerId) ? string.Empty : loc.Get(_footerId);
            if (_clueText != null) _clueText.text = _clueProvider != null ? _clueProvider() : string.Empty;
            if (_resultText != null) _resultText.text = _resultProvider != null ? _resultProvider() : string.Empty;

            RebuildItems();
        }

        private void RebuildItems()
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

            if (_rules == null) return;

            for (int i = 0; i < _rules.Count; i++)
            {
                var rule = _rules[i];
                if (rule == null) continue;

                var item = Instantiate(_itemTemplate, _listRoot);
                item.gameObject.name = "Item_" + rule.RuleId;
                item.gameObject.SetActive(true);

                var label = item.GetComponentInChildren<TMP_Text>(true);
                if (label != null && _labelProvider != null) label.text = _labelProvider(rule);

                var captured = rule;
                item.onClick.RemoveAllListeners();
                item.onClick.AddListener(() => _onSelect?.Invoke(captured));

                _spawned.Add(item);
            }
        }
    }
}
