using System.Collections.Generic;
using UnityEngine;

namespace UrbanLegendBureau.Data
{
    /// <summary>인터넷에서 볼 수 있는 정보의 종류.</summary>
    public enum WebPageType
    {
        /// <summary>커뮤니티 게시글</summary>
        ForumPost = 0,

        /// <summary>뉴스 기사</summary>
        NewsArticle = 1,

        /// <summary>개인 블로그</summary>
        BlogPost = 2,

        /// <summary>SNS 짧은 글</summary>
        SocialPost = 3,

        /// <summary>공공/내부 기록</summary>
        Record = 4
    }

    /// <summary>
    /// 인터넷 조사에서 만나는 페이지 하나의 정적 정의.
    ///
    /// 검열 여부는 여기에 저장하지 않는다. 검열하면
    /// SaveData.censoredPageIds 에 pageId 문자열만 들어간다.
    /// </summary>
    [CreateAssetMenu(fileName = "web_", menuName = "Game Data/Web Page", order = 40)]
    public class WebPageSO : ScriptableObject, IGameDataAsset
    {
        [Header("식별")]
        [Tooltip("SaveData에 기록되는 고유 ID. 예: web_test_001")]
        [SerializeField] private string _pageId;

        [Header("텍스트 (Localization String ID)")]
        [Tooltip("제목의 String ID. 예: web.test.001.title")]
        [SerializeField] private string _titleTextId;

        [Tooltip("본문의 String ID. 예: web.test.001.body")]
        [SerializeField] private string _bodyTextId;

        [Header("분류")]
        [SerializeField] private WebPageType _pageType = WebPageType.ForumPost;

        [Header("검색")]
        [Tooltip("이 글이 검색되는 키워드 ID들. 단서로 해금된 키워드만 검색할 수 있다.")]
        [SerializeField] private List<string> _searchKeywordIds = new List<string>();

        [Header("연결된 단서")]
        [Tooltip("이 글을 읽어서 얻을 수 있는 단서들의 ID.")]
        [SerializeField] private List<string> _relatedClueIds = new List<string>();

        [Header("확산")]
        [Tooltip("방치했을 때 괴담 확산도에 더해지는 양.")]
        [SerializeField, Range(0f, 100f)] private float _spreadWeight = 10f;

        [Tooltip("검열(삭제) 대상이 될 수 있는가. 끄면 삭제할 수 없는 글이다.")]
        [SerializeField] private bool _isCensorable = true;

        [Tooltip("잘못 검열했을 때의 페널티 크기. 무관한 글을 지우면 손해를 본다.")]
        [SerializeField, Range(0f, 100f)] private float _wrongCensorPenalty;

        public string Id => _pageId;
        public string PageId => _pageId;
        public string TitleTextId => _titleTextId;
        public string BodyTextId => _bodyTextId;
        public WebPageType PageType => _pageType;
        public IReadOnlyList<string> SearchKeywordIds => _searchKeywordIds;
        public IReadOnlyList<string> RelatedClueIds => _relatedClueIds;
        public float SpreadWeight => _spreadWeight;
        public bool IsCensorable => _isCensorable;
        public float WrongCensorPenalty => _wrongCensorPenalty;

        private void OnValidate()
        {
            _pageId = _pageId?.Trim();
            _titleTextId = _titleTextId?.Trim();
            _bodyTextId = _bodyTextId?.Trim();
        }
    }
}
