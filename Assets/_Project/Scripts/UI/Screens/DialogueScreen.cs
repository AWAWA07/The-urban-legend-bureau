using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.UI
{
    /// <summary>대화에 나오는 인물. 스프라이트는 인스펙터에서 갈아 끼운다.</summary>
    [Serializable]
    public class DialogueCharacter
    {
        [Tooltip("이름의 Localization String ID.")]
        public string nameTextId;

        [Tooltip("임시 이미지. 실제 캐릭터가 생기면 이것만 바꾸면 된다.")]
        public Image image;
    }

    /// <summary>
    /// 튜토리얼 대화 화면.
    ///
    /// 두 인물을 좌우에 세우고, 말하는 쪽을 밝게, 듣는 쪽을 어둡게 둔다.
    /// 말하는 인물은 아주 조금 움직인다. 코루틴으로 처리한다. 외부 트윈 패키지는 쓰지 않는다.
    ///
    /// 다음 대사로 넘기는 것은 화면을 눌렀을 때다. 마우스와 터치를 구분하지 않는다.
    /// 기존 UI 버튼(전체를 덮는 투명 버튼)을 쓰므로 EventSystem 경로를 그대로 탄다.
    /// </summary>
    public class DialogueScreen : UIScreen
    {
        [Header("인물")]
        [SerializeField] private DialogueCharacter _left;
        [SerializeField] private DialogueCharacter _right;

        [Header("대사 상자")]
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _lineText;
        [SerializeField] private TMP_Text _hintText;

        [Tooltip("화면 전체를 덮는 진행 버튼.")]
        [SerializeField] private Button _advanceButton;

        [Header("선택지")]
        [Tooltip("고를 것이 있을 때만 켜지는 자리.")]
        [SerializeField] private RectTransform _choiceRoot;

        [Tooltip("복제할 선택지 버튼.")]
        [SerializeField] private Button _choiceTemplate;

        [Header("밝기")]
        [SerializeField] private Color _brightColor = Color.white;
        [SerializeField] private Color _dimColor = new Color(0.32f, 0.32f, 0.38f, 1f);

        [Tooltip("나레이션 글자색. 인물의 말이 아니라는 것을 색으로 구분한다.")]
        [SerializeField] private Color _narrationColor = new Color(0.98f, 0.92f, 0.66f, 1f);

        private const string HintTextId = "ui.dialogue.hint";

        private Func<string> _lineProvider;
        private Func<string> _nameProvider;
        private Action _onAdvance;

        private Coroutine _motion;
        private RectTransform _speakerRect;
        private Vector2 _speakerHome;

        // --- 강조 움직임 ---
        // 대사가 시작될 때 한 번만 짧게 움직이고 멈춘다. 계속 떠 있는 연출은 쓰지 않는다.
        private const float MotionDuration = 0.3f;
        private const float MotionHeight = 18f;

        /// <summary>인물이 처음 나타날 때 서서히 드러나는 시간. 갑자기 튀어나오지 않게 한다.</summary>
        private const float AppearDuration = 0.25f;

        private Coroutine _leftAppear;
        private Coroutine _rightAppear;

        // 씬에 배치된 원래 자리. 혼자 나올 때 가운데로 옮겼다가 되돌리는 데 쓴다.
        private Vector2 _leftDesignPosition;
        private Vector2 _rightDesignPosition;
        private bool _homesCaptured;

        protected override void Awake()
        {
            base.Awake();

            if (_left != null && _left.image != null) _leftDesignPosition = _left.image.rectTransform.anchoredPosition;
            if (_right != null && _right.image != null) _rightDesignPosition = _right.image.rectTransform.anchoredPosition;
            if (_lineText != null) _lineDefaultColor = _lineText.color;
            _homesCaptured = true;
        }

        private Color _lineDefaultColor = Color.white;

        /// <summary>
        /// 왼쪽 인물만 나와 있을 때는 화면 가운데에 세운다.
        /// 상대가 등장하면 원래 자리로 돌아간다. 대사를 보이기 전에 불러야 한다.
        /// </summary>
        public void SetSoloLayout(bool solo)
        {
            if (!_homesCaptured || _left == null || _left.image == null) return;

            // 움직이던 중이면 먼저 끝낸다.
            // 그러지 않으면 움직임이 기억하던 옛 자리로 되돌려 방금 정한 위치를 덮어쓴다.
            StopMotion();

            var rt = _left.image.rectTransform;
            rt.anchoredPosition = solo
                ? new Vector2(0f, _leftDesignPosition.y)
                : _leftDesignPosition;
        }

        /// <summary>
        /// 한 줄을 보여준다.
        ///
        /// speakerIsLeft 로 어느 쪽이 말하는지 정한다.
        /// 문구는 만드는 방법(Func)으로 받는다. 언어가 바뀌어도 다시 조립된다.
        /// </summary>
        public void ShowLine(bool speakerIsLeft, Func<string> nameProvider, Func<string> lineProvider,
            float speakerAlpha = 1f, bool leftVisible = true, bool rightVisible = true)
        {
            _nameProvider = nameProvider;
            _lineProvider = lineProvider;

            if (_lineText != null) _lineText.color = _lineDefaultColor;

            // 밝기를 먼저 정하고 등장 여부를 나중에 본다.
            // 등장 연출이 지금 정한 색을 목표로 삼아야 색이 덮이지 않는다.
            ApplySpeaker(speakerIsLeft, speakerAlpha);
            SetCharactersVisible(leftVisible, rightVisible);
            Refresh();
        }

        /// <summary>
        /// 인물의 말이 아닌 나레이션 한 줄.
        ///
        /// 말하는 사람이 없으므로 이름도 비우고 강조 움직임도 하지 않는다.
        /// 글자색만 달라서 대사와 구분된다. 인물은 서 있던 그대로 둔다.
        /// </summary>
        public void ShowNarration(Func<string> lineProvider, bool leftVisible = true, bool rightVisible = true)
        {
            _nameProvider = null;
            _lineProvider = lineProvider;

            // 나레이션에는 말하는 사람이 없다. 다음 대사는 다시 움직여야 한다.
            _hasSpoken = false;
            StopMotion();
            if (_lineText != null) _lineText.color = _narrationColor;

            // 나레이션 동안에는 아무도 말하지 않으므로 모두 어둡게 둔다.
            if (_left != null && _left.image != null) _left.image.color = _dimColor;
            if (_right != null && _right.image != null) _right.image.color = _dimColor;

            SetCharactersVisible(leftVisible, rightVisible);
            Refresh();
        }

        /// <summary>
        /// 누가 화면에 있는지 정한다.
        ///
        /// 아직 등장하지 않은 인물은 아예 숨긴다. 어둡게 두는 것과 다르다.
        /// 숨어 있던 인물이 나타날 때는 잠깐 사이에 서서히 드러난다.
        /// </summary>
        public void SetCharactersVisible(bool leftVisible, bool rightVisible)
        {
            ApplyVisible(_left, leftVisible, ref _leftAppear);
            ApplyVisible(_right, rightVisible, ref _rightAppear);
        }

        private void ApplyVisible(DialogueCharacter character, bool visible, ref Coroutine appear)
        {
            if (character == null || character.image == null) return;

            var go = character.image.gameObject;
            bool wasVisible = go.activeSelf;

            if (!visible)
            {
                if (appear != null) { StopCoroutine(appear); appear = null; }
                go.SetActive(false);
                return;
            }

            go.SetActive(true);

            // 이미 나와 있던 인물은 다시 나타나는 연출을 하지 않는다.
            if (wasVisible) return;

            if (appear != null) StopCoroutine(appear);
            appear = isActiveAndEnabled ? StartCoroutine(AppearFade(character.image)) : null;
        }

        /// <summary>등장 연출. 한 번만 돌고 끝난다.</summary>
        private IEnumerator AppearFade(Image image)
        {
            var target = image.color;
            float t = 0f;
            while (t < AppearDuration)
            {
                t += Time.unscaledDeltaTime;
                if (image == null) yield break;

                var c = target;
                c.a = target.a * Mathf.Clamp01(t / AppearDuration);
                image.color = c;
                yield return null;
            }

            if (image != null) image.color = target;
        }

        /// <summary>
        /// 고를 것을 내놓는다.
        ///
        /// 고르는 동안에는 화면을 눌러 넘길 수 없다. 대사를 건너뛰고 넘어가면 순서가 엉킨다.
        /// 문구는 만드는 방법(Func)으로 받는다. 언어가 바뀌어도 다시 조립된다.
        /// </summary>
        public void ShowChoices(IReadOnlyList<Func<string>> labels, Action<int> onPick)
        {
            ClearChoices();
            if (_choiceRoot == null || _choiceTemplate == null || labels == null) return;

            _choiceLabels = labels;
            _onPick = onPick;

            for (int i = 0; i < labels.Count; i++)
            {
                var item = Instantiate(_choiceTemplate, _choiceRoot);
                item.gameObject.name = "Choice_" + i;
                item.gameObject.SetActive(true);

                var label = item.GetComponentInChildren<TMP_Text>(true);
                if (label != null && labels[i] != null) label.text = labels[i]();

                int picked = i;
                item.onClick.RemoveAllListeners();
                item.onClick.AddListener(() => OnChoicePicked(picked));

                _spawnedChoices.Add(item.gameObject);
            }

            _choiceRoot.gameObject.SetActive(true);
            if (_advanceButton != null) _advanceButton.interactable = false;
        }

        /// <summary>고를 것을 치운다. 다시 화면을 눌러 넘길 수 있게 된다.</summary>
        public void ClearChoices()
        {
            for (int i = 0; i < _spawnedChoices.Count; i++)
            {
                if (_spawnedChoices[i] == null) continue;

                // Destroy는 프레임 끝에 처리된다. 떼어 내고 꺼 둔 뒤 파괴해야 같은 프레임에 다시 그려도 남지 않는다.
                _spawnedChoices[i].transform.SetParent(null, false);
                _spawnedChoices[i].SetActive(false);
                Destroy(_spawnedChoices[i]);
            }
            _spawnedChoices.Clear();

            _choiceLabels = null;
            _onPick = null;

            if (_choiceRoot != null) _choiceRoot.gameObject.SetActive(false);
            if (_advanceButton != null) _advanceButton.interactable = true;
        }

        private void OnChoicePicked(int index)
        {
            var handler = _onPick;
            ClearChoices();
            handler?.Invoke(index);
        }

        private readonly List<GameObject> _spawnedChoices = new List<GameObject>();
        private IReadOnlyList<Func<string>> _choiceLabels;
        private Action<int> _onPick;

        /// <summary>화면을 눌렀을 때 부를 것을 지정한다.</summary>
        public void SetAdvanceHandler(Action onAdvance)
        {
            _onAdvance = onAdvance;

            if (_advanceButton != null)
            {
                _advanceButton.onClick.RemoveAllListeners();
                _advanceButton.onClick.AddListener(OnAdvanceClicked);
            }
        }

        private void OnAdvanceClicked()
        {
            // 한 번의 입력으로 두 대사가 넘어가지 않게, 처리는 호출 측에 맡기고 여기서는 한 번만 알린다.
            var handler = _onAdvance;
            if (handler != null) handler();
        }

        protected override void OnOpen()
        {
            // 화면을 새로 열면 첫 대사는 움직이며 시작한다.
            _hasSpoken = false;

            EventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);
            Refresh();
        }

        protected override void OnClose()
        {
            EventBus.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);
            StopMotion();
        }

        private void OnLanguageChanged(LanguageChangedEvent evt)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc)) return;

            // 고를 것이 떠 있으면 문구도 함께 다시 만든다. 언어가 바뀌었을 수 있다.
            if (_choiceLabels != null)
            {
                for (int i = 0; i < _spawnedChoices.Count && i < _choiceLabels.Count; i++)
                {
                    if (_spawnedChoices[i] == null || _choiceLabels[i] == null) continue;
                    var text = _spawnedChoices[i].GetComponentInChildren<TMP_Text>(true);
                    if (text != null) text.text = _choiceLabels[i]();
                }
            }

            if (_nameText != null) _nameText.text = _nameProvider != null ? _nameProvider() : string.Empty;
            if (_lineText != null) _lineText.text = _lineProvider != null ? _lineProvider() : string.Empty;
            if (_hintText != null) _hintText.text = loc.Get(HintTextId);
        }

        // ------------------------------------------------------------- 연출

        /// <summary>
        /// 말하는 쪽을 밝게, 듣는 쪽을 어둡게 한다.
        /// speakerAlpha 는 "아직 어둠 속에 있는" 연출에 쓴다. 1이면 완전히 밝다.
        /// </summary>
        private void ApplySpeaker(bool speakerIsLeft, float speakerAlpha)
        {
            var speaker = speakerIsLeft ? _left : _right;
            var listener = speakerIsLeft ? _right : _left;

            if (speaker != null && speaker.image != null)
            {
                speaker.image.color = Color.Lerp(_dimColor, _brightColor, Mathf.Clamp01(speakerAlpha));
            }
            if (listener != null && listener.image != null)
            {
                listener.image.color = _dimColor;
            }

            // 강조 움직임은 말하는 사람이 바뀔 때만 준다.
            // 같은 사람이 여러 마디를 이어 말하는데 매번 들썩이면 보기에 사납다.
            bool sameSpeaker = _hasSpoken && _lastSpeakerIsLeft == speakerIsLeft;
            if (!sameSpeaker) StartMotion(speaker);

            _lastSpeakerIsLeft = speakerIsLeft;
            _hasSpoken = true;
        }

        /// <summary>바로 앞 마디를 누가 말했는가. 같은 사람이면 움직이지 않는다.</summary>
        private bool _lastSpeakerIsLeft;
        private bool _hasSpoken;

        /// <summary>
        /// 새 화자의 강조 움직임을 시작한다.
        /// 이전 움직임은 먼저 끊고 위치를 되돌린다. 대사가 이어져도 위치가 누적되지 않는다.
        /// </summary>
        private void StartMotion(DialogueCharacter speaker)
        {
            StopMotion();
            if (speaker == null || speaker.image == null) return;

            _speakerRect = speaker.image.rectTransform;
            _speakerHome = _speakerRect.anchoredPosition;
            if (isActiveAndEnabled) _motion = StartCoroutine(SpeakMotion());
        }

        private void StopMotion()
        {
            if (_motion != null)
            {
                StopCoroutine(_motion);
                _motion = null;
            }

            // 움직이던 인물을 제자리에 돌려놓는다. 다음 대사에서 어긋난 위치로 시작하지 않게 한다.
            if (_speakerRect != null) _speakerRect.anchoredPosition = _speakerHome;
            _speakerRect = null;
        }

        /// <summary>
        /// 대사가 시작될 때 딱 한 번, 위로 조금 올라갔다 제자리로 돌아온다.
        /// 끝나면 코루틴이 종료되므로 그 뒤로는 완전히 멈춘다.
        /// </summary>
        private IEnumerator SpeakMotion()
        {
            var rect = _speakerRect;
            var home = _speakerHome;

            float t = 0f;
            while (t < MotionDuration)
            {
                t += Time.unscaledDeltaTime;
                if (rect == null) yield break;

                // 0 -> 1 -> 0. 올라갔다가 그대로 내려온다.
                float phase = Mathf.Clamp01(t / MotionDuration);
                float lift = Mathf.Sin(phase * Mathf.PI);
                rect.anchoredPosition = home + new Vector2(0f, lift * MotionHeight);
                yield return null;
            }

            // 계산 오차가 남지 않도록 원래 위치를 그대로 다시 넣는다.
            if (rect != null) rect.anchoredPosition = home;
            _motion = null;
        }
    }
}
