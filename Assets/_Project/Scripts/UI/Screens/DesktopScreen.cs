using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.UI
{
    /// <summary>바탕화면 아이콘 하나. 프로그램이 늘면 항목을 추가한다.</summary>
    [Serializable]
    public class DesktopIcon
    {
        [Tooltip("프로그램 구분용 ID. 예: gwedamnet")]
        public string appId;

        [Tooltip("아이콘 이름의 Localization String ID.")]
        public string labelTextId;

        public Button button;
        public TMP_Text label;
    }

    /// <summary>
    /// 차지한의 컴퓨터 바탕화면.
    ///
    /// 지금 실제로 열리는 것은 괴담넷뿐이다. 나머지는 자리만 잡아 둔 아이콘이다.
    /// 무엇을 누를 수 있는지는 밖에서 정한다. 튜토리얼이 한 곳만 누르게 막을 수 있어야 하기 때문이다.
    /// </summary>
    public class DesktopScreen : UIScreen
    {
        [Header("표시")]
        [SerializeField] private TMP_Text _clockText;

        [Tooltip("작업 표시줄 왼쪽의 전체 믿음도.")]
        [SerializeField] private TMP_Text _beliefText;

        [Tooltip("컴퓨터를 켠 시각. 여기서부터 실제 시간만큼 흘러간다.")]
        [SerializeField] private int _startHour = 22;
        [SerializeField] private int _startMinute = 30;

        [Header("아이콘")]
        [SerializeField] private List<DesktopIcon> _icons = new List<DesktopIcon>();

        private Action<string> _onOpen;
        private readonly HashSet<string> _allowed = new HashSet<string>();
        private bool _allowAll = true;
        private bool _lockAll;

        private const string BeliefTextId = "ui.desktop.belief";

        /// <summary>화면에 띄울 전체 믿음도. 판정은 BeliefService 가 하고 여기서는 받기만 한다.</summary>
        private int _belief;

        private int _shownMinute = int.MinValue;

        /// <summary>작업 표시줄에 띄울 전체 믿음도를 알려 준다.</summary>
        public void SetBelief(int percent)
        {
            _belief = percent;
            RefreshBelief();
        }

        protected override void Awake()
        {
            // 시계는 한 곳에서만 센다. 휴대폰도 같은 시각을 보여야 하기 때문이다.
            base.Awake();
            GameClock.Configure(_startHour, _startMinute);
        }

        private void Update()
        {
            // 분이 바뀔 때만 다시 쓴다. 매 프레임 글자를 만들 이유가 없다.
            int minute = GameClock.ElapsedMinutes;
            if (minute == _shownMinute) return;

            _shownMinute = minute;
            RefreshClock();
        }

        /// <summary>아이콘을 눌렀을 때 부를 것을 지정한다.</summary>
        public void Bind(Action<string> onOpen)
        {
            _onOpen = onOpen;

            foreach (var icon in _icons)
            {
                if (icon == null || icon.button == null) continue;

                var captured = icon.appId;
                icon.button.onClick.RemoveAllListeners();
                icon.button.onClick.AddListener(() => _onOpen?.Invoke(captured));
            }

            Refresh();
        }

        /// <summary>
        /// 지금 누를 수 있는 아이콘을 정한다.
        /// 아무것도 주지 않으면 전부 누를 수 있다. 튜토리얼이 한 곳만 열어 둘 때 쓴다.
        /// </summary>
        public void SetAllowedApps(params string[] appIds)
        {
            _allowed.Clear();
            _allowAll = appIds == null || appIds.Length == 0;
            _lockAll = false;

            if (!_allowAll)
            {
                foreach (var id in appIds) _allowed.Add(id);
            }

            ApplyInteractable();
        }

        /// <summary>
        /// 아무것도 누를 수 없게 한다. 한영이 말하는 동안에 쓴다.
        /// 말이 끝나기 전에 아이콘을 눌러 버리면 안내를 건너뛰게 된다.
        /// </summary>
        public void LockAllApps()
        {
            _lockAll = true;
            ApplyInteractable();
        }

        private void ApplyInteractable()
        {
            foreach (var icon in _icons)
            {
                if (icon == null || icon.button == null) continue;
                icon.button.interactable = !_lockAll && (_allowAll || _allowed.Contains(icon.appId));
            }
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

            RefreshClock();
            RefreshBelief();

            foreach (var icon in _icons)
            {
                if (icon == null || icon.label == null) continue;
                icon.label.text = loc.Get(icon.labelTextId);
            }

            ApplyInteractable();
        }

        /// <summary>
        /// 작업 표시줄의 시계. 컴퓨터를 켠 시각에서 실제로 흐른 만큼을 더해 보여준다.
        /// 게임 안의 조사 시간(InvestigationTimeService)과는 다른 것이다. 저쪽은 행동 수로 간다.
        /// </summary>
        private void RefreshClock()
        {
            if (_clockText == null) return;
            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc)) return;

            _clockText.text = GameClock.Format(loc);
        }

        private void RefreshBelief()
        {
            if (_beliefText == null) return;
            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc)) return;

            _beliefText.text = loc.Get(BeliefTextId, _belief);
        }
    }
}
