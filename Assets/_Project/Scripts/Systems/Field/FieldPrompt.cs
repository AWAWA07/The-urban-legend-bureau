using TMPro;
using UnityEngine;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 조사할 수 있는 것 위에 뜨는 말풍선.
    ///
    /// 현장에 하나만 둔다. 가까이 간 지점이 바뀌면 그쪽으로 옮겨 다닌다.
    /// 여럿을 만들어 두고 켜고 끄지 않는 이유가 이것이다. 한 번에 하나만 뜬다.
    ///
    /// 무엇이 가까운지는 FieldController 가 정한다. 여기서는 받은 자리에 서서 받은 글을 보일 뿐이다.
    /// </summary>
    public class FieldPrompt : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;

        [Header("색")]
        [Tooltip("말풍선 바탕.")]
        [SerializeField] private SpriteRenderer _plate;

        [Tooltip("말풍선 테두리. 색이 가장 눈에 띄는 자리다.")]
        [SerializeField] private SpriteRenderer _edge;

        [Tooltip("말풍선 아래의 뾰족한 끝. 바탕과 같은 색으로 둔다.")]
        [SerializeField] private SpriteRenderer _tail;

        [SerializeField] private Color _plateColor = new Color(0.06f, 0.06f, 0.09f, 0.92f);
        [SerializeField] private Color _edgeColor = new Color(0.86f, 0.74f, 0.48f, 0.9f);
        [SerializeField] private Color _textColor = new Color(0.93f, 0.93f, 0.96f);

        [Header("이미 조사한 곳")]
        [Tooltip("다 본 것은 연둣빛으로 둔다. 글을 읽기 전에 색으로 먼저 안다.")]
        [SerializeField] private Color _donePlateColor = new Color(0.05f, 0.09f, 0.06f, 0.92f);

        [SerializeField] private Color _doneEdgeColor = new Color(0.58f, 0.82f, 0.45f, 0.9f);
        [SerializeField] private Color _doneTextColor = new Color(0.80f, 0.92f, 0.74f);

        [Tooltip("지점 맨 위에서 이만큼 띄워 뜬다.")]
        [SerializeField] private float _lift = 0.55f;

        [Tooltip("위아래로 까딱이는 폭. 가만히 있으면 배경 무늬로 보인다.")]
        [SerializeField] private float _bob = 0.06f;

        [Tooltip("한 번 까딱이는 데 걸리는 시간(초).")]
        [SerializeField] private float _period = 1.6f;

        [Tooltip("이 높이보다 위로는 올라가지 않는다. 장면 맨 위 한 줄에 가려지기 때문이다.")]
        [SerializeField] private float _maxY = 3.1f;

        private Vector3 _home;
        private bool _showing;

        private void Awake()
        {
            Hide();
        }

        /// <summary>이 지점 위에 이 글로 띄운다.</summary>
        public void Show(InvestigationPoint point, string text)
        {
            if (point == null)
            {
                Hide();
                return;
            }

            if (_label != null) _label.text = text;

            ApplyColors(point.IsInvestigated);

            // 지점의 네모 맨 위에서 띄운다. 지점마다 크기가 달라 그 윗변을 직접 잰다.
            float top = point.transform.position.y;
            var sprite = point.GetComponent<SpriteRenderer>();
            if (sprite != null) top = sprite.bounds.max.y;

            // 천장에 달린 것(CCTV)은 그 위가 화면 밖이다. 그럴 때는 물건 위가 아니라 옆에 뜬다.
            _home = new Vector3(point.transform.position.x, Mathf.Min(top + _lift, _maxY), 0f);
            transform.position = _home;

            _showing = true;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _showing = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 아직 안 본 곳과 다 본 곳의 색을 가른다.
        /// 글을 읽기 전에 색으로 먼저 알아야 지나칠 곳을 빨리 지나친다.
        /// </summary>
        private void ApplyColors(bool done)
        {
            if (_plate != null) _plate.color = done ? _donePlateColor : _plateColor;
            if (_tail != null) _tail.color = done ? _donePlateColor : _plateColor;
            if (_edge != null) _edge.color = done ? _doneEdgeColor : _edgeColor;
            if (_label != null) _label.color = done ? _doneTextColor : _textColor;
        }

        private void Update()
        {
            if (!_showing || _period <= 0f) return;

            float phase = Mathf.Sin(Time.unscaledTime * (Mathf.PI * 2f / _period));
            transform.position = _home + new Vector3(0f, phase * _bob, 0f);
        }
    }
}
