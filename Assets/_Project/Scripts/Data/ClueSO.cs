using System.Collections.Generic;
using UnityEngine;

namespace UrbanLegendBureau.Data
{
    /// <summary>단서의 성격. 어느 조사관의 영역인지도 여기서 갈린다.</summary>
    public enum ClueType
    {
        /// <summary>물리적 흔적 - 현장의 물건, 자국</summary>
        Physical = 0,

        /// <summary>디지털 기록 - 게시글, 로그, CCTV (차지한)</summary>
        Digital = 1,

        /// <summary>영적 흔적 - 잔류 감정, 기척 (한영)</summary>
        Spiritual = 2,

        /// <summary>증언 - 목격자 진술</summary>
        Testimony = 3,

        /// <summary>문서 - 기록물, 장부</summary>
        Document = 4
    }

    /// <summary>단서를 어디서 얻는가. 실제 발견 로직은 이후 단계에서 구현한다.</summary>
    public enum ClueDiscoverySource
    {
        /// <summary>현장의 조사 가능 오브젝트</summary>
        FieldObject = 0,

        /// <summary>인터넷 게시글</summary>
        WebPage = 1,

        /// <summary>대화</summary>
        Dialogue = 2,

        /// <summary>영적 탐지 모드</summary>
        SpiritTrace = 3
    }

    /// <summary>
    /// 조사 중 발견하는 단서의 정적 정의.
    ///
    /// 획득 여부는 여기에 저장하지 않는다. 플레이어가 얻으면
    /// SaveData.acquiredClueIds 에 이 에셋의 clueId 문자열만 들어간다.
    /// </summary>
    [CreateAssetMenu(fileName = "clue_", menuName = "Game Data/Clue", order = 30)]
    public class ClueSO : ScriptableObject, IGameDataAsset
    {
        [Header("식별")]
        [Tooltip("SaveData에 기록되는 고유 ID. 예: clue_test_001. 한 번 정하면 바꾸지 않는다.")]
        [SerializeField] private string _clueId;

        [Header("텍스트 (Localization String ID)")]
        [Tooltip("단서 이름/내용의 String ID. 예: clue.test.001.text")]
        [SerializeField] private string _clueTextId;

        [Header("분류")]
        [SerializeField] private ClueType _clueType = ClueType.Physical;

        [Header("발견 조건 (판정 로직은 이후 단계)")]
        [SerializeField] private ClueDiscoverySource _discoverySource = ClueDiscoverySource.FieldObject;

        [Tooltip("발견 위치가 되는 대상의 ID. 오브젝트 ID나 web 페이지 ID 등.")]
        [SerializeField] private string _sourceId;

        [Tooltip("이 단서를 얻기 전에 먼저 필요한 단서들의 ID.")]
        [SerializeField] private List<string> _requiredClueIds = new List<string>();

        public string Id => _clueId;
        public string ClueId => _clueId;
        public string ClueTextId => _clueTextId;
        public ClueType ClueType => _clueType;
        public ClueDiscoverySource DiscoverySource => _discoverySource;
        public string SourceId => _sourceId;
        public IReadOnlyList<string> RequiredClueIds => _requiredClueIds;

        private void OnValidate()
        {
            _clueId = _clueId?.Trim();
            _clueTextId = _clueTextId?.Trim();
            _sourceId = _sourceId?.Trim();
        }
    }
}
