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
    /// 사건 목록 화면.
    /// 선택 가능한 사건을 버튼으로 나열한다.
    ///
    /// InternetListScreen 과 같은 방식(템플릿 복제)이다.
    /// 두 화면을 하나로 합치는 일반화는 하지 않았다.
    /// 지금 이득보다 기존 인터넷 화면을 건드리는 위험이 크다.
    /// </summary>
    public class CaseListScreen : UIScreen
    {
        [Header("표시 대상")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _footerText;

        [Header("목록")]
        [Tooltip("항목 버튼이 들어갈 부모.")]
        [SerializeField] private RectTransform _listRoot;

        [Tooltip("복제할 항목 버튼. 비활성 상태로 씬에 둔다.")]
        [SerializeField] private Button _itemTemplate;

        private readonly List<Button> _spawned = new List<Button>();

        private string _titleId;
        private string _footerId;
        private IReadOnlyList<CaseSO> _cases;
        private Func<CaseSO, string> _labelProvider;
        private Action<CaseSO> _onSelect;

        /// <summary>목록을 채운다. labelProvider가 항목 문구를, onSelect가 선택 처리를 맡는다.</summary>
        public void Bind(string titleTextId, string footerTextId,
            IReadOnlyList<CaseSO> cases,
            Func<CaseSO, string> labelProvider,
            Action<CaseSO> onSelect)
        {
            _titleId = titleTextId;
            _footerId = footerTextId;
            _cases = cases;
            _labelProvider = labelProvider;
            _onSelect = onSelect;
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

            RebuildItems();
        }

        private void RebuildItems()
        {
            if (_listRoot == null || _itemTemplate == null) return;

            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null) Destroy(_spawned[i].gameObject);
            }
            _spawned.Clear();

            if (_cases == null) return;

            for (int i = 0; i < _cases.Count; i++)
            {
                var caseData = _cases[i];
                if (caseData == null) continue;

                var item = Instantiate(_itemTemplate, _listRoot);
                item.gameObject.name = "Item_" + caseData.CaseId;
                item.gameObject.SetActive(true);

                var label = item.GetComponentInChildren<TMP_Text>(true);
                if (label != null && _labelProvider != null) label.text = _labelProvider(caseData);

                var captured = caseData;
                item.onClick.RemoveAllListeners();
                item.onClick.AddListener(() => _onSelect?.Invoke(captured));

                _spawned.Add(item);
            }
        }
    }
}
