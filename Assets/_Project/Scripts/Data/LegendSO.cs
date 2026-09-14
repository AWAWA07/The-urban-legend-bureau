using System.Collections.Generic;
using UnityEngine;

namespace UrbanLegendBureau.Data
{
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

        [Header("구성 데이터")]
        [Tooltip("이 괴담의 규칙들. 진짜 규칙과 함정 규칙이 섞여 있을 수 있다.")]
        [SerializeField] private List<RuleSO> _rules = new List<RuleSO>();

        [Tooltip("이 괴담 조사에서 얻을 수 있는 단서 전체.")]
        [SerializeField] private List<ClueSO> _clues = new List<ClueSO>();

        [Tooltip("이 괴담과 관련해 인터넷에 존재하는 글들.")]
        [SerializeField] private List<WebPageSO> _webPages = new List<WebPageSO>();

        public string Id => _legendId;
        public string LegendId => _legendId;
        public string NameTextId => _nameTextId;
        public string DescriptionTextId => _descriptionTextId;
        public LegendRiskLevel RiskLevel => _riskLevel;

        /// <summary>위험 등급 이름의 String ID. 표시 문구는 Localization 테이블에 있다.</summary>
        public string RiskLevelTextId => _riskLevel.ToTextId();

        public float InitialSpreadRate => _initialSpreadRate;
        public IReadOnlyList<RuleSO> Rules => _rules;
        public IReadOnlyList<ClueSO> Clues => _clues;
        public IReadOnlyList<WebPageSO> WebPages => _webPages;

        private void OnValidate()
        {
            _legendId = _legendId?.Trim();
            _nameTextId = _nameTextId?.Trim();
            _descriptionTextId = _descriptionTextId?.Trim();
        }
    }
}
