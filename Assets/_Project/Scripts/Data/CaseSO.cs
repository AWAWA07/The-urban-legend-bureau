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

        [Header("게시물 작성")]
        [Tooltip("조사를 마친 뒤 괴담넷에 올릴 글의 데이터. 비워 두면 이 사건에는 글쓰기가 없다.")]
        [SerializeField] private PostWritingSO _postWriting;

        [Header("추리")]
        [Tooltip("수사 노트, 단서 분석, 최종 추론, 조사 보고서가 읽는 이 사건의 추리 자료. 비워 두면 예전 규칙 추론 흐름을 쓴다.")]
        [SerializeField] private CaseDeductionSO _deduction;

        [Header("본편")]
        [Tooltip("본편에서 등장하는 사건인가. 튜토리얼 사건은 끈다.")]
        [SerializeField] private bool _mainStory;

        [Tooltip("본편에 등장할 때 이 괴담 글들이 가진 믿음도(%).")]
        [SerializeField, Range(1, 99)] private int _startBelief = 20;

        [Tooltip("이 근무 일차부터 등장할 수 있다. 동시 활성 상한이 차 있으면 자리가 날 때까지 기다린다.")]
        [SerializeField] private int _appearWorkday = 2;

        public string Id => _caseId;
        public string CaseId => _caseId;
        public bool MainStory => _mainStory;
        public int StartBelief => _startBelief;
        public int AppearWorkday => _appearWorkday;
        public string CaseNameTextId => _caseNameTextId;
        public string CaseDescriptionTextId => _caseDescriptionTextId;
        public string LegendId => _legendId;
        public CaseStep StartingStep => _startingStep;
        public bool IsPlayable => _isPlayable;

        /// <summary>이 사건의 게시물 작성 데이터. 없으면 null.</summary>
        public PostWritingSO PostWriting => _postWriting;

        /// <summary>이 사건의 추리 자료. 없으면 null.</summary>
        public CaseDeductionSO Deduction => _deduction;

        private void OnValidate()
        {
            _caseId = _caseId?.Trim();
            _caseNameTextId = _caseNameTextId?.Trim();
            _caseDescriptionTextId = _caseDescriptionTextId?.Trim();
            _legendId = _legendId?.Trim();
        }
    }
}
