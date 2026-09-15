using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Systems;

namespace UrbanLegendBureau.Data
{
    /// <summary>
    /// 플레이어가 고르는 조사 행동 하나의 정적 정의.
    ///
    /// 행동의 성격을 코드가 아니라 데이터로 정한다. 행동을 늘릴 때 코드를 고치지 않는다.
    /// 어느 사건에서 고를 수 있는지는 LegendSO의 목록이 정한다. ClueSO / WebPageSO 와 같은 방식이다.
    ///
    /// 진행 상태는 여기에 저장하지 않는다.
    /// 얻은 단서는 SaveData.acquiredClueIds 가, 사건 시간은 SaveData.caseTimes 가 들고 있다.
    /// </summary>
    [CreateAssetMenu(fileName = "action_", menuName = "Game Data/Investigation Action", order = 40)]
    public class InvestigationActionSO : ScriptableObject, IGameDataAsset
    {
        [Header("식별")]
        [Tooltip("고유 ID. 예: action_test_001")]
        [SerializeField] private string _actionId;

        [Header("텍스트 (Localization String ID)")]
        [Tooltip("버튼에 보일 행동 이름. 예: action.test.001.name")]
        [SerializeField] private string _actionNameTextId;

        [Tooltip("행동 설명. 예: action.test.001.desc")]
        [SerializeField] private string _descriptionTextId;

        [Tooltip("조사에 성공했을 때의 결과 문구. 예: action.test.001.result")]
        [SerializeField] private string _resultTextId;

        [Header("성격")]
        [Tooltip("15단계 InvestigationAction 중 어디에 해당하는가. 시간/확산 기본값을 여기서 가져온다.")]
        [SerializeField] private InvestigationAction _actionKind = InvestigationAction.FieldSearch;

        [Tooltip("어디에서 고를 수 있는가. 지정하지 않으면(Both) 예전처럼 사무실과 현장 양쪽에 나온다.")]
        [SerializeField] private ActionUsage _usage = ActionUsage.Both;

        [Tooltip("이 행동이 쓰는 사건 시간(분). 0이면 InvestigationTimeService의 기본값을 쓴다.")]
        [SerializeField, Min(0)] private int _minutesOverride;

        [Tooltip("켜면 아래 값으로 확산량을 덮어쓴다. 끄면 행동 종류의 기본값을 쓴다.")]
        [SerializeField] private bool _useSpreadOverride;

        [SerializeField, Min(0f)] private float _spreadOverride;

        [Header("조건")]
        [Tooltip("이 단서들을 모두 가지고 있어야 실행할 수 있다. 비우면 조건 없음.")]
        [SerializeField] private List<string> _requiredClueIds = new List<string>();

        [Tooltip("켜면 사건이 아래 단계 이상으로 진행돼야 실행할 수 있다.")]
        [SerializeField] private bool _requireStep;

        [SerializeField] private CaseStep _requiredStep = CaseStep.InternetResearch;

        [Header("결과")]
        [Tooltip("성공하면 얻는 단서 ID. 비우면 단서 없는 조사다. 이미 가진 단서는 다시 추가되지 않는다.")]
        [SerializeField] private string _rewardClueId;

        [Tooltip("성공 시 진행시킬 사건 단계. None이면 단계를 건드리지 않는다.")]
        [SerializeField] private bool _advanceStep;

        [SerializeField] private CaseStep _stepOnSuccess = CaseStep.FieldInvestigation;

        public string Id => _actionId;
        public string ActionId => _actionId;
        public string ActionNameTextId => _actionNameTextId;
        public string DescriptionTextId => _descriptionTextId;
        public string ResultTextId => _resultTextId;
        public InvestigationAction ActionKind => _actionKind;
        public ActionUsage Usage => _usage;
        public int MinutesOverride => _minutesOverride;

        /// <summary>사무실 목록에 나오는가.</summary>
        public bool UsableInOffice => _usage != ActionUsage.Field;

        /// <summary>현장 지점에서 고를 수 있는가.</summary>
        public bool UsableInField => _usage != ActionUsage.Office;

        /// <summary>이 행동이 실제로 쓰는 사건 시간(분).</summary>
        public int GetMinutes(int defaultMinutes)
        {
            return _minutesOverride > 0 ? _minutesOverride : defaultMinutes;
        }

        /// <summary>이 행동이 실제로 올리는 확산량.</summary>
        public float GetSpreadCost(float defaultSpread)
        {
            return _useSpreadOverride ? _spreadOverride : defaultSpread;
        }
        public bool UseSpreadOverride => _useSpreadOverride;
        public float SpreadOverride => _spreadOverride;
        public IReadOnlyList<string> RequiredClueIds => _requiredClueIds;
        public bool RequireStep => _requireStep;
        public CaseStep RequiredStep => _requiredStep;
        public string RewardClueId => _rewardClueId;
        public bool HasRewardClue => !string.IsNullOrEmpty(_rewardClueId);
        public bool AdvanceStep => _advanceStep;
        public CaseStep StepOnSuccess => _stepOnSuccess;

        private void OnValidate()
        {
            _actionId = _actionId?.Trim();
            _actionNameTextId = _actionNameTextId?.Trim();
            _descriptionTextId = _descriptionTextId?.Trim();
            _resultTextId = _resultTextId?.Trim();
            _rewardClueId = _rewardClueId?.Trim();
        }
    }
}
