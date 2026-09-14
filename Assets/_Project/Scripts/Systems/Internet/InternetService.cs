using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.Save;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 인터넷 페이지 조회 + 검열 처리.
    ///
    /// LegendService / RuleService 와 같은 패턴이다.
    /// 카탈로그의 직접 참조를 ID로 색인해 두고 조회만 한다.
    ///
    /// 이번 단계에서 확산 수치는 다루지 않는다.
    /// wrongCensorPenalty / spreadWeight 는 값을 꺼내볼 수만 있게 두고,
    /// 실제 수치 적용은 다음 단계에서 붙인다.
    /// </summary>
    public class InternetService : IService
    {
        private readonly GameDataCatalogSO _catalog;
        private readonly Dictionary<string, WebPageSO> _byId = new Dictionary<string, WebPageSO>();
        private readonly List<WebPageSO> _all = new List<WebPageSO>();

        public InternetService(GameDataCatalogSO catalog)
        {
            _catalog = catalog;
        }

        public int Count => _all.Count;

        public IReadOnlyList<WebPageSO> GetAllPages() => _all;

        // ------------------------------------------------------------- 수명 주기

        public void Initialize()
        {
            _byId.Clear();
            _all.Clear();

            if (_catalog == null)
            {
                Debug.LogError("[InternetService] GameDataCatalog이 지정되지 않았다. GameRoot 인스펙터를 확인할 것.");
                return;
            }

            var pages = _catalog.WebPages;
            if (pages == null)
            {
                Debug.LogError("[InternetService] 카탈로그의 게시글 목록이 비어 있다.");
                return;
            }

            int skipped = 0;
            for (int i = 0; i < pages.Count; i++)
            {
                var page = pages[i];

                if (page == null)
                {
                    Debug.LogError($"[InternetService] 카탈로그 {i}번 게시글 항목이 비어 있다.");
                    skipped++;
                    continue;
                }

                if (string.IsNullOrEmpty(page.PageId))
                {
                    Debug.LogError($"[InternetService] '{page.name}' 의 pageId가 비어 있다. 등록하지 않는다.");
                    skipped++;
                    continue;
                }

                if (_byId.TryGetValue(page.PageId, out var existing))
                {
                    Debug.LogError(
                        $"[InternetService] pageId '{page.PageId}' 가 중복된다. " +
                        $"'{existing.name}' 를 유지하고 '{page.name}' 를 건너뛴다.");
                    skipped++;
                    continue;
                }

                _byId.Add(page.PageId, page);
                _all.Add(page);
            }

            Debug.Log($"[InternetService] 게시글 {_all.Count}개 등록" + (skipped > 0 ? $" (건너뜀 {skipped}개)" : ""));
        }

        public void Shutdown()
        {
            _byId.Clear();
            _all.Clear();
        }

        // ------------------------------------------------------------- 조회

        /// <summary>ID로 게시글을 찾는다. 없으면 null을 돌려주고 경고를 남긴다.</summary>
        public WebPageSO GetPage(string pageId)
        {
            if (string.IsNullOrEmpty(pageId))
            {
                Debug.LogWarning("[InternetService] 빈 pageId로 조회했다.");
                return null;
            }

            if (_byId.TryGetValue(pageId, out var page)) return page;

            Debug.LogWarning($"[InternetService] '{pageId}' 에 해당하는 게시글이 없다. 카탈로그를 확인할 것.");
            return null;
        }

        public bool HasPage(string pageId)
        {
            return !string.IsNullOrEmpty(pageId) && _byId.ContainsKey(pageId);
        }

        /// <summary>검열할 수 있는 글인가. 데이터상의 성질이며 진행 상태와 무관하다.</summary>
        public bool IsCensorable(WebPageSO page)
        {
            return page != null && page.IsCensorable;
        }

        /// <summary>이미 검열한 글인가.</summary>
        public bool IsCensored(SaveData save, string pageId)
        {
            if (save == null || string.IsNullOrEmpty(pageId)) return false;
            return save.censoredPageIds != null && save.censoredPageIds.Contains(pageId);
        }

        /// <summary>
        /// 잘못된 검열에 매길 페널티 크기.
        /// 이번 단계에서는 읽기만 한다. 확산 수치에 반영하는 것은 다음 단계다.
        /// </summary>
        public float GetWrongCensorPenalty(string pageId)
        {
            var page = GetPageQuiet(pageId);
            return page != null ? page.WrongCensorPenalty : 0f;
        }

        /// <summary>방치했을 때 확산에 더해지는 양. 역시 이번 단계에서는 읽기만 한다.</summary>
        public float GetSpreadWeight(string pageId)
        {
            var page = GetPageQuiet(pageId);
            return page != null ? page.SpreadWeight : 0f;
        }

        // ------------------------------------------------------------- 열람 기록

        /// <summary>
        /// 열람 기록 접두사.
        /// 기존 SaveData.storyFlags 를 쓴다. storyFlags 는 원래 이런 용도의 범용 플래그 목록이라
        /// 새 필드를 만들거나 saveVersion 을 올리지 않아도 된다.
        /// </summary>
        private const string ViewedFlagPrefix = "web_viewed_";

        /// <summary>이미 열어 본 글인가. 자연 확산을 한 번만 적용하기 위한 판정이다.</summary>
        public bool IsPageViewed(SaveData save, string pageId)
        {
            if (save == null || save.storyFlags == null || string.IsNullOrEmpty(pageId)) return false;
            return save.storyFlags.Contains(ViewedFlagPrefix + pageId);
        }

        /// <summary>
        /// 열람으로 기록한다. 처음 열었을 때만 true.
        /// 같은 글을 다시 열거나 화면을 다시 들어와도 두 번 기록되지 않는다.
        /// </summary>
        public bool TryMarkPageViewed(SaveService save, string pageId)
        {
            if (save == null || save.Current == null || string.IsNullOrEmpty(pageId)) return false;
            if (!HasPage(pageId)) return false;

            var flag = ViewedFlagPrefix + pageId;
            if (save.Current.storyFlags.Contains(flag)) return false;

            save.Current.storyFlags.Add(flag);
            save.MarkDirty();
            return true;
        }

        /// <summary>
        /// 열람으로 확산이 퍼질 글인가.
        /// 이미 검열했거나 이미 열어 본 글은 더 이상 퍼지지 않는다.
        /// </summary>
        public bool ShouldSpreadOnView(SaveData save, string pageId)
        {
            if (save == null || !HasPage(pageId)) return false;
            if (IsCensored(save, pageId)) return false;
            return !IsPageViewed(save, pageId);
        }

        // ------------------------------------------------------------- 검열

        /// <summary>
        /// 게시글을 검열한다.
        /// 성공하면 SaveData.censoredPageIds 에 pageId 문자열만 추가한다.
        /// 실패 이유를 구분해 돌려주므로 호출 측에서 안내 문구를 고를 수 있다.
        /// </summary>
        public CensorResult TryCensorPage(SaveService save, string pageId)
        {
            if (save == null || save.Current == null) return CensorResult.NoSaveData;
            if (string.IsNullOrEmpty(pageId)) return CensorResult.NotFound;

            var page = GetPageQuiet(pageId);
            if (page == null) return CensorResult.NotFound;

            var censored = save.Current.censoredPageIds;
            if (censored.Contains(pageId)) return CensorResult.AlreadyCensored;   // 중복 방지

            if (!page.IsCensorable) return CensorResult.WrongTarget;

            censored.Add(pageId);
            save.MarkDirty();
            return CensorResult.Success;
        }

        /// <summary>지금 검열할 수 있는 상태인가. 버튼 노출 판단에 쓴다.</summary>
        public bool CanCensorNow(SaveService save, string pageId)
        {
            if (save == null || save.Current == null) return false;

            var page = GetPageQuiet(pageId);
            if (page == null || !page.IsCensorable) return false;

            return !save.Current.censoredPageIds.Contains(pageId);
        }

        // ------------------------------------------------------------- 내부

        /// <summary>로그 없이 조회한다. 내부 판정용.</summary>
        private WebPageSO GetPageQuiet(string pageId)
        {
            if (string.IsNullOrEmpty(pageId)) return null;
            return _byId.TryGetValue(pageId, out var page) ? page : null;
        }
    }
}
