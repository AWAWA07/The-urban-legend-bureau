using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 현장의 물건 하나를 화면 가득 크게 들여다보는 화면. 열차 안 CCTV, 창문 같은 것.
    ///
    /// 그 물건에서 눌러 볼 수 있는 곳(살필 곳)이 몇 군데 있다. 누르면 아래 띠에 본 것을 한 줄 적는다.
    /// 무엇을 보았는지는 화면이 기억한다. 몇 군데를 살폈는지 SpotChecked 로 알린다. 그것으로 연출을 거는 것은 부르는 쪽 일이다.
    /// 살펴보는 것뿐이라 시간도 확산도 쓰지 않는다. 돌아가기를 누르면 현장으로 돌아간다.
    ///
    /// 말을 주고받는 연출도 띄울 수 있다(Talk). 그동안 띠 위 이름표에 말하는 사람이 적히고, 화면 어디를 눌러도 다음 말로 넘어간다.
    /// </summary>
    public class CloseupScreen : UIScreen
    {
        [Serializable]
        public class Spot
        {
            public Button button;
            [Tooltip("눌렀을 때 아래 띠에 적는 글.")]
            public string textId;

            [Tooltip("눌렀을 때 숨기는 것. 닫힌 덮개처럼 열어야 보이는 것을 가리던 그림.")]
            public GameObject hideOnCheck;

            [Tooltip("눌렀을 때 드러나는 것. 덮개를 열면 보이는 안쪽.")]
            public GameObject showOnCheck;

            [Tooltip("켜면 처음에는 숨어 있다가 RevealSpot 으로 드러난다. 무언가 일어난 뒤에야 생기는 자국 같은 것이다.")]
            public bool hiddenAtStart;

            [Tooltip("드러날 때 함께 스르르 나타나는 그림.")]
            public GameObject revealObject;

            [Tooltip("처음 살필 때 함께 하는 조사 행동(InvestigationActionSO 의 ID). 시간이 흐르고 단서가 생긴다. 비우면 현장을 한 번 뒤진 것으로 친다.")]
            public string actionId;
        }

        [SerializeField] private Spot[] _spots = new Spot[0];
        [SerializeField] private Button _closeButton;

        [Tooltip("아래 띠. 처음에는 안내 한 줄이 떠 있다.")]
        [SerializeField] private TMP_Text _lineText;

        [Tooltip("띠 위 이름표. 평소에는 '조사', 말을 주고받는 동안에는 말하는 사람.")]
        [SerializeField] private TMP_Text _tabText;

        [Tooltip("말을 주고받는 동안 화면 전체를 덮어 누르면 넘어가게 하는 판.")]
        [SerializeField] private Button _advanceButton;

        [SerializeField] private string _hintTextId = "field.subway.cctv_close.hint";
        [SerializeField] private string _tabTextId = "ui.field.closeup_tab";

        [Tooltip("정전 뒤에 덮는 어둠. 마우스를 따라가는 휴대폰 라이트 자리만 조금 보인다(CloseupDark). 평소에는 꺼 둔다.")]
        [SerializeField] private GameObject _dark;

        [Tooltip("어둠이 덮였을 때만 켜는 것. 어둠 위에서 빛나야 하는 것(CCTV 전원 불)을 어둠 앞에 따로 그려 둔다.")]
        [SerializeField] private GameObject[] _darkOnly = new GameObject[0];

        /// <summary>정전 뒤인가. 켜면 화면이 현장처럼 깜깜하고 라이트 둘레만 보인다.</summary>
        public void SetDark(bool on)
        {
            if (_dark != null) _dark.SetActive(on);
            foreach (var g in _darkOnly) if (g != null) g.SetActive(on);
        }

        /// <summary>돌아가기를 눌렀다. 화면 스택을 아는 쪽이 닫는다.</summary>
        public Action Closed;

        /// <summary>새 곳을 살폈다. 지금까지 살핀 곳의 수를 넘긴다(같은 곳을 다시 누르면 세지 않는다).</summary>
        public Action<int> SpotChecked;

        /// <summary>새 곳을 살폈다. 살핀 곳의 번호(_spots 순서)를 넘긴다.</summary>
        public Action<int> SpotIndexChecked;

        private readonly HashSet<int> _checked = new HashSet<int>();
        private string[] _talkSpeakers;
        private string[] _talkLines;
        private int _talkIndex;
        private Action _talkDone;
        private Action<int> _talkOnLine;   // 한 줄을 띄울 때마다 그 번호로 부른다(그림 바꾸기 같은 것)

        protected override void Awake()
        {
            base.Awake();
            ResetChecked();
            for (int i = 0; i < _spots.Length; i++)
            {
                int index = i;
                if (_spots[i] != null && _spots[i].button != null) _spots[i].button.onClick.AddListener(() => OnSpot(index));
            }
            if (_closeButton != null) _closeButton.onClick.AddListener(() => Closed?.Invoke());
            if (_advanceButton != null)
            {
                _advanceButton.onClick.AddListener(NextTalk);
                _advanceButton.gameObject.SetActive(false);
            }
        }

        protected override void OnOpen()
        {
            UrbanLegendBureau.Core.Sfx.Play("closeup_open", 0.25f);   // 가까이 다가가는 쉬익. 작게 둔다. 크면 무언가 나타나는 소리로 들린다
            SetTab(_tabTextId);
            ShowLine(_hintTextId);
            SetInteractable(true);
        }

        protected override void OnClose()
        {
            // 닫을 때는 소리를 내지 않는다. 길게 빠지는 쉬익이 현장으로 돌아올 때마다 귀신이 사라지는 소리처럼 들렸다.
        }

        /// <summary>살필 곳을 모두 살폈는가. 나중에 드러나는 자리도 센다.</summary>
        public bool AllChecked
        {
            get
            {
                for (int i = 0; i < _spots.Length; i++)
                    if (_spots[i] != null && _spots[i].button != null && !_checked.Contains(i)) return false;
                return true;
            }
        }

        /// <summary>한 곳이라도 살폈는가.</summary>
        public bool AnyChecked => _checked.Count > 0;

        /// <summary>조사 행동이 걸린 자리(단서가 나오는 곳)를 모두 살폈는가. 승차권 같은 곁가지는 세지 않는다.</summary>
        public bool ActionSpotsChecked
        {
            get
            {
                for (int i = 0; i < _spots.Length; i++)
                    if (_spots[i] != null && !string.IsNullOrEmpty(_spots[i].actionId) && !_checked.Contains(i)) return false;
                return true;
            }
        }

        /// <summary>살핀 기록을 지운다. 사건이 새로 시작될 때 부른다.</summary>
        public void ResetChecked()
        {
            _checked.Clear();
            foreach (var s in _spots)
            {
                if (s == null) continue;
                if (s.hideOnCheck != null) s.hideOnCheck.SetActive(true);
                if (s.showOnCheck != null) s.showOnCheck.SetActive(false);
                if (s.hiddenAtStart)
                {
                    if (s.button != null) s.button.gameObject.SetActive(false);
                    if (s.revealObject != null) s.revealObject.SetActive(false);
                }
            }
        }

        /// <summary>그 살필 곳에 걸린 조사 행동의 ID. 없으면 빈 문자열.</summary>
        public string SpotActionId(int index)
        {
            return index >= 0 && index < _spots.Length && _spots[index] != null ? _spots[index].actionId ?? string.Empty : string.Empty;
        }

        /// <summary>아래 띠의 글 뒤에 한 줄을 덧붙인다. 살펴서 얻은 단서를 알릴 때 쓴다.</summary>
        public void AppendLine(string text)
        {
            if (_lineText == null || string.IsNullOrEmpty(text)) return;
            _lineText.text += "\n" + text;
        }

        /// <summary>숨어 있던 살필 곳을 드러낸다. 그림은 스르르 나타난다. 드러난 뒤로는 사건이 새로 시작될 때까지 그대로 있다.</summary>
        public void RevealSpot(int index)
        {
            if (index < 0 || index >= _spots.Length || _spots[index] == null) return;
            var s = _spots[index];
            if (s.button != null) s.button.gameObject.SetActive(true);
            UrbanLegendBureau.Core.Sfx.Play("heartbeat", 0.5f);   // 없던 것이 드러날 때 쿵쿵
            if (s.revealObject == null) return;
            s.revealObject.SetActive(true);
            foreach (var g in s.revealObject.GetComponentsInChildren<Graphic>())
            {
                g.canvasRenderer.SetAlpha(0f);
                g.CrossFadeAlpha(1f, 1.6f, true);
            }
        }

        /// <summary>살필 곳과 돌아가기를 누를 수 있는지 정한다. 연출 동안 막는다.</summary>
        public void SetInteractable(bool on)
        {
            foreach (var s in _spots) if (s != null && s.button != null) s.button.interactable = on;
            if (_closeButton != null) _closeButton.interactable = on;
        }

        /// <summary>
        /// 말을 차례로 띄운다. speakers 는 말하는 사람 이름 글 ID, lines 는 대사 글 ID 다.
        /// 화면 어디를 눌러도 다음 말로 넘어가고, 다 끝나면 이름표를 '조사' 로 되돌리고 done 을 부른다.
        /// </summary>
        public void Talk(string[] speakers, string[] lines, Action done, Action<int> onLine = null)
        {
            _talkOnLine = onLine;
            _talkSpeakers = speakers;
            _talkLines = lines;
            _talkIndex = -1;
            _talkDone = done;
            SetInteractable(false);
            if (_advanceButton != null)
            {
                _advanceButton.gameObject.SetActive(true);
                _advanceButton.transform.SetAsLastSibling();
            }
            NextTalk();
        }

        private void NextTalk()
        {
            if (_talkLines == null) return;
            _talkIndex++;
            if (_talkIndex >= _talkLines.Length)
            {
                _talkLines = null;
                if (_advanceButton != null) _advanceButton.gameObject.SetActive(false);
                SetTab(_tabTextId);
                if (_lineText != null) _lineText.text = string.Empty;   // 말이 끝나면 띠를 비우고 다시 조사로 돌아간다
                SetInteractable(true);
                var done = _talkDone;
                _talkDone = null;
                done?.Invoke();
                return;
            }
            SetTab(_talkSpeakers[_talkIndex]);
            ShowLine(_talkLines[_talkIndex]);
            _talkOnLine?.Invoke(_talkIndex);
        }

        private void OnSpot(int index)
        {
            // 부르는 쪽이 이 누름을 가로채면(앉아 보기를 묻는 것 같은) 평소처럼 살피지 않는다.
            if (SpotIntercept != null && SpotIntercept(index)) return;
            UrbanLegendBureau.Core.Sfx.PlayVaried("inspect", 0.5f);   // 만져 보고 들춰 보는 소리
            var spot = _spots[index];
            if (spot.hideOnCheck != null) spot.hideOnCheck.SetActive(false);
            if (spot.showOnCheck != null) spot.showOnCheck.SetActive(true);
            ShowLine(_spots[index].textId);
            if (_checked.Add(index))
            {
                SpotIndexChecked?.Invoke(index);
                SpotChecked?.Invoke(_checked.Count);
            }
        }

        // ------------------------------------------------------------- 묻기와 그림

        /// <summary>
        /// 살필 곳을 눌렀을 때 먼저 묻는다. true 를 돌려주면 그 누름은 여기서 끝나고 평소처럼 살피지 않는다.
        /// 다 살핀 뒤 같은 자리를 다시 눌러 다른 일(직접 앉아 보기)을 하게 할 때 쓴다.
        /// </summary>
        public Func<int, bool> SpotIntercept;

        private TMP_Text _pinLabel;
        private Image _pinFill;

        private void Update()
        {
            // 표시한 자리의 노란빛이 천천히 숨 쉰다. 외곽선 깜빡임(HotspotGlow)과 같은 빠르기다.
            if (_pinFill == null || !_pinFill.gameObject.activeSelf) return;
            float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f);
            _pinFill.color = new Color(1f, 0.86f, 0.25f, Mathf.Lerp(0.02f, 0.07f, k));
        }

        /// <summary>
        /// 그 살필 곳을 또렷이 표시한다. 노란 외곽선이 숨 쉬듯 깜빡이며 남고, 그 위에 짧은 글(label)이 뜬다.
        /// 다 살핀 뒤 같은 자리에서 다른 일을 해야 할 때(빈자리에 직접 앉아 보기) 어디를 누를지 알린다. off 면 거둔다.
        /// </summary>
        public void PinSpot(int index, bool on, string label = null)
        {
            if (index < 0 || index >= _spots.Length || _spots[index] == null || _spots[index].button == null) return;
            var button = _spots[index].button;
            var glow = button.GetComponent<HotspotGlow>();
            if (glow != null) glow.Pinned = on;

            if (_pinLabel == null && on)
            {
                var go = new GameObject("PinLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(button.transform.parent, false);
                _pinLabel = go.GetComponent<TextMeshProUGUI>();
                if (_lineText != null) _pinLabel.font = _lineText.font;
                _pinLabel.fontSize = 38f;
                _pinLabel.fontStyle = FontStyles.Bold;
                _pinLabel.alignment = TextAlignmentOptions.Center;
                _pinLabel.color = new Color(1f, 0.86f, 0.25f, 1f);
                _pinLabel.raycastTarget = false;
                _pinLabel.textWrappingMode = TextWrappingModes.NoWrap;
            }
            // 누르는 자리 전체를 옅은 노란빛으로 덮는다. 누름은 그대로 아래 단추가 받는다.
            if (_pinFill == null && on)
            {
                var fill = new GameObject("PinFill", typeof(RectTransform), typeof(Image));
                fill.transform.SetParent(button.transform.parent, false);
                _pinFill = fill.GetComponent<Image>();
                _pinFill.raycastTarget = false;
            }
            if (_pinFill != null)
            {
                _pinFill.gameObject.SetActive(on);
                if (on)
                {
                    var src = glow != null && glow.PrimaryTarget != null ? glow.PrimaryTarget : (RectTransform)button.transform;   // 빛나는 그림(방석)에 맞춘다
                    var frt = _pinFill.rectTransform;
                    frt.anchorMin = src.anchorMin; frt.anchorMax = src.anchorMax; frt.pivot = src.pivot;
                    frt.anchoredPosition = src.anchoredPosition; frt.sizeDelta = src.sizeDelta;
                    _pinFill.transform.SetAsLastSibling();
                }
            }

            if (_pinLabel == null) return;

            _pinLabel.gameObject.SetActive(on && !string.IsNullOrEmpty(label));
            if (!on) return;
            _pinLabel.text = label;
            // 빛나는 그림(방석) 바로 위, 가운데.
            var b = glow != null && glow.PrimaryTarget != null ? glow.PrimaryTarget : (RectTransform)button.transform;
            var rt = _pinLabel.rectTransform;
            rt.anchorMin = rt.anchorMax = b.anchorMin;
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(600f, 60f);
            rt.anchoredPosition = b.anchoredPosition + new Vector2(0f, b.sizeDelta.y * (1f - b.pivot.y) + 18f);
            _pinLabel.transform.SetAsLastSibling();
        }

        private readonly List<GameObject> _choices = new List<GameObject>();
        private Image _illustration;

        /// <summary>
        /// 띠에 물음을 띄우고 고를 것을 띠 오른쪽에 세로로 세운다. 고르는 동안에는 살필 곳도 돌아가기도 눌리지 않는다.
        /// 고르면 이름표를 '조사' 로 되돌리고 onPick 에 고른 번호를 넘긴다.
        /// 단추는 돌아가기 단추를 본떠 만든다. 모양과 글꼴이 화면의 다른 단추와 같다.
        /// </summary>
        public void Ask(string speakerTextId, string questionTextId, string[] optionTextIds, Action<int> onPick)
        {
            ClearChoices();
            SetTab(speakerTextId);
            ShowLine(questionTextId);
            SetInteractable(false);
            if (_closeButton == null || optionTextIds == null) return;

            var band = _lineText != null ? _lineText.transform.parent : transform;
            for (int i = 0; i < optionTextIds.Length; i++)
            {
                var go = Instantiate(_closeButton.gameObject, band);
                go.name = "Choice_" + i;
                foreach (var localized in go.GetComponentsInChildren<LocalizedText>(true)) DestroyImmediate(localized);
                var label = go.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = Get(optionTextIds[i]);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot = new Vector2(1f, 0.5f);
                rt.sizeDelta = new Vector2(380f, rt.sizeDelta.y);
                float gap = rt.sizeDelta.y + 16f;
                rt.anchoredPosition = new Vector2(-120f, ((optionTextIds.Length - 1) * 0.5f - i) * gap);
                var button = go.GetComponent<Button>();
                button.interactable = true;
                button.onClick.RemoveAllListeners();
                int picked = i;
                button.onClick.AddListener(() =>
                {
                    ClearChoices();
                    SetTab(_tabTextId);
                    if (_lineText != null) _lineText.text = string.Empty;
                    SetInteractable(true);
                    onPick?.Invoke(picked);
                });
                _choices.Add(go);
            }
        }

        private void ClearChoices()
        {
            foreach (var c in _choices) if (c != null) Destroy(c);
            _choices.Clear();
        }

        /// <summary>
        /// 화면 전체에 그림 한 장을 깐다. 아래 띠와 이름표는 그 위에 남아 말을 이어 갈 수 있다. 서서히 떠오른다.
        /// 그림이 없으면 검은 판에 그림 자리라고만 적는다(그림을 넣기 전에도 흐름은 돈다).
        /// </summary>
        public void ShowIllustration(Sprite sprite, string placeholder)
        {
            if (_illustration == null)
            {
                var go = new GameObject("Illustration", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                _illustration = go.GetComponent<Image>();
                _illustration.raycastTarget = false;

                var text = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
                text.transform.SetParent(go.transform, false);
                var trt = (RectTransform)text.transform;
                trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.62f);
                trt.sizeDelta = new Vector2(1400f, 200f);
                var tmp = text.GetComponent<TextMeshProUGUI>();
                if (_lineText != null) tmp.font = _lineText.font;
                tmp.fontSize = 40f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(1f, 1f, 1f, 0.35f);
                tmp.raycastTarget = false;
            }

            // 띠와 이름표, 넘기는 판보다 뒤, 나머지 모든 것보다 앞.
            // 이미 떠 있으면 다음 그림으로 곧장 바꾼다(장면이 끊어 넘어가듯). 처음 뜰 때만 서서히 떠오른다.
            bool shown = _illustration.gameObject.activeSelf;
            var bandRoot = _lineText != null ? _lineText.transform.parent : null;
            // 그림이 띠보다 뒤에 있을 때만 옮긴다. 이미 앞에 있는 것을 띠 자리로 옮기면 하나씩 밀려 띠 뒤로 가 버린다.
            int bandIndex = bandRoot != null ? bandRoot.GetSiblingIndex() : transform.childCount - 1;
            if (_illustration.transform.GetSiblingIndex() > bandIndex) _illustration.transform.SetSiblingIndex(bandIndex);

            _illustration.sprite = sprite;
            _illustration.color = sprite != null ? Color.white : Color.black;
            _illustration.preserveAspect = false;
            var holder = _illustration.GetComponentInChildren<TMP_Text>(true);
            holder.text = sprite != null ? string.Empty : placeholder;
            _illustration.gameObject.SetActive(true);
            if (_closeButton != null) _closeButton.gameObject.SetActive(false);   // 그림 위로 돌아가기가 뜨지 않게
            if (shown) return;
            foreach (var g in _illustration.GetComponentsInChildren<Graphic>(true))
            {
                g.canvasRenderer.SetAlpha(0f);
                g.CrossFadeAlpha(1f, 0.8f, true);
            }
        }

        /// <summary>깔았던 그림을 거둔다.</summary>
        public void HideIllustration()
        {
            if (_illustration != null) _illustration.gameObject.SetActive(false);
            if (_closeButton != null) _closeButton.gameObject.SetActive(true);
        }

        private void ShowLine(string id)
        {
            if (_lineText != null) _lineText.text = Get(id);
        }

        private void SetTab(string id)
        {
            if (_tabText != null) _tabText.text = Get(id);
        }

        private static string Get(string id)
        {
            return ServiceRegistry.TryGet<LocalizationService>(out var loc) ? loc.Get(id) : id;
        }
    }
}
