using System;
using System.Collections.Generic;
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
        /// <summary>괴담 하나에 대응하는 현장.</summary>
        [System.Serializable]
        public class FieldGroup
        {
            [Tooltip("이 현장이 속한 괴담의 ID.")]
            public string legendId;

            [Tooltip("현장 오브젝트들의 부모.")]
            public GameObject root;
        }

        [Tooltip("괴담별 현장. 사건이 늘면 항목을 추가한다. 코드를 고칠 필요는 없다.")]
        [SerializeField] private List<FieldGroup> _fieldGroups = new List<FieldGroup>();

        [Tooltip("조사 지점이 속한 레이어. 비워 두면 모든 레이어를 검사한다.")]
        [SerializeField] private LayerMask _investigationLayers = ~0;

        private InputService _input;
        private InvestigationPoint _pressedPoint;

        /// <summary>조사 지점이 실제로 조사됐을 때. 결과 처리는 구독자가 한다.</summary>
        public event Action<InvestigationPoint> Investigated;

        /// <summary>현재 조사 입력을 받는가.</summary>
        public bool IsActive { get; private set; }

        /// <summary>지금 켜져 있는 현장의 부모. 켜진 현장이 없으면 null.</summary>
        public GameObject ActiveRoot { get; private set; }

        private void Awake()
        {
            HideAll();
        }

        private void HideAll()
        {
            for (int i = 0; i < _fieldGroups.Count; i++)
            {
                if (_fieldGroups[i] != null && _fieldGroups[i].root != null)
                {
                    _fieldGroups[i].root.SetActive(false);
                }
            }
        }

        private void Start()
        {
            ServiceRegistry.TryGet(out _input);
        }

        /// <summary>
        /// 해당 괴담의 현장을 켠다. 다른 괴담의 현장은 꺼진다.
        /// legendId를 비워 두거나 visible이 false면 전부 끈다.
        /// </summary>
        public void SetFieldVisible(bool visible, string legendId = null)
        {
            IsActive = false;
            ActiveRoot = null;
            _pressedPoint = null;

            HideAll();
            if (!visible || string.IsNullOrEmpty(legendId)) return;

            for (int i = 0; i < _fieldGroups.Count; i++)
            {
                var group = _fieldGroups[i];
                if (group == null || group.root == null) continue;
                if (group.legendId != legendId) continue;

                group.root.SetActive(true);
                ActiveRoot = group.root;
                IsActive = true;
                return;
            }

            Debug.LogWarning($"[FieldController] '{legendId}' 에 대응하는 현장이 없다. 인스펙터의 현장 목록을 확인할 것.");
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
