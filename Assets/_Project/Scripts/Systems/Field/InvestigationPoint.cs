using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Data;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 현장에서 곁에 서서 조사할 수 있는 오브젝트.
    ///
    /// 무엇 앞에 서 있는지 고르고 조사를 걸어 주는 것은 FieldController가 한다. 이 컴포넌트는
    /// "무엇을 보여주고 무엇을 주는가"라는 데이터만 들고 있다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class InvestigationPoint : MonoBehaviour
    {
        [Header("표시 텍스트 (Localization String ID)")]
        [SerializeField] private string _nameTextId;
        [SerializeField] private string _resultTextId;

        [Tooltip("곁에 섰을 때 말풍선에 적을 글. {0} 자리에 이름이 들어간다. 비워 두면 \"조사\"로 적는다.\n" +
                 "숙소의 침대나 컴퓨터처럼 조사할 것이 아닌 물건에 쓴다.")]
        [SerializeField] private string _promptTextId;

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

        [Tooltip("곁에 섰을 때 밝힐 그림들. 채워 두면 네모 칸 대신 이 그림들만 밝아져 물건 모양에 꼭 맞는다.")]
        [SerializeField] private SpriteRenderer[] _highlightTargets;

        [Tooltip("밝힐 때 흰색으로 다가가는 정도.")]
        [Range(0f, 1f)]
        [SerializeField] private float _highlightAmount = 0.32f;

        /// <summary>밝히기 전 그림들의 색. 처음 한 번만 받아 둔다.</summary>
        private Color[] _targetColors;

        public string NameTextId => _nameTextId;
        public string ResultTextId => _resultTextId;
        public string PromptTextId => _promptTextId;
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

        /// <summary>지금 곁에 서 있는 지점인가. 색은 이 값과 조사 여부로 정해진다.</summary>
        private bool _highlighted;

        private void Awake()
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            if (HasTargets)
            {
                _targetColors = new Color[_highlightTargets.Length];
                for (int i = 0; i < _highlightTargets.Length; i++)
                    if (_highlightTargets[i] != null) _targetColors[i] = _highlightTargets[i].color;
            }
            ApplyColor();
        }

        /// <summary>조사 완료 표시. 다시 조사해도 되지만 눈으로 구분되게 한다.</summary>
        public void MarkInvestigated()
        {
            IsInvestigated = true;
            ApplyColor();
        }

        public void ResetVisual()
        {
            IsInvestigated = false;
            ApplyColor();
        }

        // ------------------------------------------------------- 곁에 섰을 때

        /// <summary>
        /// 곁에 서면 드러나고 떠나면 다시 숨는다.
        ///
        /// 조사할 곳을 늘 칠해 두면 장면이 아니라 문제집처럼 보인다.
        /// 그래서 평소에는 아무 표시도 하지 않고, 걸어가 닿았을 때에만 그 자리를 밝힌다.
        /// 어디를 조사할 수 있는지는 돌아다니며 알게 된다.
        /// </summary>
        public void SetHighlighted(bool on)
        {
            if (_highlighted == on) return;

            _highlighted = on;
            ApplyColor();
        }

        /// <summary>
        /// 지금 상태에 맞는 색을 입힌다.
        /// 이미 조사한 곳은 곁에 서도 옅게만 밝아진다. 한 일과 안 한 일이 구분돼야 한다.
        /// </summary>
        private bool HasTargets => _highlightTargets != null && _highlightTargets.Length > 0;

        private void ApplyColor()
        {
            // 물건 그림을 밝히는 지점. 네모 칸은 늘 숨겨 둔다.
            if (HasTargets && _targetColors != null)
            {
                if (_renderer != null) _renderer.color = _normalColor;

                float amount = !_highlighted ? 0f : (IsInvestigated ? _highlightAmount * 0.5f : _highlightAmount);
                for (int i = 0; i < _highlightTargets.Length; i++)
                {
                    var target = _highlightTargets[i];
                    if (target == null) continue;
                    var c = Color.Lerp(_targetColors[i], Color.white, amount);
                    c.a = _targetColors[i].a;
                    target.color = c;
                }
                return;
            }

            if (_renderer == null) return;

            if (!_highlighted)
            {
                _renderer.color = IsInvestigated ? _investigatedColor : _normalColor;
                return;
            }

            _renderer.color = IsInvestigated
                ? Color.Lerp(_pressedColor, _investigatedColor, 0.5f)
                : _pressedColor;
        }
        /// <summary>물건 그림이 차지한 자리. 밝힐 그림들을 모두 감싼다. 없으면 네모 칸의 자리다.</summary>
        public Bounds GetVisualBounds()
        {
            bool any = false;
            var b = new Bounds(transform.position, Vector3.zero);
            if (HasTargets)
                foreach (var t in _highlightTargets)
                {
                    if (t == null || !t.gameObject.activeInHierarchy) continue;
                    if (!any) { b = t.bounds; any = true; } else b.Encapsulate(t.bounds);
                }
            if (!any && _renderer != null) b = _renderer.bounds;
            return b;
        }

        // ------------------------------------------------------- 마우스를 올렸을 때

        [Header("마우스를 올렸을 때")]
        [Tooltip("물건 모양을 따라 둘러지는 외곽선의 색. 가까이 보기 화면의 외곽선과 같다.")]
        [SerializeField] private Color _outlineColor = new Color(1f, 0.86f, 0.25f, 1f);

        [Tooltip("외곽선 두께(월드 단위).")]
        [SerializeField] private float _outlineWidth = 0.05f;

        private readonly List<SpriteRenderer> _outlines = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _outlineSources = new List<SpriteRenderer>();   // _outlines 와 같은 순서. 네모 테두리는 null
        private bool _outlinesBuilt;
        private bool _hovered;

        /// <summary>
        /// 마우스를 올리면 물건 모양 그대로 노란 외곽선이 둘러진다. 내리면 사라진다.
        /// 밝힐 그림마다 같은 모양을 조금 크게 만들어 그 뒤에 깐다. 처음 올렸을 때 한 번 만든다.
        /// </summary>
        public void SetHovered(bool on)
        {
            if (_hovered == on) return;
            _hovered = on;
            if (on) BuildOutlines();
            // 꺼져 있는 그림(아직 생기지 않은 자국 같은 것)에는 외곽선도 두르지 않는다.
            for (int i = 0; i < _outlines.Count; i++)
            {
                var o = _outlines[i];
                if (o == null) continue;
                var src = _outlineSources[i];
                o.enabled = on && (src == null || (src.enabled && src.gameObject.activeInHierarchy));
            }
        }

        /// <summary>네모 칸 둘레의 테두리 네 줄. 칸이 차지한 자리를 월드에서 재어 그린다.</summary>
        private void BuildBoxOutline()
        {
            if (_renderer == null || _renderer.sprite == null) return;
            var b = _renderer.bounds;
            float w = _outlineWidth;
            var edges = new[]
            {
                (new Vector2(b.center.x, b.max.y + w * 0.5f), new Vector2(b.size.x + w * 2f, w)),
                (new Vector2(b.center.x, b.min.y - w * 0.5f), new Vector2(b.size.x + w * 2f, w)),
                (new Vector2(b.min.x - w * 0.5f, b.center.y), new Vector2(w, b.size.y)),
                (new Vector2(b.max.x + w * 0.5f, b.center.y), new Vector2(w, b.size.y)),
            };
            var unit = _renderer.sprite.bounds.size;
            foreach (var (center, size) in edges)
            {
                var go = new GameObject("BoxOutline");
                go.transform.SetParent(transform, true);
                go.transform.position = new Vector3(center.x, center.y, transform.position.z);
                go.transform.rotation = Quaternion.identity;
                var parentScale = transform.lossyScale;
                go.transform.localScale = new Vector3(size.x / unit.x / parentScale.x, size.y / unit.y / parentScale.y, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _renderer.sprite;
                sr.sortingLayerID = _renderer.sortingLayerID;
                sr.sortingOrder = _renderer.sortingOrder + 1;
                sr.color = _outlineColor;
                sr.enabled = false;
                _outlines.Add(sr);
                _outlineSources.Add(null);
            }
        }

        private void BuildOutlines()
        {
            if (_outlinesBuilt) return;
            _outlinesBuilt = true;

            var targets = new List<SpriteRenderer>();
            if (HasTargets) { foreach (var t in _highlightTargets) if (t != null) targets.Add(t); }
            else
            {
                // 밝힐 그림이 따로 없는 지점은 네모 칸 둘레에 테두리만 두른다. 칸을 통째로 칠하면 장면이 가려진다.
                BuildBoxOutline();
                return;
            }

            // 외곽선은 같은 묶음(SortingGroup) 안에서 가장 뒤의 그림보다 한 칸 뒤에 둔다. 물건의 다른 조각을 덮지 않는다.
            var lowest = new Dictionary<UnityEngine.Rendering.SortingGroup, int>();
            var noGroup = int.MaxValue;
            foreach (var t in targets)
            {
                var g = t.GetComponentInParent<UnityEngine.Rendering.SortingGroup>();
                if (g == null) noGroup = Mathf.Min(noGroup, t.sortingOrder);
                else lowest[g] = lowest.TryGetValue(g, out var v) ? Mathf.Min(v, t.sortingOrder) : t.sortingOrder;
            }

            foreach (var t in targets)
            {
                if (t.sprite == null) continue;
                var g = t.GetComponentInParent<UnityEngine.Rendering.SortingGroup>();
                int order = (g == null ? noGroup : lowest[g]) - 1;

                var go = new GameObject(t.name + "_Outline");
                go.transform.SetParent(t.transform.parent, false);
                go.transform.localPosition = t.transform.localPosition;
                go.transform.localRotation = t.transform.localRotation;
                go.transform.localScale = t.transform.localScale;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = t.sprite;
                sr.drawMode = t.drawMode;
                sr.flipX = t.flipX;
                sr.flipY = t.flipY;
                sr.maskInteraction = t.maskInteraction;
                sr.sortingLayerID = t.sortingLayerID;
                sr.sortingOrder = order;
                sr.sharedMaterial = t.sharedMaterial;
                sr.color = _outlineColor;

                // 둘레만큼 키운다. 월드에서 잰 두께가 어느 그림이든 같도록 그림의 월드 크기로 나눈다.
                var lossy = t.transform.lossyScale;
                float sx = Mathf.Max(1e-4f, Mathf.Abs(lossy.x)), sy = Mathf.Max(1e-4f, Mathf.Abs(lossy.y));
                if (t.drawMode != SpriteDrawMode.Simple)
                {
                    sr.size = t.size + new Vector2(_outlineWidth * 2f / sx, _outlineWidth * 2f / sy);
                }
                else
                {
                    var size = t.sprite.bounds.size;
                    float wx = size.x * sx, wy = size.y * sy;
                    var s = go.transform.localScale;
                    go.transform.localScale = new Vector3(s.x * (wx + _outlineWidth * 2f) / Mathf.Max(1e-4f, wx),
                        s.y * (wy + _outlineWidth * 2f) / Mathf.Max(1e-4f, wy), s.z);
                }
                sr.enabled = false;
                _outlines.Add(sr);
                _outlineSources.Add(t);
            }
        }

    }
}
