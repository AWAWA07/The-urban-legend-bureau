using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.InputSystemLayer;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 현장에서 클릭/탭으로 조사할 수 있는 오브젝트.
    ///
    /// 실제 조사 처리는 FieldController가 한다. 이 컴포넌트는
    /// "무엇을 보여주고 무엇을 주는가"라는 데이터만 들고 있다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class InvestigationPoint : MonoBehaviour, IPointerInteractable
    {
        [Header("표시 텍스트 (Localization String ID)")]
        [SerializeField] private string _nameTextId;
        [SerializeField] private string _resultTextId;

        [Header("식별")]
        [Tooltip("지점 고유 ID. 로그와 디버그용. 비워 두면 오브젝트 이름을 쓴다.")]
        [SerializeField] private string _pointId;

        [Header("조사 방법 (17단계)")]
        [Tooltip("이 지점에서 고를 수 있는 조사 행동. 비워 두면 예전처럼 곧바로 단서를 준다.")]
        [SerializeField] private List<InvestigationActionSO> _actions = new List<InvestigationActionSO>();

        [Header("해금 조건")]
        [Tooltip("이 단서들을 모두 가지고 있어야 조사할 수 있다. 비우면 언제나 조사 가능.")]
        [SerializeField] private List<string> _requiredClueIds = new List<string>();

        [Tooltip("켜면 사건이 아래 단계 이상으로 진행돼야 조사할 수 있다.")]
        [SerializeField] private bool _requireStep;

        [SerializeField] private CaseStep _requiredStep = CaseStep.FieldInvestigation;

        [Header("결과 (조사 방법이 없을 때 쓰는 예전 방식)")]
        [Tooltip("조사 시 획득하는 단서의 ID. 비워 두면 단서가 없는 지점이다.")]
        [SerializeField] private string _clueId;

        [Header("연출")]
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _pressedColor = new Color(1f, 0.85f, 0.4f);
        [SerializeField] private Color _investigatedColor = new Color(0.45f, 0.45f, 0.5f);

        public string NameTextId => _nameTextId;
        public string ResultTextId => _resultTextId;
        public string ClueId => _clueId;
        public bool HasClue => !string.IsNullOrEmpty(_clueId);

        public string PointId => string.IsNullOrEmpty(_pointId) ? name : _pointId;

        /// <summary>이 지점에서 고를 수 있는 조사 방법.</summary>
        public IReadOnlyList<InvestigationActionSO> Actions => _actions;

        /// <summary>조사 방법을 가진 지점인가. 아니라면 예전처럼 곧바로 단서를 준다.</summary>
        public bool HasActions => _actions != null && _actions.Count > 0;

        public IReadOnlyList<string> RequiredClueIds => _requiredClueIds;
        public bool RequireStep => _requireStep;
        public CaseStep RequiredStep => _requiredStep;

        /// <summary>이미 조사한 지점인가.</summary>
        public bool IsInvestigated { get; private set; }

        private void Awake()
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            ApplyColor(_normalColor);
        }

        /// <summary>조사 완료 표시. 다시 조사해도 되지만 눈으로 구분되게 한다.</summary>
        public void MarkInvestigated()
        {
            IsInvestigated = true;
            ApplyColor(_investigatedColor);
        }

        public void ResetVisual()
        {
            IsInvestigated = false;
            ApplyColor(_normalColor);
        }

        private void ApplyColor(Color color)
        {
            if (_renderer != null) _renderer.color = color;
        }

        // ------------------------------------------------------- IPointerInteractable

        public void PointerPressed(in PointerContext context)
        {
            ApplyColor(_pressedColor);
        }

        public void PointerReleased(in PointerContext context)
        {
            ApplyColor(IsInvestigated ? _investigatedColor : _normalColor);
        }

        public void PointerHeld(in PointerContext context)
        {
        }
    }
}
