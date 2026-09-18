using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Localization;
using UrbanLegendBureau.Save;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 메모장.
    ///
    /// 컴퓨터로 열면 바탕화면 위에 뜬 창이고, 휴대폰으로 열면 손 안의 앱이다.
    /// 화면은 하나뿐이다. 괴담넷과 같은 방식으로 창 크기와 글자 크기만 바꿔 쓴다.
    /// 어느 쪽으로 열든 적어 둔 것은 같다. 현장에서 적고 컴퓨터에서 읽을 수 있어야 하기 때문이다.
    ///
    /// 메모는 여러 장을 둘 수 있다. 윗줄의 탭이 그 장들이고, + 를 누르면 한 장이 늘어난다.
    /// 적어 둔 글은 저장본(SaveData.memos)이 들고 있다. 이 화면이 직접 읽고 쓴다.
    /// 시계(ClockLabel)나 믿음도(BeliefLabel)와 같은 자리의 물건이다.
    /// </summary>
    public class MemoScreen : UIScreen
    {
        [Header("창")]
        [Tooltip("컴퓨터 창이자 휴대폰 화면. 이 사각형만 모양을 바꾼다.")]
        [SerializeField] private RectTransform _window;

        [SerializeField] private TMP_Text _titleText;

        [Tooltip("적는 자리.")]
        [SerializeField] private TMP_InputField _input;

        [Tooltip("창 아래에 적는 한 줄. 저절로 저장된다는 것을 알린다.")]
        [SerializeField] private TMP_Text _footerText;

        [SerializeField] private Button _closeButton;

        [Header("메모 장")]
        [Tooltip("탭을 늘어놓는 자리.")]
        [SerializeField] private RectTransform _tabRoot;

        [Tooltip("탭이 줄을 넘칠 때 끌어서 넘기는 칸. 막대는 없다.")]
        [SerializeField] private ScrollRect _tabScroll;

        [Tooltip("복제할 탭 한 칸.")]
        [SerializeField] private Button _tabTemplate;

        [Tooltip("메모를 한 장 더 만드는 단추.")]
        [SerializeField] private Button _newButton;

        [Tooltip("메모를 몇 장까지 둘 수 있는가.")]
        [SerializeField] private int _maxNotes = 6;

        [Header("모양")]
        [Tooltip("휴대폰 껍데기 안에 맞출 때 기준이 되는 폭. 이 폭으로 짜 두고 통째로 줄인다.")]
        [SerializeField] private float _phoneFitWidth = 380f;

        [Tooltip("휴대폰으로 크게 열 때의 폭. 껍데기 없이 화면 가운데에 세울 때 쓴다.")]
        [SerializeField] private float _phoneWidth = 620f;

        [SerializeField] private float _phoneMargin = 28f;

        [Tooltip("컴퓨터 창의 크기. 바탕화면 가운데에 이만큼으로 뜬다.")]
        [SerializeField] private Vector2 _deskSize = new Vector2(1180f, 760f);

        [Header("글자")]
        [SerializeField] private float _deskFontSize = 30f;
        [SerializeField] private float _deskTitleSize = 28f;
        [SerializeField] private float _phoneFontSize = 15f;
        [SerializeField] private float _phoneTitleSize = 14f;

        private const string TitleTextId = "ui.desktop.app_memo";
        private const string PlaceholderTextId = "ui.memo.placeholder";
        private const string FooterTextId = "ui.memo.autosave";
        private const string NoteNameTextId = "ui.memo.note";
        private const string NewNoteTextId = "ui.memo.new";

        /// <summary>탭 이름에 쓸 첫 줄의 길이. 이보다 길면 잘라 쓴다.</summary>
        private const int TabTitleLength = 8;

        private Action _onClose;
        private readonly List<Button> _tabs = new List<Button>();

        /// <summary>지금 펼쳐 놓은 메모의 번호.</summary>
        private int _index;

        /// <summary>휴대폰으로 보고 있는가. 글자 크기가 이 값을 따른다.</summary>
        public bool IsPhone { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(() => _onClose?.Invoke());
            }

            if (_newButton != null)
            {
                _newButton.onClick.RemoveAllListeners();
                _newButton.onClick.AddListener(AddNote);
            }

            if (_input != null)
            {
                _input.onValueChanged.RemoveAllListeners();
                _input.onValueChanged.AddListener(OnTyped);
            }
        }

        /// <summary>닫기를 눌렀을 때 부를 것을 정한다. 여는 쪽이 제 화면 스택을 안다.</summary>
        public void Bind(Action onClose)
        {
            _onClose = onClose;
        }

        // ------------------------------------------------------------- 열렸는가

        /// <summary>
        /// 메모장을 쓸 수 있게 됐다는 표시. 기존 storyFlags 에 둔다.
        /// 한영이 메모하라고 말한 뒤부터 열린다. 그 전에는 아이콘이 있어도 열리지 않는다.
        /// </summary>
        public const string UnlockedFlag = "memo_unlocked";

        public static bool IsUnlocked
        {
            get
            {
                var data = GetSave()?.Current;
                return data != null && data.storyFlags != null && data.storyFlags.Contains(UnlockedFlag);
            }
        }

        /// <summary>메모장을 연다. 이미 열려 있으면 아무 일도 없다. 열렸으면 true.</summary>
        public static bool Unlock()
        {
            var save = GetSave();
            var data = save?.Current;
            if (data == null) return false;

            data.storyFlags ??= new List<string>();
            if (data.storyFlags.Contains(UnlockedFlag)) return false;

            data.storyFlags.Add(UnlockedFlag);
            save.MarkDirty();
            return true;
        }

        // ------------------------------------------------------------- 메모 장

        private static SaveService GetSave()
        {
            return ServiceRegistry.TryGet<SaveService>(out var save) ? save : null;
        }

        /// <summary>적어 둔 메모들. 한 장도 없으면 빈 것을 한 장 만들어 준다.</summary>
        private List<string> Notes()
        {
            var data = GetSave()?.Current;
            if (data == null) return null;

            data.memos ??= new List<string>();
            if (data.memos.Count == 0) data.memos.Add(string.Empty);
            return data.memos;
        }

        /// <summary>메모를 한 장 더 만든다. 정해 둔 장수를 넘기면 아무 일도 없다.</summary>
        private void AddNote()
        {
            var notes = Notes();
            if (notes == null || notes.Count >= _maxNotes) return;

            notes.Add(string.Empty);
            GetSave()?.MarkDirty();

            _index = notes.Count - 1;
            ShowNote();
            RebuildTabs();
            ScrollToCurrentTab();

            Debug.Log("[MemoScreen] 메모를 한 장 더 만들었다 | " + notes.Count + "/" + _maxNotes);
        }

        private void SelectNote(int index)
        {
            var notes = Notes();
            if (notes == null || index < 0 || index >= notes.Count || index == _index) return;

            _index = index;
            ShowNote();
            RefreshTabLooks();
            ScrollToCurrentTab();
        }

        /// <summary>지금 고른 메모를 적는 자리에 펼친다.</summary>
        private void ShowNote()
        {
            var notes = Notes();
            if (notes == null || _input == null) return;

            _index = Mathf.Clamp(_index, 0, notes.Count - 1);

            // 값을 넣는 동안에는 저장 쪽으로 되돌아가지 않게 알림을 끈다.
            _input.SetTextWithoutNotify(notes[_index] ?? string.Empty);
            _input.caretPosition = _input.text.Length;
        }

        /// <summary>
        /// 한 글자 칠 때마다 저장본에 옮겨 적는다.
        ///
        /// 파일로 쓰는 것은 닫을 때 한 번뿐이다. 여기서는 표시만 해 둔다.
        /// 실제 메모장도 치는 동안 디스크를 긁지 않는다.
        /// </summary>
        private void OnTyped(string text)
        {
            var notes = Notes();
            if (notes == null || _index < 0 || _index >= notes.Count) return;
            if (notes[_index] == text) return;

            notes[_index] = text;
            GetSave()?.MarkDirty();

            // 탭 이름은 첫 줄에서 따온다. 지금 고친 그 탭만 다시 적는다.
            if (_index < _tabs.Count) SetTabLabel(_tabs[_index], _index, text);
        }

        /// <summary>탭을 처음부터 다시 늘어놓는다. 장수가 바뀌었을 때만 부른다.</summary>
        private void RebuildTabs()
        {
            if (_tabRoot == null || _tabTemplate == null) return;

            for (int i = 0; i < _tabs.Count; i++)
            {
                if (_tabs[i] != null) Destroy(_tabs[i].gameObject);
            }
            _tabs.Clear();

            var notes = Notes();
            if (notes == null) return;

            for (int i = 0; i < notes.Count; i++)
            {
                var tab = Instantiate(_tabTemplate, _tabRoot);
                tab.gameObject.name = "Tab_" + i;
                tab.gameObject.SetActive(true);

                SetTabLabel(tab, i, notes[i]);

                int picked = i;
                tab.onClick.RemoveAllListeners();
                tab.onClick.AddListener(() => SelectNote(picked));

                _tabs.Add(tab);
            }

            // 더 만들 수 없으면 + 를 잠근다. 눌러도 안 되는 것을 눌러 보게 두지 않는다.
            if (_newButton != null) _newButton.interactable = notes.Count < _maxNotes;

            RefreshTabLooks();
        }

        /// <summary>
        /// 탭 이름. 적어 둔 첫 줄을 쓰고, 빈 장이면 번호로 부른다.
        /// 실제 메모장도 저장한 이름이 없으면 제목 없음으로 부른다.
        /// </summary>
        private void SetTabLabel(Button tab, int index, string text)
        {
            if (tab == null) return;
            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc)) return;

            var label = tab.GetComponentInChildren<TMP_Text>(true);
            if (label == null) return;

            label.text = BuildTabTitle(loc, index, text);
            label.fontSize = IsPhone ? _phoneTitleSize - 2f : _deskTitleSize - 6f;
        }

        private string BuildTabTitle(LocalizationService loc, int index, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return loc.Get(NoteNameTextId, index + 1);

            int end = text.IndexOf('\n');
            string head = (end >= 0 ? text.Substring(0, end) : text).Trim();

            if (string.IsNullOrEmpty(head)) return loc.Get(NoteNameTextId, index + 1);
            return head.Length <= TabTitleLength ? head : head.Substring(0, TabTitleLength) + "…";
        }

        /// <summary>
        /// 지금 고른 탭이 보이는 자리로 줄을 민다.
        ///
        /// 좁은 화면에서는 탭이 줄을 넘는다. 새로 만든 장이 줄 끝에 생기므로,
        /// 밀어 주지 않으면 만들어 놓고도 그 탭이 화면 밖에 있어 보이지 않는다.
        /// 칸의 너비는 배치가 한 번 돌아야 정해지므로 그 뒤에 민다.
        /// </summary>
        private void ScrollToCurrentTab()
        {
            if (_tabScroll == null || _tabRoot == null) return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(_tabRoot);

            var view = _tabScroll.viewport;
            if (view == null) return;

            float over = _tabRoot.rect.width - view.rect.width;
            if (over <= 0f)
            {
                // 다 들어간다. 밀 것이 없다.
                _tabScroll.horizontalNormalizedPosition = 0f;
                return;
            }

            if (_index >= _tabs.Count || _tabs[_index] == null) return;

            var tab = (RectTransform)_tabs[_index].transform;

            // 고른 탭이 칸 가운데에 오도록 민다.
            float center = tab.anchoredPosition.x + tab.rect.width * 0.5f;
            float wanted = center - view.rect.width * 0.5f;

            _tabScroll.horizontalNormalizedPosition = Mathf.Clamp01(wanted / over);
        }

        /// <summary>펼쳐 놓은 탭만 밝게 둔다. 어느 장을 보고 있는지 한눈에 보여야 한다.</summary>
        private void RefreshTabLooks()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (_tabs[i] == null) continue;

                var image = _tabs[i].targetGraphic as Image;
                if (image != null) image.color = i == _index ? ActiveTabColor : IdleTabColor;
            }
        }

        private static readonly Color ActiveTabColor = new Color(0.97f, 0.96f, 0.93f, 1f);
        private static readonly Color IdleTabColor = new Color(0.78f, 0.77f, 0.73f, 1f);

        // ------------------------------------------------------------- 화면

        protected override void OnOpen()
        {
            EventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);

            ShowNote();
            RebuildTabs();
            Refresh();
            ScrollToCurrentTab();
        }

        protected override void OnClose()
        {
            EventBus.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);

            // 여기서 한 번 파일로 쓴다. 닫는 것이 곧 저장이다.
            GetSave()?.AutoSave();
        }

        private void OnLanguageChanged(LanguageChangedEvent evt)
        {
            Refresh();
            RebuildTabs();
        }

        public void Refresh()
        {
            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc)) return;

            if (_titleText != null) _titleText.text = loc.Get(TitleTextId);
            if (_footerText != null) _footerText.text = loc.Get(FooterTextId);

            if (_newButton != null)
            {
                var label = _newButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = loc.Get(NewNoteTextId);
            }

            if (_input != null && _input.placeholder is TMP_Text placeholder)
            {
                placeholder.text = loc.Get(PlaceholderTextId);
            }

            ApplyFontSizes();
        }

        /// <summary>
        /// 컴퓨터 창과 휴대폰 화면을 오간다.
        ///
        /// 괴담넷(CommunityPageScreen)과 같은 방식이다. 내용은 그대로 두고 창과 글자만 바꾼다.
        /// frame 을 주면 이미 그려져 있는 휴대폰 껍데기 안쪽에 맞춘다.
        /// </summary>
        public void SetShape(bool phone, RectTransform frame = null)
        {
            IsPhone = phone;

            if (_window != null)
            {
                if (phone && frame != null && FitTo(frame))
                {
                    // FitTo 가 자리를 다 잡았다.
                }
                else if (phone)
                {
                    _window.localScale = Vector3.one;
                    _window.anchorMin = new Vector2(0.5f, 0f);
                    _window.anchorMax = new Vector2(0.5f, 1f);
                    _window.pivot = new Vector2(0.5f, 0.5f);
                    _window.anchoredPosition = Vector2.zero;
                    _window.sizeDelta = new Vector2(_phoneWidth, -_phoneMargin * 2f);
                }
                else
                {
                    // 바탕화면 위에 뜬 창. 화면을 다 덮지 않고 가운데에 이만큼만 뜬다.
                    // 크기가 정해져 있어야 제목 줄을 잡고 끌어 옮길 수 있다.
                    _window.localScale = Vector3.one;
                    _window.anchorMin = new Vector2(0.5f, 0.5f);
                    _window.anchorMax = new Vector2(0.5f, 0.5f);
                    _window.pivot = new Vector2(0.5f, 0.5f);
                    _window.anchoredPosition = Vector2.zero;
                    _window.sizeDelta = _deskSize;
                }
            }

            // 휴대폰 껍데기 안에서는 창을 끌어 옮길 수 없다. 옮길 자리가 없다.
            var drag = _window != null ? _window.GetComponentInChildren<WindowDrag>(true) : null;
            if (drag != null) drag.enabled = !phone;

            ApplyFontSizes();
            RefreshTabLooks();
        }

        /// <summary>
        /// 휴대폰 껍데기 안쪽 칸에 창을 맞춘다.
        ///
        /// 칸에 맞춰 잘게 다시 짜지 않는다. 늘 같은 폭으로 짜 놓고 통째로 줄인다.
        /// 그래야 여백과 줄 높이가 한꺼번에 같은 비율로 줄어 배치가 무너지지 않는다.
        /// </summary>
        private bool FitTo(RectTransform frame)
        {
            var parent = _window != null ? _window.parent as RectTransform : null;
            if (parent == null || frame == null) return false;

            // 꺼져 있는 동안에는 캔버스 배율이 아직 실리지 않아 자리가 엉뚱하게 나온다.
            if (!parent.gameObject.activeInHierarchy || !frame.gameObject.activeInHierarchy) return false;

            var corners = new Vector3[4];
            frame.GetWorldCorners(corners);

            var min = parent.InverseTransformPoint(corners[0]);
            var max = parent.InverseTransformPoint(corners[2]);

            float width = max.x - min.x;
            float height = max.y - min.y;
            if (width <= 1f || height <= 1f) return false;

            float scale = width / _phoneFitWidth;

            _window.anchorMin = new Vector2(0.5f, 0.5f);
            _window.anchorMax = new Vector2(0.5f, 0.5f);
            _window.pivot = new Vector2(0.5f, 0.5f);
            _window.localScale = new Vector3(scale, scale, 1f);
            _window.sizeDelta = new Vector2(_phoneFitWidth, height / scale);
            _window.anchoredPosition = new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
            return true;
        }

        /// <summary>창이 좁아진 만큼 글자도 줄인다. 큰 화면의 글자를 그대로 두면 열 자도 안 들어간다.</summary>
        private void ApplyFontSizes()
        {
            float body = IsPhone ? _phoneFontSize : _deskFontSize;
            float title = IsPhone ? _phoneTitleSize : _deskTitleSize;

            if (_input != null)
            {
                _input.pointSize = body;
                if (_input.textComponent != null) _input.textComponent.fontSize = body;
                if (_input.placeholder is TMP_Text placeholder) placeholder.fontSize = body;
            }

            if (_titleText != null) _titleText.fontSize = title;
            if (_footerText != null) _footerText.fontSize = IsPhone ? _phoneTitleSize - 2f : _deskTitleSize - 6f;

            if (_newButton != null)
            {
                var label = _newButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.fontSize = IsPhone ? _phoneTitleSize - 2f : _deskTitleSize - 6f;
            }

            for (int i = 0; i < _tabs.Count; i++)
            {
                if (_tabs[i] == null) continue;
                var label = _tabs[i].GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.fontSize = IsPhone ? _phoneTitleSize - 2f : _deskTitleSize - 6f;
            }
        }
    }
}
