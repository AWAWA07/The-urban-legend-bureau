using System.Collections.Generic;
using UnityEngine;

namespace UrbanLegendBureau.Data
{
    /// <summary>
    /// 괴담 하나에 대한 최종 판정.
    ///
    /// 조사의 답은 둘뿐이다. 진짜인가 가짜인가, 그리고 어떻게 끊는가.
    /// 앞쪽이 이것이다. 괴담은 대개 통째로 참이거나 거짓이기보다,
    /// 참인 알맹이에 사람들이 살을 붙인 모양으로 굴러다닌다.
    /// </summary>
    public enum LegendVerdict
    {
        /// <summary>전부 지어낸 이야기다.</summary>
        Fake = 0,

        /// <summary>이야기 그대로 일어난다.</summary>
        Real = 1,

        /// <summary>알맹이는 진짜고 나머지는 나중에 덧붙었다.</summary>
        Half = 2
    }

    /// <summary>
    /// 괴담 하나의 정적 정의.
    ///
    /// 여기에는 플레이 중 변하는 값을 넣지 않는다.
    /// 확산도/발견 여부/봉인 여부 같은 진행 상태는 SaveData.legendStates 가 legendId 로 들고 있다.
    ///
    /// 괴담을 추가할 때 코드를 고치지 않는다. 이 에셋을 하나 더 만들면 된다.
    /// </summary>
    [CreateAssetMenu(fileName = "legend_", menuName = "Game Data/Legend", order = 10)]
    public class LegendSO : ScriptableObject, IGameDataAsset
    {
        [Header("식별")]
        [Tooltip("SaveData에 기록되는 고유 ID. 예: legend_test_001. 한 번 정하면 바꾸지 않는다.")]
        [SerializeField] private string _legendId;

        [Header("텍스트 (Localization String ID)")]
        [Tooltip("괴담 이름의 String ID. 예: legend.test.001.name")]
        [SerializeField] private string _nameTextId;

        [Tooltip("괴담 설명의 String ID. 예: legend.test.001.description")]
        [SerializeField] private string _descriptionTextId;

        [Header("위험도")]
        [SerializeField] private LegendRiskLevel _riskLevel = LegendRiskLevel.Observation;

        [Tooltip("사건 시작 시점의 확산도. 진행 중 값은 SaveData가 들고 있다.")]
        [SerializeField, Range(0f, 100f)] private float _initialSpreadRate;

        [Header("결론 (Localization String ID)")]
        [Tooltip("이 괴담이 진짜인지 가짜인지, 그리고 왜 그런지. 규칙을 맞히면 이 글이 결론으로 나온다.")]
        [SerializeField] private string _verdictTextId;

        [Tooltip("이 괴담이 진짜인가 가짜인가. 플레이어가 고른 답을 여기에 견준다.")]
        [SerializeField] private LegendVerdict _verdictKind = LegendVerdict.Real;

        [Tooltip("판정의 근거가 되는 단서들. 결론 칸이 이 단서들을 그대로 인용한다.")]
        [SerializeField] private List<string> _verdictClueIds = new List<string>();

        [Tooltip("이 괴담을 어떻게 피하거나 끊는가. 맞는 규칙에서 따라 나오는 한 줄이다.")]
        [SerializeField] private string _counterTextId;

        [Tooltip("파훼법 후보들. 플레이어가 이 중에서 고른다. 정답은 위의 파훼법 한 줄이며, 이 목록에 함께 들어 있어야 한다.")]
        [SerializeField] private List<string> _counterOptionTextIds = new List<string>();

        [Tooltip("파훼법의 근거가 되는 단서들.")]
        [SerializeField] private List<string> _counterClueIds = new List<string>();

        [Header("구성 데이터")]
        [Tooltip("이 괴담의 규칙들. 진짜 규칙과 함정 규칙이 섞여 있을 수 있다.")]
        [SerializeField] private List<RuleSO> _rules = new List<RuleSO>();

        [Tooltip("이 괴담 조사에서 얻을 수 있는 단서 전체.")]
        [SerializeField] private List<ClueSO> _clues = new List<ClueSO>();

        [Tooltip("이 괴담과 관련해 인터넷에 존재하는 글들.")]
        [SerializeField] private List<WebPageSO> _webPages = new List<WebPageSO>();

        [Tooltip("이 괴담을 조사할 때 고를 수 있는 조사 행동들.")]
        [SerializeField] private List<InvestigationActionSO> _investigationActions = new List<InvestigationActionSO>();

        public string Id => _legendId;
        public string LegendId => _legendId;
        public string NameTextId => _nameTextId;
        public string DescriptionTextId => _descriptionTextId;
        public LegendRiskLevel RiskLevel => _riskLevel;

        /// <summary>위험 등급 이름의 String ID. 표시 문구는 Localization 테이블에 있다.</summary>
        public string RiskLevelTextId => _riskLevel.ToTextId();

        public float InitialSpreadRate => _initialSpreadRate;
        public string VerdictTextId => _verdictTextId;
        public string CounterTextId => _counterTextId;
        public LegendVerdict VerdictKind => _verdictKind;
        public IReadOnlyList<string> CounterOptionTextIds => _counterOptionTextIds;
        public IReadOnlyList<string> VerdictClueIds => _verdictClueIds;
        public IReadOnlyList<string> CounterClueIds => _counterClueIds;
        public IReadOnlyList<RuleSO> Rules => _rules;
        public IReadOnlyList<ClueSO> Clues => _clues;
        public IReadOnlyList<WebPageSO> WebPages => _webPages;
        public IReadOnlyList<InvestigationActionSO> InvestigationActions => _investigationActions;

        private void OnValidate()
        {
            _legendId = _legendId?.Trim();
            _nameTextId = _nameTextId?.Trim();
            _descriptionTextId = _descriptionTextId?.Trim();
        }
    }
}
