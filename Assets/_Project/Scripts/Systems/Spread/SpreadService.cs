using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.Save;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 괴담 확산도를 다룬다.
    ///
    /// 값은 SaveData.legendStates[].spreadRate 에 그대로 들어간다.
    /// 별도의 수치를 따로 들고 있지 않는다. 저장본과 어긋날 일을 만들지 않기 위해서다.
    ///
    /// 이번 단계에서는 시간에 따른 자동 확산이나 괴담 간 전파를 하지 않는다.
    /// 플레이어의 검열 행동에만 반응한다.
    /// </summary>
    public class SpreadService : IService
    {
        /// <summary>확산도 하한.</summary>
        public const float MinSpread = 0f;

        /// <summary>확산도 상한.</summary>
        public const float MaxSpread = 100f;

        private readonly GameDataCatalogSO _catalog;
        private readonly Dictionary<string, WebPageSO> _pagesById = new Dictionary<string, WebPageSO>();

        public SpreadService(GameDataCatalogSO catalog)
        {
            _catalog = catalog;
        }

        // ------------------------------------------------------------- 수명 주기

        public void Initialize()
        {
            _pagesById.Clear();

            if (_catalog == null)
            {
                Debug.LogError("[SpreadService] GameDataCatalog이 지정되지 않았다. GameRoot 인스펙터를 확인할 것.");
                return;
            }

            var pages = _catalog.WebPages;
            if (pages == null) return;

            for (int i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                if (page == null || string.IsNullOrEmpty(page.PageId)) continue;
                if (_pagesById.ContainsKey(page.PageId)) continue;

                _pagesById.Add(page.PageId, page);
            }

            Debug.Log($"[SpreadService] 준비 완료 | 확산 계수 참조 게시글 {_pagesById.Count}개 | 범위 {MinSpread}~{MaxSpread}");
        }

        public void Shutdown()
        {
            _pagesById.Clear();
        }

        // ------------------------------------------------------------- 확산도

        /// <summary>괴담의 현재 확산도. 기록이 없으면 0.</summary>
        public float GetSpreadRate(SaveData save, string legendId)
        {
            var state = FindState(save, legendId);
            return state != null ? state.spreadRate : 0f;
        }

        /// <summary>확산도를 직접 지정한다. 사건 시작 시 초기값을 넣을 때 쓴다.</summary>
        public bool TrySetSpreadRate(SaveData save, string legendId, float value)
        {
            if (save == null || string.IsNullOrEmpty(legendId)) return false;

            var state = save.GetOrCreateLegendState(legendId);
            if (state == null) return false;

            state.spreadRate = Mathf.Clamp(value, MinSpread, MaxSpread);
            return true;
        }

        /// <summary>확산도를 올린다. 상한을 넘지 않는다.</summary>
        public bool TryAddSpread(SaveData save, string legendId, float amount)
        {
            if (save == null || string.IsNullOrEmpty(legendId)) return false;
            if (amount < 0f) return TryReduceSpread(save, legendId, -amount);

            var state = save.GetOrCreateLegendState(legendId);
            if (state == null) return false;

            state.spreadRate = Mathf.Clamp(state.spreadRate + amount, MinSpread, MaxSpread);
            return true;
        }

        /// <summary>확산도를 내린다. 하한 아래로 내려가지 않는다.</summary>
        public bool TryReduceSpread(SaveData save, string legendId, float amount)
        {
            if (save == null || string.IsNullOrEmpty(legendId)) return false;
            if (amount < 0f) return TryAddSpread(save, legendId, -amount);

            var state = FindState(save, legendId);
            if (state == null) return false;   // 기록이 없으면 줄일 것도 없다

            state.spreadRate = Mathf.Clamp(state.spreadRate - amount, MinSpread, MaxSpread);
            return true;
        }

        // ------------------------------------------------------------- 게시글 계수

        /// <summary>게시글을 방치했을 때의 확산 기여도. 정상 검열 시 이만큼 줄인다.</summary>
        public float GetSpreadWeight(string pageId)
        {
            var page = FindPage(pageId);
            return page != null ? page.SpreadWeight : 0f;
        }

        /// <summary>잘못 검열했을 때의 페널티. 확산이 이만큼 늘어난다.</summary>
        public float GetWrongCensorPenalty(string pageId)
        {
            var page = FindPage(pageId);
            return page != null ? page.WrongCensorPenalty : 0f;
        }

        // ------------------------------------------------------------- 내부

        /// <summary>기록이 없으면 null. 없는 괴담을 조회해도 상태를 새로 만들지 않는다.</summary>
        private static LegendState FindState(SaveData save, string legendId)
        {
            if (save == null || save.legendStates == null || string.IsNullOrEmpty(legendId)) return null;

            for (int i = 0; i < save.legendStates.Count; i++)
            {
                if (save.legendStates[i] != null && save.legendStates[i].legendId == legendId)
                {
                    return save.legendStates[i];
                }
            }
            return null;
        }

        private WebPageSO FindPage(string pageId)
        {
            if (string.IsNullOrEmpty(pageId)) return null;
            return _pagesById.TryGetValue(pageId, out var page) ? page : null;
        }
    }
}
