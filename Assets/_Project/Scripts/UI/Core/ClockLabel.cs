using TMPro;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 지금 시각을 적는 글자 하나.
    ///
    /// 컴퓨터 작업 표시줄에도, 휴대폰 위쪽에도 같은 것이 붙는다. 시각은 GameClock 한 곳에서 온다.
    /// 분이 바뀔 때만 다시 쓴다. 매 프레임 글자를 만들 이유가 없다.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class ClockLabel : MonoBehaviour
    {
        [Tooltip("좁은 상태 줄에서는 오전/오후를 떼고 시각만 적는다.")]
        [SerializeField] private bool _short;

        private TMP_Text _label;
        private int _shownMinute = int.MinValue;

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            _shownMinute = int.MinValue;   // 다시 켜면 곧바로 한 번 쓴다
        }

        private void Update()
        {
            int minute = GameClock.ElapsedMinutes;
            if (minute == _shownMinute) return;

            _shownMinute = minute;
            Write();
        }

        private void Write()
        {
            if (_label == null) return;

            if (_short)
            {
                _label.text = GameClock.FormatShort();
                return;
            }

            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc)) return;
            _label.text = GameClock.Format(loc);
        }
    }
}
