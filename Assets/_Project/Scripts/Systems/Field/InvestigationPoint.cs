using UnityEngine;
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

        [Header("결과")]
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
