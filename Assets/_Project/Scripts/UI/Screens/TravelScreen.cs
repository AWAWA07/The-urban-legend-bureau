using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 한 장소에서 다른 장소로 옮겨 가는 동안 덮는 화면.
    ///
    /// 장면이 툭 바뀌면 그 사이에 흐른 시간이 느껴지지 않는다. 그래서 잠깐 화면을 덮고,
    /// 그 위에서 택시가 길을 따라 달리고 시계가 흘러가는 모습을 보여 준다.
    /// 흐른 만큼의 시간을 실제로 시계에 더하는 일은 부른 쪽이 넘겨준 onMinutes 가 한다.
    /// 이 화면은 그림과 박자만 맡는다.
    /// </summary>
    public class TravelScreen : UIScreen
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _durationText;

        [Tooltip("점 셋이 차례로 차오르는 글자. 기다리는 중이라는 표시.")]
        [SerializeField] private TMP_Text _dotsText;

        [Tooltip("길 위를 달리는 작은 택시. 왼쪽 끝에서 오른쪽 끝까지 간다.")]
        [SerializeField] private RectTransform _runner;

        [Tooltip("택시가 달리는 길. 이 폭만큼 간다.")]
        [SerializeField] private RectTransform _track;

        [Tooltip("아래의 진행 막대. 가로로 차오른다.")]
        [SerializeField] private RectTransform _barFill;

        [Tooltip("화면 전체의 밝기. 들어올 때 밝아지고 나갈 때 어두워진다.")]
        [SerializeField] private CanvasGroup _group;

        private const float FadeSeconds = 0.3f;
        private const float BobHeight = 4f;
        private const float BobSpeed = 18f;
        private string _titleId;
        private string _durationId;
        private int _minutes;
        private Coroutine _playing;

        /// <summary>
        /// 옮겨 가는 중인가. 이 화면이 덮고 있는 동안에는 현장에서 걷거나 조사하지 않는다.
        /// 이 화면은 화면 쌓기 밖에 떠 있어서 현장 화면이 덮였다는 것을 모른다. 그래서 따로 알린다.
        /// </summary>
        public static bool IsPlaying { get; private set; }

        /// <summary>
        /// 옮겨 가는 모습을 seconds 동안 보여 준다.
        ///
        /// 그 사이 minutes 분이 흐른다. 흐른 몫은 조금씩 onMinutes 로 넘긴다. 합치면 꼭 minutes 가 된다.
        /// 순서: 밝아지며 덮는다 → 달린다 → 도착(onArrive) → 옅어지며 걷힌다 → 끝(onDone).
        /// 장소를 바꾸는 일은 onArrive 에서 한다. 화면이 완전히 덮고 있을 때라 떠나온 곳이 비치지 않는다.
        /// 대사처럼 화면이 걷힌 뒤에 보여야 하는 것은 onDone 에서 한다. 화면을 닫는 것도 부른 쪽이 한다.
        /// </summary>
        public void Play(string titleTextId, string durationTextId, int minutes, float seconds, Action<int> onMinutes,
            Action onArrive, Action onDone)
        {
            _titleId = titleTextId;
            _durationId = durationTextId;
            _minutes = minutes;
            Refresh();

            if (_playing != null) StopCoroutine(_playing);
            _playing = StartCoroutine(Run(minutes, Mathf.Max(0.5f, seconds), onMinutes, onArrive, onDone));
        }

        private IEnumerator Run(int minutes, float seconds, Action<int> onMinutes, Action onArrive, Action onDone)
        {
            IsPlaying = true;
            int given = 0;
            Place(0f, 0f);

            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float p = t / seconds;

                // 들어올 때만 밝아진다. 나갈 때의 옅어짐은 도착한 뒤에 따로 한다.
                if (_group != null) _group.alpha = Mathf.Clamp01(t / FadeSeconds);

                // 가운데는 빠르고 양 끝은 느리다. 출발하고 서는 차처럼 보인다.
                float eased = Mathf.SmoothStep(0f, 1f, p);
                Place(eased, t);

                int due = Mathf.FloorToInt(minutes * eased);
                if (due > given)
                {
                    onMinutes?.Invoke(due - given);
                    given = due;
                }

                if (_dotsText != null) _dotsText.text = new string('·', 1 + Mathf.FloorToInt(t * 3f) % 3);
                yield return null;
            }

            if (given < minutes) onMinutes?.Invoke(minutes - given);
            Place(1f, seconds);
            if (_group != null) _group.alpha = 1f;

            // 덮고 있는 동안 장소를 바꾼다. 한 박자 쉬어 새 장소가 다 그려진 뒤에 걷는다.
            onArrive?.Invoke();
            yield return null;

            for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                if (_group != null) _group.alpha = 1f - t / FadeSeconds;
                yield return null;
            }
            if (_group != null) _group.alpha = 0f;

            _playing = null;
            IsPlaying = false;
            onDone?.Invoke();
        }

        /// <summary>택시와 막대를 진행한 만큼 옮긴다. 달리는 동안 택시가 살짝 들썩인다.</summary>
        private void Place(float progress, float time)
        {
            if (_barFill != null) _barFill.anchorMax = new Vector2(progress, 1f);

            if (_runner != null && _track != null)
            {
                float width = _track.rect.width;
                float bob = progress > 0f && progress < 1f ? Mathf.Abs(Mathf.Sin(time * BobSpeed)) * BobHeight : 0f;
                _runner.anchoredPosition = new Vector2(Mathf.Lerp(0f, width, progress), bob);
            }
        }

        protected override void OnOpen()
        {
            EventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);
            if (_group != null) _group.alpha = 0f;
            Refresh();
        }

        protected override void OnClose()
        {
            EventBus.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);
            if (_playing != null) { StopCoroutine(_playing); _playing = null; }
            IsPlaying = false;
        }

        private void OnLanguageChanged(LanguageChangedEvent evt)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc)) return;

            if (_titleText != null) _titleText.text = string.IsNullOrEmpty(_titleId) ? string.Empty : loc.Get(_titleId);
            if (_durationText != null) _durationText.text = string.IsNullOrEmpty(_durationId) ? string.Empty : loc.Get(_durationId, _minutes);
        }
    }
}
