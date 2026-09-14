using UnityEngine;
using UrbanLegendBureau.Systems;

namespace UrbanLegendBureau.Data
{
    /// <summary>
    /// 사건 하나의 정적 정의.
    ///
    /// 지금까지 CaseDirector가 인스펙터 문자열로 들고 있던 caseId / legendId 를 여기로 옮긴다.
    /// 사건을 추가할 때 코드를 고치지 않는다. 이 에셋을 하나 더 만들면 된다.
    ///
    /// 진행 상태는 여기에 저장하지 않는다.
    /// 완료 여부는 SaveData.completedCaseIds 가, 현재 단계는 currentStepIndex 가 들고 있다.
    /// </summary>
    [CreateAssetMenu(fileName = "case_", menuName = "Game Data/Case", order = 5)]
    public class CaseSO : ScriptableObject, IGameDataAsset
    {
        [Header("식별")]
        [Tooltip("SaveData.currentCaseId / completedCaseIds 에 기록되는 고유 ID. 예: case_test_001")]
        [SerializeField] private string _caseId;

        [Header("텍스트 (Localization String ID)")]
        [Tooltip("사건 이름의 String ID. 예: case.test.001.name")]
        [SerializeField] private string _caseNameTextId;

        [Tooltip("사건 개요의 String ID. 예: case.test.001.description")]
        [SerializeField] private string _caseDescriptionTextId;

        [Header("연결")]
        [Tooltip("이 사건이 다루는 괴담의 ID. LegendSO를 직접 참조하지 않고 ID로 잇는다.")]
        [SerializeField] private string _legendId;

        [Header("진행")]
        [Tooltip("사건을 새로 시작할 때의 단계.")]
        [SerializeField] private CaseStep _startingStep = CaseStep.LegendBriefing;

        [Tooltip("끄면 사건 목록에 나오지만 선택할 수 없다. 제작 중인 사건을 감추는 데 쓴다.")]
        [SerializeField] private bool _isPlayable = true;

        public string Id => _caseId;
        public string CaseId => _caseId;
        public string CaseNameTextId => _caseNameTextId;
        public string CaseDescriptionTextId => _caseDescriptionTextId;
        public string LegendId => _legendId;
        public CaseStep StartingStep => _startingStep;
        public bool IsPlayable => _isPlayable;

        private void OnValidate()
        {
            _caseId = _caseId?.Trim();
            _caseNameTextId = _caseNameTextId?.Trim();
            _caseDescriptionTextId = _caseDescriptionTextId?.Trim();
            _legendId = _legendId?.Trim();
        }
    }
}
