using System;
using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.InputSystemLayer;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 현장에서 무엇을 조사할지 정한다.
    ///
    /// 손가락으로 찍어 고르지 않는다. 걸어가서 곁에 서면 그 지점 위에 말풍선이 뜨고,
    /// 상호작용 키를 눌러야 조사가 된다. 현장에 서 있다는 느낌은 여기서 나온다.
    ///
    /// 옆에서 본 한 폭의 장면이라 가깝고 먼 것은 좌우 거리로만 잰다.
    /// 천장의 CCTV처럼 높이 달린 것도 그 아래에 서면 닿는다.
    ///
    /// 조사할 수 있는지(단서·단계 조건)는 여기서 따지지 않는다. 그것은 CaseDirector 의 일이다.
    /// 여기서는 "무엇 앞에 서 있고 언제 눌렀는가"까지만 알린다.
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

        [Header("가까이 가서 조사하기")]
        [Tooltip("이만큼 안에 들어오면 조사할 수 있다. 좌우 거리로만 잰다.")]
        [SerializeField] private float _reach = 2.4f;

        [Tooltip("조사할 것 위에 뜨는 말풍선. 현장마다 따로 두지 않고 하나를 옮겨 쓴다.")]
        [SerializeField] private FieldPrompt _prompt;

        private InputService _input;
        private LocalizationService _loc;

        /// <summary>지금 켜진 현장에 든 것들. 현장을 켤 때 한 번만 모은다.</summary>
        private FieldWalker[] _walkers;
        private InvestigationPoint[] _points;

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
            ServiceRegistry.TryGet(out _loc);
        }

        /// <summary>지금 곁에 서 있는 지점. 없으면 null.</summary>
        public InvestigationPoint NearPoint { get; private set; }

        /// <summary>
        /// 지금 조사할 수 있는가.
        ///
        /// 끄면 곁에 서도 말풍선이 뜨지 않고 키도 먹지 않는다. 걸어 다니는 것은 그대로다.
        /// 막차가 종점에 닿은 뒤처럼, 장소는 보이지만 더 뒤질 것이 없을 때 끈다.
        /// </summary>
        public bool InvestigationAllowed { get; set; } = true;

        /// <summary>
        /// 해당 괴담의 현장을 켠다. 다른 괴담의 현장은 꺼진다.
        /// legendId를 비워 두거나 visible이 false면 전부 끈다.
        /// </summary>
        public void SetFieldVisible(bool visible, string legendId = null)
        {
            IsActive = false;
            ActiveRoot = null;

            ClearNear();
            _walkers = null;
            _points = null;

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

                // 걷는 사람도 조사할 것도 현장이 켜져 있는 동안에는 늘어나지 않는다.
                // 꺼진 것까지 모아 두고, 매 프레임에는 켜진 것만 본다.
                // 막차 현장처럼 승강장과 열차 안이 번갈아 켜지는 곳이 있기 때문이다.
                _walkers = group.root.GetComponentsInChildren<FieldWalker>(true);
                _points = group.root.GetComponentsInChildren<InvestigationPoint>(true);
                return;
            }

            Debug.LogWarning($"[FieldController] '{legendId}' 에 대응하는 현장이 없다. 인스펙터의 현장 목록을 확인할 것.");
        }

        private void Update()
        {
            if (!IsActive || _input == null || !_input.IsReady) return;

            // 누가 말하는 동안, 그리고 다른 화면이 위에 떠 있는 동안에는 조사도 멈춘다.
            // 걷는 쪽(FieldWalker)과 같은 곳을 본다.
            if (UrbanLegendBureau.UI.FieldHudScreen.IsSpeaking || !UrbanLegendBureau.UI.FieldHudScreen.IsFront)
            {
                ClearNear();
                return;
            }

            SetNear(FindNear());

            // 곁에 선 채로 조사를 마쳤으면 말풍선의 글도 바뀌어야 한다.
            // 떠났다 돌아올 때까지 "조사함" 이 안 붙으면 방금 뒤진 곳을 또 누르게 된다.
            if (NearPoint != null && NearPoint.IsInvestigated != _nearWasInvestigated)
            {
                _nearWasInvestigated = NearPoint.IsInvestigated;
                if (_prompt != null) _prompt.Show(NearPoint, BuildPromptText(NearPoint));
            }

            if (NearPoint != null && _input.InteractPressed)
            {
                Investigated?.Invoke(NearPoint);
            }
        }

        /// <summary>곁에 선 지점을 마지막으로 봤을 때 조사가 끝나 있었는가.</summary>
        private bool _nearWasInvestigated;

        /// <summary>
        /// 서 있는 자리에서 가장 가까운 조사 지점.
        ///
        /// 좌우 거리로만 잰다. 위아래까지 재면 천장의 CCTV 처럼 높이 달린 것에 영영 닿지 못한다.
        /// </summary>
        private InvestigationPoint FindNear()
        {
            if (!InvestigationAllowed) return null;

            var walker = ActiveWalker();
            if (walker == null || _points == null) return null;

            float x = walker.transform.position.x;

            InvestigationPoint best = null;
            float bestDistance = _reach;

            for (int i = 0; i < _points.Length; i++)
            {
                var point = _points[i];

                // 아직 열리지 않은 지점은 꺼져 있다. 탈 자리가 그렇다.
                if (point == null || !point.isActiveAndEnabled) continue;

                float distance = Mathf.Abs(point.transform.position.x - x);
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                best = point;
            }

            return best;
        }

        /// <summary>지금 켜져 있는 쪽의 걷는 사람. 승강장과 열차 안에 하나씩 있다.</summary>
        private FieldWalker ActiveWalker()
        {
            if (_walkers == null) return null;

            for (int i = 0; i < _walkers.Length; i++)
            {
                if (_walkers[i] != null && _walkers[i].isActiveAndEnabled) return _walkers[i];
            }
            return null;
        }

        private void SetNear(InvestigationPoint point)
        {
            if (NearPoint == point) return;

            if (NearPoint != null) NearPoint.SetHighlighted(false);
            NearPoint = point;

            if (point == null)
            {
                if (_prompt != null) _prompt.Hide();
                return;
            }

            point.SetHighlighted(true);
            _nearWasInvestigated = point.IsInvestigated;
            if (_prompt != null) _prompt.Show(point, BuildPromptText(point));
        }

        private void ClearNear()
        {
            if (NearPoint != null) NearPoint.SetHighlighted(false);
            NearPoint = null;
            if (_prompt != null) _prompt.Hide();
        }

        private const string PromptTextId = "ui.field.prompt";
        private const string PromptDoneTextId = "ui.field.prompt_done";

        /// <summary>
        /// 말풍선에 적을 글. 무슨 키를 누르는지와 무엇을 조사하는지를 함께 적는다.
        /// 이미 뒤져 본 곳은 그렇다고 적는다. 같은 곳을 두 번 뒤지느라 시간을 버리지 않게 한다.
        /// </summary>
        private string BuildPromptText(InvestigationPoint point)
        {
            if (_loc == null && !ServiceRegistry.TryGet(out _loc)) return string.Empty;

            string what = string.IsNullOrEmpty(point.NameTextId) ? point.PointId : _loc.Get(point.NameTextId);
            return _loc.Get(point.IsInvestigated ? PromptDoneTextId : PromptTextId, what);
        }
    }
}
