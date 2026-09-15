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
    /// 인터넷 조사 목록 화면.
    /// 등록된 게시글을 버튼으로 나열하고, 각 항목에 검열 상태를 함께 보여준다.
    ///
    /// 항목 버튼은 씬에 넣어 둔 템플릿을 복제해 만든다.
    /// 페이지 수가 바뀌어도 씬을 다시 만들 필요가 없다.
    /// </summary>
    public class InternetListScreen : UIScreen
    {
        [Header("표시 대상")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _footerText;

        [Tooltip("확산도 / 믿음도 표시줄.")]
        [SerializeField] private TMP_Text _statsText;

        [Header("목록")]
        [Tooltip("항목 버튼이 들어갈 부모.")]
        [SerializeField] private RectTransform _listRoot;

        [Tooltip("복제할 항목 버튼. 비활성 상태로 씬에 둔다.")]
        [SerializeField] private Button _itemTemplate;

        private readonly List<Button> _spawned = new List<Button>();

        private string _titleId;
        private string _footerId;
        private IReadOnlyList<WebPageSO> _pages;
        private Func<WebPageSO, string> _labelProvider;
        private Action<WebPageSO> _onSelect;
        private Func<string> _statsProvider;

        /// <summary>목록을 채운다. labelProvider가 항목 문구를, onSelect가 선택 처리를 맡는다.</summary>
        public void Bind(string titleTextId, string footerTextId,
            IReadOnlyList<WebPageSO> pages,
            Func<WebPageSO, string> labelProvider,
            Action<WebPageSO> onSelect,
            Func<string> statsProvider = null)
        {
            _statsProvider = statsProvider;
            _titleId = titleTextId;
            _footerId = footerTextId;
            _pages = pages;
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

        /// <summary>검열 상태가 바뀐 뒤 목록 문구를 다시 그린다.</summary>
        public void Refresh()
        {
            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc)) return;

            if (_titleText != null) _titleText.text = string.IsNullOrEmpty(_titleId) ? string.Empty : loc.Get(_titleId);
            if (_footerText != null) _footerText.text = string.IsNullOrEmpty(_footerId) ? string.Empty : loc.Get(_footerId);
            if (_statsText != null) _statsText.text = _statsProvider != null ? _statsProvider() : string.Empty;

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

            if (_pages == null) return;

            for (int i = 0; i < _pages.Count; i++)
            {
                var page = _pages[i];
                if (page == null) continue;

                var item = Instantiate(_itemTemplate, _listRoot);
                item.gameObject.name = "Item_" + page.PageId;
                item.gameObject.SetActive(true);

                var label = item.GetComponentInChildren<TMP_Text>(true);
                if (label != null && _labelProvider != null) label.text = _labelProvider(page);

                var captured = page;
                item.onClick.RemoveAllListeners();
                item.onClick.AddListener(() => _onSelect?.Invoke(captured));

                _spawned.Add(item);
            }
        }
    }
}
