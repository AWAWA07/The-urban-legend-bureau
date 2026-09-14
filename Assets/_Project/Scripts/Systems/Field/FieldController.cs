using System;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.InputSystemLayer;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 현장의 포인터 입력을 조사 지점으로 전달한다.
    ///
    /// 기존 PointerInput을 그대로 쓴다. 마우스인지 터치인지 구분하지 않는다.
    /// UI 위를 눌렀을 때는 월드 조사가 일어나지 않게 막는다.
    /// </summary>
    public class FieldController : MonoBehaviour
    {
        [Tooltip("현장 오브젝트들의 부모. 현장 단계가 아닐 때는 꺼 둔다.")]
        [SerializeField] private GameObject _fieldRoot;

        [Tooltip("조사 지점이 속한 레이어. 비워 두면 모든 레이어를 검사한다.")]
        [SerializeField] private LayerMask _investigationLayers = ~0;

        private InputService _input;
        private InvestigationPoint _pressedPoint;

        /// <summary>조사 지점이 실제로 조사됐을 때. 결과 처리는 구독자가 한다.</summary>
        public event Action<InvestigationPoint> Investigated;

        /// <summary>현재 조사 입력을 받는가.</summary>
        public bool IsActive { get; private set; }

        private void Awake()
        {
            SetFieldVisible(false);
        }

        private void Start()
        {
            ServiceRegistry.TryGet(out _input);
        }

        /// <summary>현장을 켜고 끈다.</summary>
        public void SetFieldVisible(bool visible)
        {
            IsActive = visible;
            if (_fieldRoot != null) _fieldRoot.SetActive(visible);
            _pressedPoint = null;
        }

        private void Update()
        {
            if (!IsActive || _input == null || !_input.IsReady) return;

            var pointer = _input.Pointer;

            if (pointer.Pressed)
            {
                // UI 위를 누른 것이면 월드 조사로 넘기지 않는다.
                _pressedPoint = pointer.IsOverUI ? null : Raycast(pointer.WorldPosition);
                if (_pressedPoint != null)
                {
                    var context = pointer.CreateContext();
                    _pressedPoint.PointerPressed(in context);
                }
            }

            if (pointer.Released && _pressedPoint != null)
            {
                var context = pointer.CreateContext();
                _pressedPoint.PointerReleased(in context);

                // 누른 곳과 뗀 곳이 같을 때만 조사로 친다.
                var released = pointer.IsOverUI ? null : Raycast(pointer.WorldPosition);
                if (released == _pressedPoint)
                {
                    Investigated?.Invoke(_pressedPoint);
                }

                _pressedPoint = null;
            }
        }

        private InvestigationPoint Raycast(Vector2 worldPosition)
        {
            var hit = Physics2D.OverlapPoint(worldPosition, _investigationLayers);
            return hit != null ? hit.GetComponentInParent<InvestigationPoint>() : null;
        }
    }
}
