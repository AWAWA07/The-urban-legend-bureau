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

        [Tooltip("가리키는 세모. 지금 눌러야 하는 아이콘일 때만 뜬다. 비워 두면 아무 표시도 없다.")]
        public GameObject hint;

        [Tooltip("새로 올라온 것이 몇 개인지 적는 배지. 셀 것이 없으면 꺼진다.")]
        public GameObject badge;

        [Tooltip("배지 안의 숫자.")]
        public TMP_Text badgeCount;
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

        [Header("검색")]
        [Tooltip("작업 표시줄의 검색 칸. 적으면 이름이 맞는 앱이 위에 뜬다.")]
        [SerializeField] private TMP_InputField _searchInput;

        [Tooltip("검색 칸 위에 뜨는 결과 창. 적은 것이 없으면 숨는다.")]
        [SerializeField] private RectTransform _searchResults;

        [Tooltip("결과 한 줄. 복제해서 쓴다. 안에 Icon(Image)과 Label(TMP)이 있다.")]
        [SerializeField] private Button _searchItemTemplate;

        [Tooltip("맞는 것이 없을 때 띄우는 글.")]
        [SerializeField] private TMP_Text _searchEmpty;

        private readonly List<GameObject> _searchItems = new List<GameObject>();
        private const string SearchNoneTextId = "ui.desktop.search_none";

        [Header("켜지는 모습")]
        [Tooltip("컴퓨터를 켤 때 화면이 가운데의 가는 선에서 펴지며 커지는 시간(초).")]
        [SerializeField] private float _powerOnSeconds = 0.45f;

        private Coroutine _powerOn;

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
        /// 가리키는 세모를 세울 아이콘 하나를 정한다. 빈 값을 주면 아무 곳도 가리키지 않는다.
        ///
        /// 누를 수 있는 것과 가리키는 것은 다른 이야기다.
        /// 메모장은 언제든 열 수 있지만, 지금 눌러야 하는 곳은 괴담넷 하나다.
        /// </summary>
        public void SetHintApp(string appId)
        {
            _hintAppId = appId;
            ApplyInteractable();
        }

        private string _hintAppId;

        /// <summary>
        /// 아이콘에 새로 올라온 것이 몇 개인지 적는다.
        ///
        /// 무엇이 새것인지 세는 것은 밖의 일이다. 여기서는 받은 숫자를 적기만 한다.
        /// 0 을 주면 배지가 꺼진다. 읽고 나면 그렇게 지운다.
        /// </summary>
        public void SetAppBadge(string appId, int count)
        {
            foreach (var icon in _icons)
            {
                if (icon == null || icon.appId != appId) continue;

                if (icon.badgeCount != null) icon.badgeCount.text = count.ToString();
                if (icon.badge != null) icon.badge.SetActive(count > 0);
            }
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
                if (icon == null) continue;

                bool open = !_lockAll && (_allowAll || _allowed.Contains(icon.appId));
                if (icon.button != null) icon.button.interactable = open;

                // 가리키는 세모는 지목된 아이콘 하나에만 선다. 잠겨 있으면 아직 누를 때가 아니다.
                if (icon.hint != null) icon.hint.SetActive(open && icon.appId == _hintAppId);
            }
        }

        protected override void OnOpen()
        {
            EventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);
            Refresh();
            BindSearch();
            ClearSearch();
            PlayPowerOn();
        }

        protected override void OnClose()
        {
            EventBus.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);
            ClearSearch();
            if (_powerOn != null) { StopCoroutine(_powerOn); _powerOn = null; }
            transform.localScale = Vector3.one;

            // 한 번 알리고 비운다. 다음에 여는 쪽이 다시 건다.
            var closed = Closed;
            Closed = null;
            closed?.Invoke();
        }

        /// <summary>
        /// 컴퓨터 화면이 닫혔을 때 한 번 부른다. ESC 로 닫아도 불린다.
        /// 숙소에서 켠 컴퓨터를 끄고 방으로 돌아올 때 쓴다.
        /// </summary>
        public Action Closed { get; set; }

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

        // ------------------------------------------------------------- 켜지는 모습

        /// <summary>
        /// 모니터가 켜지는 모습. 가운데에 가는 빛줄기가 옆으로 퍼진 뒤 위아래로 펴지며 화면이 된다.
        /// 옛 모니터가 켜질 때처럼 짧게 지나간다.
        /// </summary>
        private void PlayPowerOn()
        {
            if (_powerOn != null) StopCoroutine(_powerOn);
            if (!isActiveAndEnabled || _powerOnSeconds <= 0f) { transform.localScale = Vector3.one; return; }
            _powerOn = StartCoroutine(PowerOn());
        }

        private System.Collections.IEnumerator PowerOn()
        {
            float wide = _powerOnSeconds * 0.35f;
            float tall = _powerOnSeconds - wide;
            const float Line = 0.006f;

            for (float t = 0f; t < wide; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / wide);
                transform.localScale = new Vector3(Mathf.Lerp(0.04f, 1f, k), Line, 1f);
                yield return null;
            }
            for (float t = 0f; t < tall; t += Time.unscaledDeltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / tall, 3f);   // 빠르게 펴지다 끝에서 멈춘다
                transform.localScale = new Vector3(1f, Mathf.Lerp(Line, 1f, k), 1f);
                yield return null;
            }
            transform.localScale = Vector3.one;
            _powerOn = null;
        }

        // ------------------------------------------------------------- 검색

        private bool _searchBound;

        private void BindSearch()
        {
            if (_searchBound || _searchInput == null) return;
            _searchBound = true;
            _searchInput.onValueChanged.AddListener(OnSearchChanged);
            _searchInput.onSubmit.AddListener(OnSearchSubmit);
        }

        private void ClearSearch()
        {
            if (_searchInput != null) _searchInput.SetTextWithoutNotify(string.Empty);
            ShowSearchResults(string.Empty);
        }

        private void OnSearchChanged(string query)
        {
            ShowSearchResults(query);
        }

        /// <summary>엔터를 치면 맨 위에 뜬 앱을 연다.</summary>
        private void OnSearchSubmit(string query)
        {
            var hits = FindApps(query);
            if (hits.Count > 0) OpenFromSearch(hits[0]);
        }

        /// <summary>이름이나 앱 ID 에 적은 글자가 들어간 아이콘. 대소문자와 띄어쓰기는 가리지 않는다.</summary>
        private List<DesktopIcon> FindApps(string query)
        {
            var hits = new List<DesktopIcon>();
            string q = Normalize(query);
            if (q.Length == 0) return hits;

            foreach (var icon in _icons)
            {
                if (icon == null) continue;
                string label = icon.label != null ? Normalize(icon.label.text) : string.Empty;
                if (label.Contains(q) || Normalize(icon.appId).Contains(q)) hits.Add(icon);
            }
            return hits;
        }

        private static string Normalize(string s)
        {
            return string.IsNullOrEmpty(s) ? string.Empty : s.Replace(" ", string.Empty).ToLowerInvariant();
        }

        private void ShowSearchResults(string query)
        {
            foreach (var item in _searchItems)
            {
                if (item == null) continue;
                item.transform.SetParent(null, false);
                Destroy(item);
            }
            _searchItems.Clear();

            if (_searchResults == null) return;

            bool empty = string.IsNullOrWhiteSpace(query);
            _searchResults.gameObject.SetActive(!empty);
            if (empty) return;

            var hits = FindApps(query);
            foreach (var icon in hits)
            {
                var item = Instantiate(_searchItemTemplate, _searchResults);
                item.gameObject.name = "Result_" + icon.appId;
                item.gameObject.SetActive(true);

                var label = item.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = icon.label != null ? icon.label.text : icon.appId;

                // 결과 줄의 그림은 바탕화면 아이콘의 그림을 그대로 쓴다.
                var iconImage = item.transform.Find("Icon")?.GetComponent<Image>();
                var source = icon.button != null ? icon.button.transform.Find("Box")?.GetComponent<Image>() : null;
                if (iconImage != null && source != null) iconImage.sprite = source.sprite;

                // 지금 열 수 없는 앱은 보이기만 한다. 아이콘을 누를 수 없을 때와 같다.
                item.interactable = icon.button == null || icon.button.interactable;
                var captured = icon;
                item.onClick.AddListener(() => OpenFromSearch(captured));

                _searchItems.Add(item.gameObject);
            }

            if (_searchEmpty != null)
            {
                _searchEmpty.gameObject.SetActive(hits.Count == 0);
                _searchEmpty.transform.SetAsLastSibling();
                if (hits.Count == 0 && ServiceRegistry.TryGet<LocalizationService>(out var loc))
                    _searchEmpty.text = loc.Get(SearchNoneTextId, query.Trim());
            }
        }

        private void OpenFromSearch(DesktopIcon icon)
        {
            if (icon == null) return;
            if (icon.button != null && !icon.button.interactable) return;

            ClearSearch();
            _onOpen?.Invoke(icon.appId);
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
