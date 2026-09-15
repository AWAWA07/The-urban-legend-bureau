using System.Collections.Generic;
using UnityEngine;

namespace UrbanLegendBureau.Data
{
    /// <summary>
    /// 런타임에 필요한 정적 데이터를 직접 참조로 모아 둔 목록.
    ///
    /// 이 방식을 고른 이유:
    ///  - Resources 폴더를 새로 늘리지 않는다. (기존 Resources는 Localization 전용)
    ///  - Addressables 같은 패키지를 도입하지 않는다.
    ///  - 직접 참조이므로 참조된 에셋이 빌드에 확실히 포함된다.
    ///  - 에셋을 옮기거나 이름을 바꿔도 참조가 끊기지 않는다.
    ///
    /// 목록은 손으로 채우지 않아도 된다.
    /// UrbanLegendBureau > Data > Rebuild Game Data Catalog 메뉴가 폴더를 훑어 채운다.
    /// </summary>
    [CreateAssetMenu(fileName = "GameDataCatalog", menuName = "Game Data/Catalog", order = 0)]
    public class GameDataCatalogSO : ScriptableObject
    {
        [Tooltip("게임에 존재하는 모든 사건.")]
        [SerializeField] private List<CaseSO> _cases = new List<CaseSO>();

        [Tooltip("게임에 존재하는 모든 괴담. 메뉴로 자동 갱신할 수 있다.")]
        [SerializeField] private List<LegendSO> _legends = new List<LegendSO>();

        [Tooltip("게임에 존재하는 모든 규칙.")]
        [SerializeField] private List<RuleSO> _rules = new List<RuleSO>();

        [Tooltip("게임에 존재하는 모든 단서.")]
        [SerializeField] private List<ClueSO> _clues = new List<ClueSO>();

        [Tooltip("게임에 존재하는 모든 인터넷 페이지.")]
        [SerializeField] private List<WebPageSO> _webPages = new List<WebPageSO>();

        [Tooltip("게임에 존재하는 모든 조사 행동.")]
        [SerializeField] private List<InvestigationActionSO> _investigationActions = new List<InvestigationActionSO>();

        public IReadOnlyList<CaseSO> Cases => _cases;
        public IReadOnlyList<LegendSO> Legends => _legends;
        public IReadOnlyList<RuleSO> Rules => _rules;
        public IReadOnlyList<ClueSO> Clues => _clues;
        public IReadOnlyList<WebPageSO> WebPages => _webPages;
        public IReadOnlyList<InvestigationActionSO> InvestigationActions => _investigationActions;

        public int CaseCount => _cases != null ? _cases.Count : 0;
        public int LegendCount => _legends != null ? _legends.Count : 0;
        public int RuleCount => _rules != null ? _rules.Count : 0;
    }
}
