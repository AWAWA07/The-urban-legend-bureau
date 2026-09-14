using System;
using System.Collections.Generic;
using UnityEngine;

namespace UrbanLegendBureau.Data
{
    /// <summary>
    /// 규칙 조건의 종류.
    /// 괴담마다 규칙이 다르므로, 조건을 코드가 아니라 데이터로 표현하기 위한 최소 어휘다.
    /// 실제 판정은 이후 단계의 RuleService가 담당한다.
    /// </summary>
    public enum RuleConditionType
    {
        None = 0,

        /// <summary>특정 행동을 했는가 (targetId = 행동 ID)</summary>
        PerformAction = 1,

        /// <summary>특정 장소에 있는가 (targetId = 장소 ID)</summary>
        AtLocation = 2,

        /// <summary>특정 오브젝트와 상호작용했는가 (targetId = 오브젝트 ID)</summary>
        WithObject = 3,

        /// <summary>특정 단서를 가지고 있는가 (targetId = clueId)</summary>
        HasClue = 4,

        /// <summary>특정 상태인가 (targetId = 상태 ID, 예: 시간대/소지품)</summary>
        HasState = 5
    }

    /// <summary>규칙 조건 하나. 데이터로만 존재하며 이번 단계에서는 평가하지 않는다.</summary>
    [Serializable]
    public class RuleCondition
    {
        [SerializeField] private RuleConditionType _type = RuleConditionType.None;

        [Tooltip("조건 대상의 ID. 종류에 따라 행동/장소/오브젝트/단서/상태 ID가 들어간다.")]
        [SerializeField] private string _targetId;

        [Tooltip("수치가 필요한 조건에서 사용한다. 예: 시각, 횟수.")]
        [SerializeField] private float _value;

        [Tooltip("켜면 조건을 부정한다. '~하지 않아야 한다' 형태의 규칙에 쓴다.")]
        [SerializeField] private bool _negate;

        public RuleConditionType Type => _type;
        public string TargetId => _targetId;
        public float Value => _value;
        public bool Negate => _negate;
    }

    /// <summary>
    /// 괴담의 규칙 하나에 대한 정적 정의.
    ///
    /// 플레이어가 이 규칙을 추론해냈는지는 여기에 저장하지 않는다.
    /// 추론에 성공하면 SaveData.deducedRuleIds 에 ruleId 문자열만 들어간다.
    /// </summary>
    [CreateAssetMenu(fileName = "rule_", menuName = "Game Data/Rule", order = 20)]
    public class RuleSO : ScriptableObject, IGameDataAsset
    {
        [Header("식별")]
        [Tooltip("SaveData에 기록되는 고유 ID. 예: rule_test_001")]
        [SerializeField] private string _ruleId;

        [Header("텍스트 (Localization String ID)")]
        [Tooltip("규칙 문장의 String ID. 예: rule.test.001.text")]
        [SerializeField] private string _ruleTextId;

        [Header("성격")]
        [Tooltip("진짜 규칙이면 켠다. 끄면 함정 규칙이다. 플레이어가 잘못 믿으면 퇴마에 실패한다.")]
        [SerializeField] private bool _isTrue = true;

        [Header("추론 조건")]
        [Tooltip("이 규칙을 추론하려면 필요한 단서들의 ID.")]
        [SerializeField] private List<string> _requiredClueIds = new List<string>();

        [Header("규칙 조건 (판정 로직은 이후 단계)")]
        [Tooltip("규칙이 발동하는 조건들. 이번 단계에서는 데이터로만 존재한다.")]
        [SerializeField] private List<RuleCondition> _conditions = new List<RuleCondition>();

        public string Id => _ruleId;
        public string RuleId => _ruleId;
        public string RuleTextId => _ruleTextId;
        public bool IsTrue => _isTrue;
        public IReadOnlyList<string> RequiredClueIds => _requiredClueIds;
        public IReadOnlyList<RuleCondition> Conditions => _conditions;

        private void OnValidate()
        {
            _ruleId = _ruleId?.Trim();
            _ruleTextId = _ruleTextId?.Trim();
        }
    }
}
