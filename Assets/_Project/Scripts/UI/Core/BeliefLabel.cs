using TMPro;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 전체 믿음도를 적는 글자 하나. 시계 옆에 선다.
    ///
    /// 값은 GameStatus 한 곳에서 온다. 컴퓨터 작업 표시줄이든 휴대폰 상태 줄이든 같은 숫자다.
    /// 값이 바뀔 때만 다시 쓴다.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class BeliefLabel : MonoBehaviour
    {
        private const string BeliefTextId = "ui.desktop.belief";
        private const string ShortTextId = "ui.status.belief_short";

        [Tooltip("좁은 상태 줄에서는 '전체 믿음도' 를 '믿음' 으로 줄인다. 한 줄에 들어가야 한다.")]
        [SerializeField] private bool _short;

        private TMP_Text _label;
        private int _shown = int.MinValue;

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            _shown = int.MinValue;   // 다시 켜면 곧바로 한 번 쓴다
        }

        private void Update()
        {
            if (GameStatus.Belief == _shown) return;
            _shown = GameStatus.Belief;

            if (_label == null) return;
            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc)) return;

            _label.text = loc.Get(_short ? ShortTextId : BeliefTextId, _shown);
        }
    }
}
