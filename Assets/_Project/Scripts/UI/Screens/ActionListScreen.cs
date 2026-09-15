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
    /// 조사 행동 선택 화면.
    /// 고를 수 있는 행동을 버튼으로 나열하고, 바로 아래에 직전 결과를 보여준다.
    ///
    /// InternetListScreen / CaseListScreen 과 같은 방식이다.
    /// 항목은 씬에 둔 템플릿을 복제해 만들기 때문에 행동이 늘어도 씬을 다시 만들지 않는다.
    ///
    /// 판정은 하지 않는다. 무엇을 보여줄지는 전부 CaseDirector가 넘겨준다.
    /// </summary>
    public class ActionListScreen : UIScreen
    {
        [Header("표시 대상")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _footerText;

        [Tooltip("사건 시간 / 확산도 표시줄.")]
        [SerializeField] private TMP_Text _statsText;

        [Tooltip("직전 조사 결과. 화면을 다시 열면 지워진다.")]
        [SerializeField] private TMP_Text _resultText;

        [Header("목록")]
        [SerializeField] private RectTransform _listRoot;
        [SerializeField] private Button _itemTemplate;

        private readonly List<Button> _spawned = new List<Button>();

        private string _titleId;
        private string _footerId;
        private IReadOnlyList<InvestigationActionSO> _actions;
        private Func<InvestigationActionSO, string> _labelProvider;
        private Action<InvestigationActionSO> _onSelect;
        private Func<string> _statsProvider;
        private Func<string> _resultProvider;

        /// <summary>목록을 채운다. 결과 문구는 화면을 새로 열 때 지워진다.</summary>
        public void Bind(string titleTextId, string footerTextId,
            IReadOnlyList<InvestigationActionSO> actions,
            Func<InvestigationActionSO, string> labelProvider,
            Action<InvestigationActionSO> onSelect,
            Func<string> statsProvider = null)
        {
            _titleId = titleTextId;
            _footerId = footerTextId;
            _actions = actions;
            _labelProvider = labelProvider;
            _onSelect = onSelect;
            _statsProvider = statsProvider;
            _resultProvider = null;
            Refresh();
        }

        /// <summary>
        /// 직전 조사 결과를 알린다.
        /// 완성된 문자열이 아니라 만드는 방법을 받아 두어야 언어가 바뀔 때 다시 조립된다.
        /// </summary>
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
            if (_statsText != null) _statsText.text = _statsProvider != null ? _statsProvider() : string.Empty;
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
                // 목록에서 떼어 내고 꺼 둔 다음 파괴한다.
                var old = _spawned[i].gameObject;
                old.transform.SetParent(null, false);
                old.SetActive(false);
                Destroy(old);
            }
            _spawned.Clear();

            if (_actions == null) return;

            for (int i = 0; i < _actions.Count; i++)
            {
                var action = _actions[i];
                if (action == null) continue;

                var item = Instantiate(_itemTemplate, _listRoot);
                item.gameObject.name = "Item_" + action.ActionId;
                item.gameObject.SetActive(true);

                var label = item.GetComponentInChildren<TMP_Text>(true);
                if (label != null && _labelProvider != null) label.text = _labelProvider(action);

                var captured = action;
                item.onClick.RemoveAllListeners();
                item.onClick.AddListener(() => _onSelect?.Invoke(captured));

                _spawned.Add(item);
            }
        }
    }
}
